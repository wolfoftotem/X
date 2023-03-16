using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Collections.Concurrent;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(CollectionDebuggerView<>))]
public class ConcurrentStack<T> : IProducerConsumerCollection<T>, IEnumerable<T>, ICollection, IEnumerable
{
	private class Node
	{
		public T Value = default(T);

		public Node Next;
	}

	private class NodeObjectPool : ObjectPool<Node>
	{
		protected override Node Creator()
		{
			return new Node();
		}
	}

	private Node head;

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

	public ConcurrentStack()
	{
	}

	public ConcurrentStack(IEnumerable<T> collection)
	{
		foreach (T item in collection)
		{
			Push(item);
		}
	}

	bool IProducerConsumerCollection<T>.TryAdd(T elem)
	{
		Push(elem);
		return true;
	}

	public void Push(T item)
	{
		Node node = pool.Take();
		node.Value = item;
		do
		{
			node.Next = head;
		}
		while (Interlocked.CompareExchange(ref head, node, node.Next) != node.Next);
		Interlocked.Increment(ref count);
	}

	public void PushRange(T[] items)
	{
		PushRange(items, 0, items.Length);
	}

	public void PushRange(T[] items, int startIndex, int count)
	{
		Node node = null;
		Node node2 = null;
		for (int i = startIndex; i < count; i++)
		{
			Node node3 = pool.Take();
			node3.Value = items[i];
			node3.Next = node;
			node = node3;
			if (node2 == null)
			{
				node2 = node3;
			}
		}
		do
		{
			node2.Next = head;
		}
		while (Interlocked.CompareExchange(ref head, node, node2.Next) != node2.Next);
		Interlocked.Add(ref count, count);
	}

	public bool TryPop(out T result)
	{
		Node node;
		do
		{
			node = head;
			if (node == null)
			{
				result = default(T);
				return false;
			}
		}
		while (Interlocked.CompareExchange(ref head, node.Next, node) != node);
		Interlocked.Decrement(ref count);
		result = node.Value;
		pool.Release(ZeroOut(node));
		return true;
	}

	public int TryPopRange(T[] items)
	{
		return TryPopRange(items, 0, items.Length);
	}

	public int TryPopRange(T[] items, int startIndex, int count)
	{
		Node next;
		Node node;
		do
		{
			next = head;
			if (next == null)
			{
				return -1;
			}
			node = next;
			for (int i = 0; i < count - 1; i++)
			{
				node = node.Next;
				if (node == null)
				{
					break;
				}
			}
		}
		while (Interlocked.CompareExchange(ref head, node, next) != next);
		int j;
		for (j = startIndex; j < count; j++)
		{
			if (next == null)
			{
				break;
			}
			items[j] = next.Value;
			node = next;
			next = next.Next;
			pool.Release(ZeroOut(node));
		}
		return j - 1;
	}

	public bool TryPeek(out T result)
	{
		Node node = head;
		if (node == null)
		{
			result = default(T);
			return false;
		}
		result = node.Value;
		return true;
	}

	public void Clear()
	{
		count = 0;
		head = null;
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
		if (my_head != null)
		{
			Node next;
			do
			{
				yield return my_head.Value;
				my_head = (next = my_head.Next);
			}
			while (next != null);
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

	bool IProducerConsumerCollection<T>.TryTake(out T item)
	{
		return TryPop(out item);
	}

	public T[] ToArray()
	{
		T[] array = new T[count];
		CopyTo(array, 0);
		return array;
	}
}
