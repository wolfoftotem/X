using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Collections.Concurrent
{
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
			Node oldTail = null;
			Node oldNext = null;
			bool update = false;
			while (!update)
			{
				oldTail = tail;
				oldNext = oldTail.Next;
				if (tail == oldTail)
				{
					if (oldNext == null)
					{
						update = Interlocked.CompareExchange(ref tail.Next, node, null) == null;
					}
					else
					{
						Interlocked.CompareExchange(ref tail, oldNext, oldTail);
					}
				}
			}
			Interlocked.CompareExchange(ref tail, node, oldTail);
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
			bool advanced = false;
			while (!advanced)
			{
				Node oldHead = head;
				Node oldTail = tail;
				Node oldNext = oldHead.Next;
				if (oldHead != head)
				{
					continue;
				}
				if (oldHead == oldTail)
				{
					if (oldNext != null)
					{
						Interlocked.CompareExchange(ref tail, oldNext, oldTail);
					}
					result = default(T);
					return false;
				}
				result = oldNext.Value;
				advanced = Interlocked.CompareExchange(ref head, oldNext, oldHead) == oldHead;
				if (advanced)
				{
					pool.Release(ZeroOut(oldHead));
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
			Node first = head.Next;
			result = first.Value;
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

		public T[] ToArray()
		{
			T[] dest = new T[count];
			CopyTo(dest, 0);
			return dest;
		}

		bool IProducerConsumerCollection<T>.TryTake(out T item)
		{
			return TryDequeue(out item);
		}
	}
}
