using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Concurrent
{
	[ComVisible(false)]
	[DebuggerDisplay("Count={Count}")]
	public class BlockingCollection<T> : IEnumerable<T>, ICollection, IEnumerable, IDisposable
	{
		private readonly IProducerConsumerCollection<T> underlyingColl;

		private readonly int upperBound;

		private AtomicBoolean isComplete;

		private long completeId;

		private int addId = int.MinValue;

		private int removeId = int.MinValue;

		[ThreadStatic]
		private SpinWait sw;

		public int BoundedCapacity => upperBound;

		public int Count => underlyingColl.Count;

		public bool IsAddingCompleted => isComplete.Value;

		public bool IsCompleted
		{
			get
			{
				if (isComplete.Value)
				{
					return addId == removeId;
				}
				return false;
			}
		}

		object ICollection.SyncRoot => underlyingColl.SyncRoot;

		bool ICollection.IsSynchronized => underlyingColl.IsSynchronized;

		public BlockingCollection()
			: this((IProducerConsumerCollection<T>)new ConcurrentQueue<T>(), -1)
		{
		}

		public BlockingCollection(int upperBound)
			: this((IProducerConsumerCollection<T>)new ConcurrentQueue<T>(), upperBound)
		{
		}

		public BlockingCollection(IProducerConsumerCollection<T> underlyingColl)
			: this(underlyingColl, -1)
		{
		}

		public BlockingCollection(IProducerConsumerCollection<T> underlyingColl, int upperBound)
		{
			this.underlyingColl = underlyingColl;
			this.upperBound = upperBound;
			isComplete = new AtomicBoolean();
		}

		public void Add(T item)
		{
			Add(item, null);
		}

		public void Add(T item, CancellationToken token)
		{
			Add(item, () => token.IsCancellationRequested);
		}

		private void Add(T item, Func<bool> cancellationFunc)
		{
			while (true)
			{
				int cachedAddId = addId;
				int cachedRemoveId = removeId;
				if (upperBound != -1 && cachedAddId - cachedRemoveId > upperBound)
				{
					Block();
					continue;
				}
				if (isComplete.Value && cachedAddId >= completeId)
				{
					throw new InvalidOperationException("The BlockingCollection<T> has been marked as complete with regards to additions.");
				}
				if (Interlocked.CompareExchange(ref addId, cachedAddId + 1, cachedAddId) == cachedAddId)
				{
					break;
				}
				if (cancellationFunc != null && cancellationFunc())
				{
					throw new OperationCanceledException("CancellationToken triggered");
				}
			}
			if (!underlyingColl.TryAdd(item))
			{
				throw new InvalidOperationException("The underlying collection didn't accept the item.");
			}
		}

		public T Take()
		{
			return Take(null);
		}

		public T Take(CancellationToken token)
		{
			return Take(() => token.IsCancellationRequested);
		}

		private T Take(Func<bool> cancellationFunc)
		{
			while (true)
			{
				int cachedRemoveId = removeId;
				int cachedAddId = addId;
				if (cachedRemoveId == cachedAddId)
				{
					if (IsCompleted)
					{
						throw new OperationCanceledException("The BlockingCollection<T> has been marked as complete with regards to additions.");
					}
					Block();
					continue;
				}
				if (Interlocked.CompareExchange(ref removeId, cachedRemoveId + 1, cachedRemoveId) == cachedRemoveId)
				{
					break;
				}
				if (cancellationFunc == null || !cancellationFunc())
				{
					continue;
				}
				throw new OperationCanceledException("The CancellationToken has had cancellation requested.");
			}
			T item;
			while (!underlyingColl.TryTake(out item))
			{
			}
			return item;
		}

		public bool TryAdd(T item)
		{
			return TryAdd(item, null, null);
		}

		private bool TryAdd(T item, Func<bool> contFunc, CancellationToken? token)
		{
			do
			{
				if (token.HasValue && token.Value.IsCancellationRequested)
				{
					throw new OperationCanceledException("The CancellationToken has had cancellation requested.");
				}
				int cachedAddId = addId;
				int cachedRemoveId = removeId;
				if (upperBound != -1 && cachedAddId - cachedRemoveId > upperBound)
				{
					continue;
				}
				if (isComplete.Value && cachedAddId >= completeId)
				{
					throw new InvalidOperationException("The BlockingCollection<T> has been marked as complete with regards to additions.");
				}
				if (Interlocked.CompareExchange(ref addId, cachedAddId + 1, cachedAddId) == cachedAddId)
				{
					if (!underlyingColl.TryAdd(item))
					{
						throw new InvalidOperationException("The underlying collection didn't accept the item.");
					}
					return true;
				}
			}
			while (contFunc != null && contFunc());
			return false;
		}

		public bool TryAdd(T item, TimeSpan ts)
		{
			return TryAdd(item, (int)ts.TotalMilliseconds);
		}

		public bool TryAdd(T item, int millisecondsTimeout)
		{
			Watch stopwatch = Watch.StartNew();
			return TryAdd(item, () => stopwatch.ElapsedMilliseconds < millisecondsTimeout, null);
		}

		public bool TryAdd(T item, int millisecondsTimeout, CancellationToken token)
		{
			Watch stopwatch = Watch.StartNew();
			return TryAdd(item, () => stopwatch.ElapsedMilliseconds < millisecondsTimeout, token);
		}

		public bool TryTake(out T item)
		{
			return TryTake(out item, null, null);
		}

		private bool TryTake(out T item, Func<bool> contFunc, CancellationToken? token)
		{
			item = default(T);
			do
			{
				if (token.HasValue && token.Value.IsCancellationRequested)
				{
					throw new OperationCanceledException("The CancellationToken has had cancellation requested.");
				}
				int cachedRemoveId = removeId;
				int cachedAddId = addId;
				if (cachedRemoveId == cachedAddId)
				{
					if (IsCompleted)
					{
						return false;
					}
				}
				else if (Interlocked.CompareExchange(ref removeId, cachedRemoveId + 1, cachedRemoveId) == cachedRemoveId)
				{
					return underlyingColl.TryTake(out item);
				}
			}
			while (contFunc != null && contFunc());
			return false;
		}

		public bool TryTake(out T item, TimeSpan ts)
		{
			return TryTake(out item, (int)ts.TotalMilliseconds);
		}

		public bool TryTake(out T item, int millisecondsTimeout)
		{
			item = default(T);
			Watch sw = Watch.StartNew();
			return TryTake(out item, () => sw.ElapsedMilliseconds < millisecondsTimeout, null);
		}

		public bool TryTake(out T item, int millisecondsTimeout, CancellationToken token)
		{
			item = default(T);
			Watch sw = Watch.StartNew();
			return TryTake(out item, () => sw.ElapsedMilliseconds < millisecondsTimeout, token);
		}

		private static void CheckArray(BlockingCollection<T>[] collections)
		{
			if (collections == null)
			{
				throw new ArgumentNullException("collections");
			}
			if (collections.Length == 0 || IsThereANullElement(collections))
			{
				throw new ArgumentException("The collections argument is a 0-length array or contains a null element.", "collections");
			}
		}

		private static bool IsThereANullElement(BlockingCollection<T>[] collections)
		{
			foreach (BlockingCollection<T> e in collections)
			{
				if (e == null)
				{
					return true;
				}
			}
			return false;
		}

		public static int AddToAny(BlockingCollection<T>[] collections, T item)
		{
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				try
				{
					coll.Add(item);
					return index;
				}
				catch
				{
				}
				index++;
			}
			return -1;
		}

		public static int AddToAny(BlockingCollection<T>[] collections, T item, CancellationToken token)
		{
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				try
				{
					coll.Add(item, token);
					return index;
				}
				catch
				{
				}
				index++;
			}
			return -1;
		}

		public static int TryAddToAny(BlockingCollection<T>[] collections, T item)
		{
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				if (coll.TryAdd(item))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static int TryAddToAny(BlockingCollection<T>[] collections, T item, TimeSpan ts)
		{
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				if (coll.TryAdd(item, ts))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static int TryAddToAny(BlockingCollection<T>[] collections, T item, int millisecondsTimeout)
		{
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				if (coll.TryAdd(item, millisecondsTimeout))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static int TryAddToAny(BlockingCollection<T>[] collections, T item, int millisecondsTimeout, CancellationToken token)
		{
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				if (coll.TryAdd(item, millisecondsTimeout, token))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static int TakeFromAny(BlockingCollection<T>[] collections, out T item)
		{
			item = default(T);
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				try
				{
					item = coll.Take();
					return index;
				}
				catch
				{
				}
				index++;
			}
			return -1;
		}

		public static int TakeFromAny(BlockingCollection<T>[] collections, out T item, CancellationToken token)
		{
			item = default(T);
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				try
				{
					item = coll.Take(token);
					return index;
				}
				catch
				{
				}
				index++;
			}
			return -1;
		}

		public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T item)
		{
			item = default(T);
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				if (coll.TryTake(out item))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T item, TimeSpan ts)
		{
			item = default(T);
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				if (coll.TryTake(out item, ts))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T item, int millisecondsTimeout)
		{
			item = default(T);
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				if (coll.TryTake(out item, millisecondsTimeout))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T item, int millisecondsTimeout, CancellationToken token)
		{
			item = default(T);
			CheckArray(collections);
			int index = 0;
			foreach (BlockingCollection<T> coll in collections)
			{
				if (coll.TryTake(out item, millisecondsTimeout, token))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public void CompleteAdding()
		{
			completeId = addId;
			isComplete.Value = true;
		}

		void ICollection.CopyTo(Array array, int index)
		{
			underlyingColl.CopyTo(array, index);
		}

		public void CopyTo(T[] array, int index)
		{
			underlyingColl.CopyTo(array, index);
		}

		public IEnumerable<T> GetConsumingEnumerable()
		{
			return GetConsumingEnumerable(Take);
		}

		public IEnumerable<T> GetConsumingEnumerable(CancellationToken token)
		{
			return GetConsumingEnumerable(() => Take(token));
		}

		private IEnumerable<T> GetConsumingEnumerable(Func<T> getFunc)
		{
			while (true)
			{
				T item;
				try
				{
					item = getFunc();
				}
				catch
				{
					yield break;
				}
				yield return item;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)underlyingColl).GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return underlyingColl.GetEnumerator();
		}

		public void Dispose()
		{
		}

		protected virtual void Dispose(bool managedRes)
		{
		}

		public T[] ToArray()
		{
			return underlyingColl.ToArray();
		}

		private void Block()
		{
			sw.SpinOnce();
		}
	}
}
