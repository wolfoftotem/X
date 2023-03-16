using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Concurrent;

public class ConcurrentOrderedList<T> : ICollection<T>, IEnumerable<T>, IEnumerable
{
	private class Node
	{
		public T Data;

		public int Key;

		public Node Next;

		public bool Marked;

		public Node()
		{
		}

		public Node(Node wrapped)
		{
			Marked = true;
			Next = wrapped;
		}
	}

	private Node head;

	private Node tail;

	private IEqualityComparer<T> comparer;

	private int count;

	public IEqualityComparer<T> Comparer => comparer;

	public int Count => count;

	bool ICollection<T>.IsReadOnly => false;

	public ConcurrentOrderedList()
		: this((IEqualityComparer<T>)EqualityComparer<T>.Default)
	{
	}

	public ConcurrentOrderedList(IEqualityComparer<T> comparer)
	{
		if (comparer == null)
		{
			throw new ArgumentNullException("comparer");
		}
		this.comparer = comparer;
		head = new Node();
		tail = new Node();
		head.Next = tail;
	}

	public bool TryAdd(T data)
	{
		Node node = new Node();
		node.Data = data;
		node.Key = comparer.GetHashCode(data);
		if (ListInsert(node))
		{
			Interlocked.Increment(ref count);
			return true;
		}
		return false;
	}

	public bool TryRemove(T data)
	{
		T data2;
		return TryRemoveHash(comparer.GetHashCode(data), out data2);
	}

	public bool TryRemoveHash(int key, out T data)
	{
		if (ListDelete(key, out data))
		{
			Interlocked.Decrement(ref count);
			return true;
		}
		return false;
	}

	public bool TryPop(out T data)
	{
		return ListPop(out data);
	}

	public bool Contains(T data)
	{
		return ContainsHash(comparer.GetHashCode(data));
	}

	public bool ContainsHash(int key)
	{
		if (!ListFind(key, out var _))
		{
			return false;
		}
		return true;
	}

	public bool TryGetFromHash(int key, out T data)
	{
		data = default(T);
		if (!ListFind(key, out var data2))
		{
			return false;
		}
		data = data2.Data;
		return true;
	}

	public void Clear()
	{
		head.Next = tail;
	}

	public void CopyTo(T[] array, int startIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex");
		}
		if (count > array.Length - startIndex)
		{
			throw new ArgumentException("array", "The number of elements is greater than the available space from startIndex to the end of the destination array.");
		}
		foreach (T item in (IEnumerable<T>)this)
		{
			if (startIndex < array.Length)
			{
				array[startIndex++] = item;
				continue;
			}
			break;
		}
	}

	private Node ListSearch(int key, ref Node left)
	{
		Node node = null;
		Node node2 = null;
		while (true)
		{
			Node node3 = head;
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
			while (next.Marked || node3.Key < key);
			node2 = node3;
			if (node == node2)
			{
				if (node2 == tail || !node2.Next.Marked)
				{
					return node2;
				}
			}
			else if (Interlocked.CompareExchange(ref left.Next, node2, node) == node && (node2 == tail || !node2.Next.Marked))
			{
				break;
			}
		}
		return node2;
	}

	private bool ListDelete(int key, out T data)
	{
		Node node = null;
		Node node2 = null;
		Node left = null;
		data = default(T);
		do
		{
			node = ListSearch(key, ref left);
			if (node == tail || node.Key != key)
			{
				return false;
			}
			data = node.Data;
			node2 = node.Next;
		}
		while (node2.Marked || Interlocked.CompareExchange(ref node.Next, new Node(node2), node2) != node2);
		if (Interlocked.CompareExchange(ref left.Next, node2, node) != node2)
		{
			ListSearch(node.Key, ref left);
		}
		return true;
	}

	private bool ListPop(out T data)
	{
		Node node = null;
		Node node2 = null;
		Node left = head;
		data = default(T);
		do
		{
			node = head.Next;
			if (node == tail)
			{
				return false;
			}
			data = node.Data;
			node2 = node.Next;
		}
		while (node2.Marked || Interlocked.CompareExchange(ref node.Next, new Node(node2), node2) != node2);
		if (Interlocked.CompareExchange(ref left.Next, node2, node) != node2)
		{
			ListSearch(node.Key, ref left);
		}
		return true;
	}

	private bool ListInsert(Node newNode)
	{
		int key = newNode.Key;
		Node node = null;
		Node left = null;
		do
		{
			node = ListSearch(key, ref left);
			if (node != tail && node.Key == key)
			{
				return false;
			}
			newNode.Next = node;
		}
		while (Interlocked.CompareExchange(ref left.Next, newNode, node) != node);
		return true;
	}

	private bool ListFind(int key, out Node data)
	{
		Node node = null;
		Node left = null;
		data = null;
		node = (data = ListSearch(key, ref left));
		if (node != tail)
		{
			return node.Key == key;
		}
		return false;
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumeratorInternal();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumeratorInternal();
	}

	private IEnumerator<T> GetEnumeratorInternal()
	{
		for (Node node = head.Next; node != tail; node = node.Next)
		{
			while (node.Marked)
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

	void ICollection<T>.Add(T item)
	{
		TryAdd(item);
	}

	bool ICollection<T>.Remove(T item)
	{
		return TryRemove(item);
	}
}
