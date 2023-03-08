using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Concurrent
{
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
        private int count;

        public IEqualityComparer<T> Comparer { get; }

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
			this.Comparer = comparer;
			head = new Node();
			tail = new Node();
			head.Next = tail;
		}

		public bool TryAdd(T data)
		{
			Node node = new Node();
			node.Data = data;
			node.Key = Comparer.GetHashCode(data);
			if (ListInsert(node))
			{
				Interlocked.Increment(ref count);
				return true;
			}
			return false;
		}

		public bool TryRemove(T data)
		{
			T dummy;
			return TryRemoveHash(Comparer.GetHashCode(data), out dummy);
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
			return ContainsHash(Comparer.GetHashCode(data));
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
			if (!ListFind(key, out var node))
			{
				return false;
			}
			data = node.Data;
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
			Node leftNodeNext = null;
			Node rightNode = null;
			while (true)
			{
				Node t = head;
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
				while (tNext.Marked || t.Key < key);
				rightNode = t;
				if (leftNodeNext == rightNode)
				{
					if (rightNode == tail || !rightNode.Next.Marked)
					{
						return rightNode;
					}
				}
				else if (Interlocked.CompareExchange(ref left.Next, rightNode, leftNodeNext) == leftNodeNext && (rightNode == tail || !rightNode.Next.Marked))
				{
					break;
				}
			}
			return rightNode;
		}

		private bool ListDelete(int key, out T data)
		{
			Node rightNode = null;
			Node rightNodeNext = null;
			Node leftNode = null;
			data = default(T);
			do
			{
				rightNode = ListSearch(key, ref leftNode);
				if (rightNode == tail || rightNode.Key != key)
				{
					return false;
				}
				data = rightNode.Data;
				rightNodeNext = rightNode.Next;
			}
			while (rightNodeNext.Marked || Interlocked.CompareExchange(ref rightNode.Next, new Node(rightNodeNext), rightNodeNext) != rightNodeNext);
			if (Interlocked.CompareExchange(ref leftNode.Next, rightNodeNext, rightNode) != rightNodeNext)
			{
				ListSearch(rightNode.Key, ref leftNode);
			}
			return true;
		}

		private bool ListPop(out T data)
		{
			Node rightNode = null;
			Node rightNodeNext = null;
			Node leftNode = head;
			data = default(T);
			do
			{
				rightNode = head.Next;
				if (rightNode == tail)
				{
					return false;
				}
				data = rightNode.Data;
				rightNodeNext = rightNode.Next;
			}
			while (rightNodeNext.Marked || Interlocked.CompareExchange(ref rightNode.Next, new Node(rightNodeNext), rightNodeNext) != rightNodeNext);
			if (Interlocked.CompareExchange(ref leftNode.Next, rightNodeNext, rightNode) != rightNodeNext)
			{
				ListSearch(rightNode.Key, ref leftNode);
			}
			return true;
		}

		private bool ListInsert(Node newNode)
		{
			int key = newNode.Key;
			Node rightNode = null;
			Node leftNode = null;
			do
			{
				rightNode = ListSearch(key, ref leftNode);
				if (rightNode != tail && rightNode.Key == key)
				{
					return false;
				}
				newNode.Next = rightNode;
			}
			while (Interlocked.CompareExchange(ref leftNode.Next, newNode, rightNode) != rightNode);
			return true;
		}

		private bool ListFind(int key, out Node data)
		{
			Node rightNode = null;
			Node leftNode = null;
			data = null;
			rightNode = (data = ListSearch(key, ref leftNode));
			if (rightNode != tail)
			{
				return rightNode.Key == key;
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
}
