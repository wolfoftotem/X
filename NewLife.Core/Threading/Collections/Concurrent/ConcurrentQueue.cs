using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Collections.Concurrent;

[DebuggerTypeProxy(typeof(CollectionDebuggerView<>))]
[DebuggerDisplay("Count={Count}")]
public class ConcurrentQueue<T> : IProducerConsumerCollection<T>, IEnumerable<T>, ICollection, IEnumerable
{
	private class Node
	{
		public T Value;

		public Node Next;
	}

	private class NodeObjectPool : ObjectPool<Node>
	{
		protected override Node Creator()
		{
			return new Node();
		}
	}

	private Node head = new Node();

	private Node tail;

	private int count;

	private static readonly NodeObjectPool pool = new NodeObjectPool();

	private object syncRoot = new object();

	bool ICollection.IsSynchronized => true;

	object ICollection.SyncRoot => syncRoot;

	public int Count => count;

	public bool IsEmpty => count == 0;

	private static Node ZeroOut(Node node)
	{
		node.Value = default(T);
		node.Next = null;
		return node;
	}

	public ConcurrentQueue()
	{
		tail = head;
	}

	public ConcurrentQueue(IEnumerable<T> collection)
		: this()
	{
		foreach (T item in collection)
		{
			Enqueue(item);
		}
	}

	public void Enqueue(T item)
	{
		Node node = pool.Take();
		node.Value = item;
		Node node2 = null;
		Node node3 = null;
		bool flag = false;
		while (!flag)
		{
			node2 = tail;
			node3 = node2.Next;
			if (tail == node2)
			{
				if (node3 == null)
				{
					flag = Interlocked.CompareExchange(ref tail.Next, node, null) == null;
				}
				else
				{
					Interlocked.CompareExchange(ref tail, node3, node2);
				}
			}
		}
		Interlocked.CompareExchange(ref tail, node, node2);
		Interlocked.Increment(ref count);
	}

	bool IProducerConsumerCollection<T>.TryAdd(T item)
	{
		Enqueue(item);
		return true;
	}

	public bool TryDequeue(out T result)
	{
		result = default(T);
		bool flag = false;
		while (!flag)
		{
			Node node = head;
			Node node2 = tail;
			Node next = node.Next;
			if (node != head)
			{
				continue;
			}
			if (node == node2)
			{
				if (next != null)
				{
					Interlocked.CompareExchange(ref tail, next, node2);
				}
				result = default(T);
				return false;
			}
			result = next.Value;
			flag = Interlocked.CompareExchange(ref head, next, node) == node;
			if (flag)
			{
				pool.Release(ZeroOut(node));
			}
		}
		Interlocked.Decrement(ref count);
		return true;
	}

	public bool TryPeek(out T result)
	{
		if (IsEmpty)
		{
			result = default(T);
			return false;
		}
		Node next = head.Next;
		result = next.Value;
		return true;
	}

	internal void Clear()
	{
		count = 0;
		tail = (head = new Node());
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return InternalGetEnumerator();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return InternalGetEnumerator();
	}

	private IEnumerator<T> InternalGetEnumerator()
	{
		Node my_head = head;
		while (true)
		{
			Node next;
			my_head = (next = my_head.Next);
			if (next != null)
			{
				yield return my_head.Value;
				continue;
			}
			break;
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array is T[] array2)
		{
			CopyTo(array2, index);
		}
	}

	public void CopyTo(T[] array, int index)
	{
		IEnumerator<T> enumerator = InternalGetEnumerator();
		int num = index;
		while (enumerator.MoveNext())
		{
			array[num++] = enumerator.Current;
		}
	}

	public T[] ToArray()
	{
		T[] array = new T[count];
		CopyTo(array, 0);
		return array;
	}

	bool IProducerConsumerCollection<T>.TryTake(out T item)
	{
		return TryDequeue(out item);
	}
}
