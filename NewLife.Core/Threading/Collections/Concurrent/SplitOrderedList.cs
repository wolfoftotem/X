using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Concurrent
{
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
				SpinWait sw = default(SpinWait);
				while (true)
				{
					if ((rwlock & 3) > 0)
					{
						sw.SpinOnce();
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
				SpinWait sw = default(SpinWait);
				while (true)
				{
					int state = rwlock;
					if (state < 2)
					{
						if (Interlocked.CompareExchange(ref rwlock, 2, state) == state)
						{
							break;
						}
						state = rwlock;
					}
					while ((state & 1) == 0 && Interlocked.CompareExchange(ref rwlock, state | 1, state) != state)
					{
						state = rwlock;
					}
					while (rwlock > 1)
					{
						sw.SpinOnce();
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
			uint b = key % (uint)size;
			Node bucket;
			if ((bucket = GetBucket(b)) == null)
			{
				bucket = InitializeBucket(b);
			}
			if (!ListInsert(node, bucket, out current, dataCreator))
			{
				return false;
			}
			int csize = size;
			if (Interlocked.Increment(ref count) / csize > 5 && (csize & 0x40000000) == 0)
			{
				Interlocked.CompareExchange(ref size, 2 * csize, csize);
			}
			current = node;
			return true;
		}

		public bool Find(uint key, TKey subKey, out T data)
		{
			uint b = key % (uint)size;
			data = default(T);
			Node bucket;
			if ((bucket = GetBucket(b)) == null)
			{
				bucket = InitializeBucket(b);
			}
			if (!ListFind(ComputeRegularKey(key), subKey, bucket, out var node))
			{
				return false;
			}
			data = node.Data;
			return !node.Marked;
		}

		public bool CompareExchange(uint key, TKey subKey, T data, Func<T, bool> check)
		{
			uint b = key % (uint)size;
			Node bucket;
			if ((bucket = GetBucket(b)) == null)
			{
				bucket = InitializeBucket(b);
			}
			if (!ListFind(ComputeRegularKey(key), subKey, bucket, out var node))
			{
				return false;
			}
			if (!check(node.Data))
			{
				return false;
			}
			node.Data = data;
			return true;
		}

		public bool Delete(uint key, TKey subKey, out T data)
		{
			uint b = key % (uint)size;
			Node bucket;
			if ((bucket = GetBucket(b)) == null)
			{
				bucket = InitializeBucket(b);
			}
			if (!ListDelete(bucket, ComputeRegularKey(key), subKey, out data))
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
			Node bucket;
			if ((bucket = GetBucket(parent)) == null)
			{
				bucket = InitializeBucket(parent);
			}
			Node dummy = pool.Take().Init(ComputeDummyKey(b));
			if (!ListInsert(dummy, bucket, out var current, null))
			{
				return current;
			}
			return SetBucket(b, dummy);
		}

		private static uint GetParent(uint v)
		{
			uint tt;
			uint t;
			int pos = (((tt = v >> 16) == 0) ? (((t = v >> 8) != 0) ? (8 + logTable[t]) : logTable[v]) : (((t = tt >> 8) != 0) ? (24 + logTable[t]) : (16 + logTable[tt])));
			return (uint)(v & ~(1 << pos));
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
			Node leftNodeNext = null;
			Node rightNode = null;
			while (true)
			{
				Node t = h;
				Node tNext = t.Next;
				do
				{
					if (!tNext.Marked)
					{
						left = t;
						leftNodeNext = tNext;
					}
					t = (tNext.Marked ? tNext.Next : tNext);
					if (t == tail)
					{
						break;
					}
					tNext = t.Next;
				}
				while (tNext.Marked || t.Key < key || (tNext.Key == key && !comparer.Equals(subKey, t.SubKey)));
				rightNode = t;
				if (leftNodeNext == rightNode)
				{
					if (rightNode == tail || !rightNode.Next.Marked)
					{
						return rightNode;
					}
				}
				else if (Interlocked.CompareExchange(ref left.Next, rightNode, leftNodeNext) == leftNodeNext)
				{
					pool.Release(leftNodeNext);
					if (rightNode == tail || !rightNode.Next.Marked)
					{
						break;
					}
				}
			}
			return rightNode;
		}

		private bool ListDelete(Node startPoint, ulong key, TKey subKey, out T data)
		{
			Node rightNode = null;
			Node rightNodeNext = null;
			Node leftNode = null;
			data = default(T);
			Node markedNode = null;
			while (true)
			{
				rightNode = ListSearch(key, subKey, ref leftNode, startPoint);
				if (rightNode == tail || rightNode.Key != key || !comparer.Equals(subKey, rightNode.SubKey))
				{
					return false;
				}
				data = rightNode.Data;
				rightNodeNext = rightNode.Next;
				if (!rightNodeNext.Marked)
				{
					if (markedNode == null)
					{
						markedNode = pool.Take();
					}
					markedNode.Init(rightNodeNext);
					if (Interlocked.CompareExchange(ref rightNode.Next, markedNode, rightNodeNext) == rightNodeNext)
					{
						break;
					}
				}
			}
			if (Interlocked.CompareExchange(ref leftNode.Next, rightNodeNext, rightNode) != rightNode)
			{
				ListSearch(rightNode.Key, subKey, ref leftNode, startPoint);
			}
			else
			{
				pool.Release(rightNode);
			}
			return true;
		}

		private bool ListInsert(Node newNode, Node startPoint, out Node current, Func<T> dataCreator)
		{
			ulong key = newNode.Key;
			Node rightNode = null;
			Node leftNode = null;
			do
			{
				rightNode = (current = ListSearch(key, newNode.SubKey, ref leftNode, startPoint));
				if (rightNode != tail && rightNode.Key == key && comparer.Equals(newNode.SubKey, rightNode.SubKey))
				{
					return false;
				}
				newNode.Next = rightNode;
				if (dataCreator != null)
				{
					newNode.Data = dataCreator();
				}
			}
			while (Interlocked.CompareExchange(ref leftNode.Next, newNode, rightNode) != rightNode);
			return true;
		}

		private bool ListFind(ulong key, TKey subKey, Node startPoint, out Node data)
		{
			Node rightNode = null;
			Node leftNode = null;
			data = null;
			rightNode = (data = ListSearch(key, subKey, ref leftNode, startPoint));
			if (rightNode != tail && rightNode.Key == key)
			{
				return comparer.Equals(subKey, rightNode.SubKey);
			}
			return false;
		}
	}
}
