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
        private AtomicBoolean isComplete;

		private Int64 completeId;

		private Int32 addId = Int32.MinValue;

		private Int32 removeId = Int32.MinValue;

		[ThreadStatic]
		private SpinWait sw;

        public Int32 BoundedCapacity { get; }

        public Int32 Count => underlyingColl.Count;

		public Boolean IsAddingCompleted => isComplete.Value;

		public Boolean IsCompleted
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

        Object ICollection.SyncRoot => underlyingColl.SyncRoot;

        Boolean ICollection.IsSynchronized => underlyingColl.IsSynchronized;

		public BlockingCollection()
			: this((IProducerConsumerCollection<T>)new ConcurrentQueue<T>(), -1)
		{
		}

		public BlockingCollection(Int32 upperBound)
			: this((IProducerConsumerCollection<T>)new ConcurrentQueue<T>(), upperBound)
		{
		}

		public BlockingCollection(IProducerConsumerCollection<T> underlyingColl)
			: this(underlyingColl, -1)
		{
		}

		public BlockingCollection(IProducerConsumerCollection<T> underlyingColl, Int32 upperBound)
		{
			this.underlyingColl = underlyingColl;
			this.BoundedCapacity = upperBound;
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

		private void Add(T item, Func<Boolean> cancellationFunc)
		{
			while (true)
			{
				var cachedAddId = addId;
				var cachedRemoveId = removeId;
				if (BoundedCapacity != -1 && cachedAddId - cachedRemoveId > BoundedCapacity)
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

		private T Take(Func<Boolean> cancellationFunc)
		{
			while (true)
			{
				var cachedRemoveId = removeId;
				var cachedAddId = addId;
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

		public Boolean TryAdd(T item)
		{
			return TryAdd(item, null, null);
		}

		private Boolean TryAdd(T item, Func<Boolean> contFunc, CancellationToken? token)
		{
			do
			{
				if (token.HasValue && token.Value.IsCancellationRequested)
				{
					throw new OperationCanceledException("The CancellationToken has had cancellation requested.");
				}
				var cachedAddId = addId;
				var cachedRemoveId = removeId;
				if (BoundedCapacity != -1 && cachedAddId - cachedRemoveId > BoundedCapacity)
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

		public Boolean TryAdd(T item, TimeSpan ts)
		{
			return TryAdd(item, (Int32)ts.TotalMilliseconds);
		}

		public Boolean TryAdd(T item, Int32 millisecondsTimeout)
		{
			var stopwatch = Watch.StartNew();
			return TryAdd(item, () => stopwatch.ElapsedMilliseconds < millisecondsTimeout, null);
		}

		public Boolean TryAdd(T item, Int32 millisecondsTimeout, CancellationToken token)
		{
			var stopwatch = Watch.StartNew();
			return TryAdd(item, () => stopwatch.ElapsedMilliseconds < millisecondsTimeout, token);
		}

		public Boolean TryTake(out T item)
		{
			return TryTake(out item, null, null);
		}

		private Boolean TryTake(out T item, Func<Boolean> contFunc, CancellationToken? token)
		{
			item = default(T);
			do
			{
				if (token.HasValue && token.Value.IsCancellationRequested)
				{
					throw new OperationCanceledException("The CancellationToken has had cancellation requested.");
				}
				var cachedRemoveId = removeId;
				var cachedAddId = addId;
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

		public Boolean TryTake(out T item, TimeSpan ts)
		{
			return TryTake(out item, (Int32)ts.TotalMilliseconds);
		}

		public Boolean TryTake(out T item, Int32 millisecondsTimeout)
		{
			item = default(T);
			var sw = Watch.StartNew();
			return TryTake(out item, () => sw.ElapsedMilliseconds < millisecondsTimeout, null);
		}

		public Boolean TryTake(out T item, Int32 millisecondsTimeout, CancellationToken token)
		{
			item = default(T);
			var sw = Watch.StartNew();
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

		private static Boolean IsThereANullElement(BlockingCollection<T>[] collections)
		{
			foreach (var e in collections)
			{
				if (e == null)
				{
					return true;
				}
			}
			return false;
		}

		public static Int32 AddToAny(BlockingCollection<T>[] collections, T item)
		{
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
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

		public static Int32 AddToAny(BlockingCollection<T>[] collections, T item, CancellationToken token)
		{
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
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

		public static Int32 TryAddToAny(BlockingCollection<T>[] collections, T item)
		{
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
			{
				if (coll.TryAdd(item))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static Int32 TryAddToAny(BlockingCollection<T>[] collections, T item, TimeSpan ts)
		{
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
			{
				if (coll.TryAdd(item, ts))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static Int32 TryAddToAny(BlockingCollection<T>[] collections, T item, Int32 millisecondsTimeout)
		{
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
			{
				if (coll.TryAdd(item, millisecondsTimeout))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static Int32 TryAddToAny(BlockingCollection<T>[] collections, T item, Int32 millisecondsTimeout, CancellationToken token)
		{
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
			{
				if (coll.TryAdd(item, millisecondsTimeout, token))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static Int32 TakeFromAny(BlockingCollection<T>[] collections, out T item)
		{
			item = default(T);
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
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

		public static Int32 TakeFromAny(BlockingCollection<T>[] collections, out T item, CancellationToken token)
		{
			item = default(T);
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
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

		public static Int32 TryTakeFromAny(BlockingCollection<T>[] collections, out T item)
		{
			item = default(T);
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
			{
				if (coll.TryTake(out item))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static Int32 TryTakeFromAny(BlockingCollection<T>[] collections, out T item, TimeSpan ts)
		{
			item = default(T);
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
			{
				if (coll.TryTake(out item, ts))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static Int32 TryTakeFromAny(BlockingCollection<T>[] collections, out T item, Int32 millisecondsTimeout)
		{
			item = default(T);
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
			{
				if (coll.TryTake(out item, millisecondsTimeout))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static Int32 TryTakeFromAny(BlockingCollection<T>[] collections, out T item, Int32 millisecondsTimeout, CancellationToken token)
		{
			item = default(T);
			CheckArray(collections);
			var index = 0;
			foreach (var coll in collections)
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

		void ICollection.CopyTo(Array array, Int32 index)
		{
			underlyingColl.CopyTo(array, index);
		}

		public void CopyTo(T[] array, Int32 index)
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

		protected virtual void Dispose(Boolean managedRes)
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
