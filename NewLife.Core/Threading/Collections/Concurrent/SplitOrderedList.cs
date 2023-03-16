using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Concurrent;

internal class SplitOrderedList<TKey, T>
{
	private class Node
	{
		public bool Marked;

		public ulong Key;

		public TKey SubKey;

		public T Data;

		public Node Next;

		public Node Init(ulong key, TKey subKey, T data)
		{
			Key = key;
			SubKey = subKey;
			Data = data;
			Marked = false;
			Next = null;
			return this;
		}

		public Node Init(ulong key)
		{
			Key = key;
			Data = default(T);
			Next = null;
			Marked = false;
			SubKey = default(TKey);
			return this;
		}

		public Node Init(Node wrapped)
		{
			Marked = true;
			Next = wrapped;
			Key = 0uL;
			Data = default(T);
			SubKey = default(TKey);
			return this;
		}
	}

	private class NodeObjectPool : ObjectPool<Node>
	{
		protected override Node Creator()
		{
			return new Node();
		}
	}

	private struct SimpleRwLock
	{
		private const int RwWait = 1;

		private const int RwWrite = 2;

		private const int RwRead = 4;

		private int rwlock;

		public void EnterReadLock()
		{
			SpinWait spinWait = default(SpinWait);
			while (true)
			{
				if ((rwlock & 3) > 0)
				{
					spinWait.SpinOnce();
					continue;
				}
				if ((Interlocked.Add(ref rwlock, 4) & 1) == 0)
				{
					break;
				}
				Interlocked.Add(ref rwlock, -4);
			}
		}

		public void ExitReadLock()
		{
			Interlocked.Add(ref rwlock, -4);
		}

		public void EnterWriteLock()
		{
			SpinWait spinWait = default(SpinWait);
			while (true)
			{
				int num = rwlock;
				if (num < 2)
				{
					if (Interlocked.CompareExchange(ref rwlock, 2, num) == num)
					{
						break;
					}
					num = rwlock;
				}
				while ((num & 1) == 0 && Interlocked.CompareExchange(ref rwlock, num | 1, num) != num)
				{
					num = rwlock;
				}
				while (rwlock > 1)
				{
					spinWait.SpinOnce();
				}
			}
		}

		public void ExitWriteLock()
		{
			Interlocked.Add(ref rwlock, -2);
		}
	}

	private const int MaxLoad = 5;

	private const uint BucketSize = 512u;

	private static readonly NodeObjectPool pool = new NodeObjectPool();

	private Node head;

	private Node tail;

	private Node[] buckets = new Node[512];

	private int count;

	private int size = 2;

	private SimpleRwLock slim = default(SimpleRwLock);

	private readonly IEqualityComparer<TKey> comparer;

	private static readonly byte[] reverseTable = new byte[256]
	{
		0, 128, 64, 192, 32, 160, 96, 224, 16, 144,
		80, 208, 48, 176, 112, 240, 8, 136, 72, 200,
		40, 168, 104, 232, 24, 152, 88, 216, 56, 184,
		120, 248, 4, 132, 68, 196, 36, 164, 100, 228,
		20, 148, 84, 212, 52, 180, 116, 244, 12, 140,
		76, 204, 44, 172, 108, 236, 28, 156, 92, 220,
		60, 188, 124, 252, 2, 130, 66, 194, 34, 162,
		98, 226, 18, 146, 82, 210, 50, 178, 114, 242,
		10, 138, 74, 202, 42, 170, 106, 234, 26, 154,
		90, 218, 58, 186, 122, 250, 6, 134, 70, 198,
		38, 166, 102, 230, 22, 150, 86, 214, 54, 182,
		118, 246, 14, 142, 78, 206, 46, 174, 110, 238,
		30, 158, 94, 222, 62, 190, 126, 254, 1, 129,
		65, 193, 33, 161, 97, 225, 17, 145, 81, 209,
		49, 177, 113, 241, 9, 137, 73, 201, 41, 169,
		105, 233, 25, 153, 89, 217, 57, 185, 121, 249,
		5, 133, 69, 197, 37, 165, 101, 229, 21, 149,
		85, 213, 53, 181, 117, 245, 13, 141, 77, 205,
		45, 173, 109, 237, 29, 157, 93, 221, 61, 189,
		125, 253, 3, 131, 67, 195, 35, 163, 99, 227,
		19, 147, 83, 211, 51, 179, 115, 243, 11, 139,
		75, 203, 43, 171, 107, 235, 27, 155, 91, 219,
		59, 187, 123, 251, 7, 135, 71, 199, 39, 167,
		103, 231, 23, 151, 87, 215, 55, 183, 119, 247,
		15, 143, 79, 207, 47, 175, 111, 239, 31, 159,
		95, 223, 63, 191, 127, 255
	};

