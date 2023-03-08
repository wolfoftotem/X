using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Collections.Concurrent
{
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
			Node temp = pool.Take();
			temp.Value = item;
			do
			{
				temp.Next = head;
			}
			while (Interlocked.CompareExchange(ref head, temp, temp.Next) != temp.Next);
			Interlocked.Increment(ref count);
		}

		public void PushRange(T[] items)
		{
			PushRange(items, 0, items.Length);
		}

		public void PushRange(T[] items, int startIndex, int count)
		{
			Node insert = null;
			Node first = null;
			for (int i = startIndex; i < count; i++)
			{
				Node temp = pool.Take();
				temp.Value = items[i];
				temp.Next = insert;
				insert = temp;
				if (first == null)
				{
					first = temp;
				}
			}
			do
			{
				first.Next = head;
			}
			while (Interlocked.CompareExchange(ref head, insert, first.Next) != first.Next);
			Interlocked.Add(ref count, count);
		}

		public bool TryPop(out T result)
		{
			Node temp;
			do
			{
				temp = head;
				if (temp == null)
				{
					result = default(T);
					return false;
				}
			}
			while (Interlocked.CompareExchange(ref head, temp.Next, temp) != temp);
			Interlocked.Decrement(ref count);
			result = temp.Value;
			pool.Release(ZeroOut(temp));
			return true;
		}

		public int TryPopRange(T[] items)
		{
			return TryPopRange(items, 0, items.Length);
		}

		public int TryPopRange(T[] items, int startIndex, int count)
		{
			Node temp;
			Node end;
			do
			{
				temp = head;
				if (temp == null)
				{
					return -1;
				}
				end = temp;
				for (int j = 0; j < count - 1; j++)
				{
					end = end.Next;
					if (end == null)
					{
						break;
					}
				}
			}
			while (Interlocked.CompareExchange(ref head, end, temp) != temp);
			int i;
			for (i = startIndex; i < count; i++)
			{
				if (temp == null)
				{
					break;
				}
				items[i] = temp.Value;
				end = temp;
				temp = temp.Next;
				pool.Release(ZeroOut(end));
			}
			return i - 1;
		}

		public bool TryPeek(out T result)
		{
			Node myHead = head;
			if (myHead == null)
			{
				result = default(T);
				return false;
			}
			result = myHead.Value;
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
			T[] dest = array as T[];
			if (dest != null)
			{
				CopyTo(dest, index);
			}
		}

		public void CopyTo(T[] array, int index)
		{
			IEnumerator<T> e = InternalGetEnumerator();
			int i = index;
			while (e.MoveNext())
			{
				array[i++] = e.Current;
			}
		}

		bool IProducerConsumerCollection<T>.TryTake(out T item)
		{
			return TryPop(out item);
		}

		public T[] ToArray()
		{
			T[] dest = new T[count];
			CopyTo(dest, 0);
			return dest;
		}
	}
}
