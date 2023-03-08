using System.Collections.Concurrent;

namespace System.Threading.Tasks
{
	internal struct TaskCompletionQueue<TCompletion> where TCompletion : class
	{
		private TCompletion single;

		private ConcurrentOrderedList<TCompletion> completed;

		public bool HasElements
		{
			get
			{
				if (single == null)
				{
					if (completed != null)
					{
						return completed.Count != 0;
					}
					return false;
				}
				return true;
			}
		}

		public void Add(TCompletion continuation)
		{
			if (single != null || Interlocked.CompareExchange(ref single, continuation, null) != null)
			{
				if (completed == null)
				{
					Interlocked.CompareExchange(ref completed, new ConcurrentOrderedList<TCompletion>(), null);
				}
				completed.TryAdd(continuation);
			}
		}

		public bool Remove(TCompletion continuation)
		{
			TCompletion temp = single;
			if (temp != null && temp == continuation && Interlocked.CompareExchange(ref single, null, continuation) == continuation)
			{
				return true;
			}
			if (completed != null)
			{
				return completed.TryRemove(continuation);
			}
			return false;
		}

		public bool TryGetNextCompletion(out TCompletion continuation)
		{
			continuation = null;
			if (single != null && (continuation = Interlocked.Exchange(ref single, null)) != null)
			{
				return true;
			}
			if (completed != null)
			{
				return completed.TryPop(out continuation);
			}
			return false;
		}
	}
}