	private static readonly byte[] logTable = new byte[256]
	{
		255, 0, 1, 1, 2, 2, 2, 2, 3, 3,
		3, 3, 3, 3, 3, 3, 4, 4, 4, 4,
		4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
		4, 4, 5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
		5, 5, 5, 5, 6, 6, 6, 6, 6, 6,
		6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
		6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
		6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
		6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
		6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
		6, 6, 6, 6, 6, 6, 6, 6, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7
	};

	public int Count => count;

	public SplitOrderedList(IEqualityComparer<TKey> comparer)
	{
		this.comparer = comparer;
		head = new Node().Init(0uL);
		tail = new Node().Init(ulong.MaxValue);
		head.Next = tail;
		SetBucket(0u, head);
	}

	public T InsertOrUpdate(uint key, TKey subKey, Func<T> addGetter, Func<T, T> updateGetter)
	{
		if (InsertInternal(key, subKey, default(T), addGetter, out var current))
		{
			return current.Data;
		}
		return current.Data = updateGetter(current.Data);
	}

	public T InsertOrUpdate(uint key, TKey subKey, T addValue, T updateValue)
	{
		if (InsertInternal(key, subKey, addValue, null, out var current))
		{
			return current.Data;
		}
		return current.Data = updateValue;
	}

	public bool Insert(uint key, TKey subKey, T data)
	{
		Node current;
		return InsertInternal(key, subKey, data, null, out current);
	}

	public T InsertOrGet(uint key, TKey subKey, T data, Func<T> dataCreator)
	{
		InsertInternal(key, subKey, data, dataCreator, out var current);
		return current.Data;
	}

	private bool InsertInternal(uint key, TKey subKey, T data, Func<T> dataCreator, out Node current)
	{
		Node node = pool.Take().Init(ComputeRegularKey(key), subKey, data);
		uint num = key % (uint)size;
		Node startPoint;
		if ((startPoint = GetBucket(num)) == null)
		{
			startPoint = InitializeBucket(num);
		}
		if (!ListInsert(node, startPoint, out current, dataCreator))
		{
			return false;
		}
		int num2 = size;
		if (Interlocked.Increment(ref count) / num2 > 5 && (num2 & 0x40000000) == 0)
		{
			Interlocked.CompareExchange(ref size, 2 * num2, num2);
		}
		current = node;
		return true;
	}

	public bool Find(uint key, TKey subKey, out T data)
	{
		uint num = key % (uint)size;
		data = default(T);
		Node startPoint;
		if ((startPoint = GetBucket(num)) == null)
		{
			startPoint = InitializeBucket(num);
		}
		if (!ListFind(ComputeRegularKey(key), subKey, startPoint, out var data2))
		{
			return false;
		}
		data = data2.Data;
		return !data2.Marked;
	}

	public bool CompareExchange(uint key, TKey subKey, T data, Func<T, bool> check)
	{
		uint num = key % (uint)size;
		Node startPoint;
		if ((startPoint = GetBucket(num)) == null)
		{
			startPoint = InitializeBucket(num);
		}
		if (!ListFind(ComputeRegularKey(key), subKey, startPoint, out var data2))
		{
			return false;
		}
		if (!check(data2.Data))
		{
			return false;
		}
		data2.Data = data;
		return true;
	}

	public bool Delete(uint key, TKey subKey, out T data)
	{
		uint num = key % (uint)size;
		Node startPoint;
		if ((startPoint = GetBucket(num)) == null)
		{
			startPoint = InitializeBucket(num);
		}
		if (!ListDelete(startPoint, ComputeRegularKey(key), subKey, out data))
		{
			return false;
		}
		Interlocked.Decrement(ref count);
		return true;
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (Node node = head.Next; node != tail; node = node.Next)
		{
			while (node.Marked || (node.Key & 1) == 0)
			{
				node = node.Next;
				if (node == tail)
				{
					yield break;
				}
			}
			yield return node.Data;
		}
	}

	private Node InitializeBucket(uint b)
	{
		uint parent = GetParent(b);
		Node startPoint;
		if ((startPoint = GetBucket(parent)) == null)
		{
			startPoint = InitializeBucket(parent);
		}
		Node node = pool.Take().Init(ComputeDummyKey(b));
		if (!ListInsert(node, startPoint, out var current, null))
		{
			return current;
		}
		return SetBucket(b, node);
	}

	private static uint GetParent(uint v)
	{
		uint num;
		uint num2;
		int num3 = (((num = v >> 16) == 0) ? (((num2 = v >> 8) != 0) ? (8 + logTable[num2]) : logTable[v]) : (((num2 = num >> 8) != 0) ? (24 + logTable[num2]) : (16 + logTable[num])));
		return (uint)(v & ~(1 << num3));
	}

	private static ulong ComputeRegularKey(uint key)
	{
		return ComputeDummyKey(key) | 1;
	}

	private static ulong ComputeDummyKey(uint key)
	{
		return (ulong)(uint)((reverseTable[key & 0xFF] << 24) | (reverseTable[(key >> 8) & 0xFF] << 16) | (reverseTable[(key >> 16) & 0xFF] << 8) | reverseTable[(key >> 24) & 0xFF]) << 1;
	}

	private Node GetBucket(uint index)
	{
		if (index >= buckets.Length)
		{
			return null;
		}
		return buckets[index];
	}

	private Node SetBucket(uint index, Node node)
	{
		try
		{
			slim.EnterReadLock();
			CheckSegment(index, readLockTaken: true);
			Interlocked.CompareExchange(ref buckets[index], node, null);
			return buckets[index];
		}
		finally
		{
			slim.ExitReadLock();
		}
	}

	private void CheckSegment(uint segment, bool readLockTaken)
	{
		if (segment < buckets.Length)
		{
			return;
		}
		if (readLockTaken)
		{
			slim.ExitReadLock();
		}
		try
		{
			slim.EnterWriteLock();
			while (segment >= buckets.Length)
			{
				Array.Resize(ref buckets, buckets.Length * 2);
			}
		}
		finally
		{
			slim.ExitWriteLock();
		}
		if (readLockTaken)
		{
			slim.EnterReadLock();
		}
	}

	private Node ListSearch(ulong key, TKey subKey, ref Node left, Node h)
	{
		Node node = null;
		Node node2 = null;
		while (true)
		{
			Node node3 = h;
			Node next = node3.Next;
			do
			{
				if (!next.Marked)
				{
					left = node3;
					node = next;
				}
				node3 = (next.Marked ? next.Next : next);
				if (node3 == tail)
				{
					break;
				}
				next = node3.Next;
			}
			while (next.Marked || node3.Key < key || (next.Key == key && !comparer.Equals(subKey, node3.SubKey)));
			node2 = node3;
			if (node == node2)
			{
				if (node2 == tail || !node2.Next.Marked)
				{
					return node2;
				}
			}
			else if (Interlocked.CompareExchange(ref left.Next, node2, node) == node)
			{
				pool.Release(node);
				if (node2 == tail || !node2.Next.Marked)
				{
					break;
				}
			}
		}
		return node2;
	}

	private bool ListDelete(Node startPoint, ulong key, TKey subKey, out T data)
	{
		Node node = null;
		Node node2 = null;
		Node left = null;
		data = default(T);
		Node node3 = null;
		while (true)
		{
			node = ListSearch(key, subKey, ref left, startPoint);
			if (node == tail || node.Key != key || !comparer.Equals(subKey, node.SubKey))
			{
				return false;
			}
			data = node.Data;
			node2 = node.Next;
			if (!node2.Marked)
			{
				if (node3 == null)
				{
					node3 = pool.Take();
				}
				node3.Init(node2);
				if (Interlocked.CompareExchange(ref node.Next, node3, node2) == node2)
				{
					break;
				}
			}
		}
		if (Interlocked.CompareExchange(ref left.Next, node2, node) != node)
		{
			ListSearch(node.Key, subKey, ref left, startPoint);
		}
		else
		{
			pool.Release(node);
		}
		return true;
	}

	private bool ListInsert(Node newNode, Node startPoint, out Node current, Func<T> dataCreator)
	{
		ulong key = newNode.Key;
		Node node = null;
		Node left = null;
		do
		{
			node = (current = ListSearch(key, newNode.SubKey, ref left, startPoint));
			if (node != tail && node.Key == key && comparer.Equals(newNode.SubKey, node.SubKey))
			{
				return false;
			}
			newNode.Next = node;
			if (dataCreator != null)
			{
				newNode.Data = dataCreator();
			}
		}
		while (Interlocked.CompareExchange(ref left.Next, newNode, node) != node);
		return true;
	}

	private bool ListFind(ulong key, TKey subKey, Node startPoint, out Node data)
	{
		Node node = null;
		Node left = null;
		data = null;
		node = (data = ListSearch(key, subKey, ref left, startPoint));
		if (node != tail && node.Key == key)
		{
			return comparer.Equals(subKey, node.SubKey);
		}
		return false;
	}
}
