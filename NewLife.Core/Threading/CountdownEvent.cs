using System.Diagnostics;

namespace System.Threading
{
	[DebuggerDisplay("Initial Count={InitialCount}, Current Count={CurrentCount}")]
	public class CountdownEvent : IDisposable
	{
		private int initialCount;

		private int initial;

		private ManualResetEventSlim evt;

		public int CurrentCount => initialCount;

		public int InitialCount => initial;

		public bool IsSet => initialCount == 0;

		public WaitHandle WaitHandle => evt.WaitHandle;

		public CountdownEvent(int initialCount)
		{
			if (initialCount < 0)
			{
				throw new ArgumentOutOfRangeException("initialCount");
			}
			evt = new ManualResetEventSlim(initialCount == 0);
			initial = (this.initialCount = initialCount);
		}

		public bool Signal()
		{
			return Signal(1);
		}

		public bool Signal(int signalCount)
		{
			if (signalCount <= 0)
			{
				throw new ArgumentOutOfRangeException("signalCount");
			}
			CheckDisposed();
			if (!ApplyOperation(-signalCount, out var newValue))
			{
				throw new InvalidOperationException("The event is already set");
			}
			if (newValue == 0)
			{
				evt.Set();
				return true;
			}
			return false;
		}

		public void AddCount()
		{
			AddCount(1);
		}

		public void AddCount(int signalCount)
		{
			if (!TryAddCount(signalCount))
			{
				throw new InvalidOperationException("The event is already signaled and cannot be incremented");
			}
		}

		public bool TryAddCount()
		{
			return TryAddCount(1);
		}

		public bool TryAddCount(int signalCount)
		{
			if (signalCount <= 0)
			{
				throw new ArgumentOutOfRangeException("signalCount");
			}
			CheckDisposed();
			int temp;
			return ApplyOperation(signalCount, out temp);
		}

		private bool ApplyOperation(int num, out int newValue)
		{
			int oldCount;
			do
			{
				oldCount = initialCount;
				if (oldCount == 0)
				{
					newValue = 0;
					return false;
				}
				newValue = oldCount + num;
				if (newValue < 0)
				{
					return false;
				}
			}
			while (Interlocked.CompareExchange(ref initialCount, newValue, oldCount) != oldCount);
			return true;
		}

		public void Wait()
		{
			evt.Wait();
		}

		public void Wait(CancellationToken cancellationToken)
		{
			evt.Wait(cancellationToken);
		}

		public bool Wait(int millisecondsTimeout)
		{
			return evt.Wait(millisecondsTimeout);
		}

		public bool Wait(TimeSpan timeout)
		{
			return evt.Wait(timeout);
		}

		public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
		{
			return evt.Wait(millisecondsTimeout, cancellationToken);
		}

		public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
		{
			return evt.Wait(timeout, cancellationToken);
		}

		public void Reset()
		{
			Reset(initial);
		}

		public void Reset(int count)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			CheckDisposed();
			initialCount = (initial = count);
			if (count == 0)
			{
				evt.Set();
			}
			else
			{
				evt.Reset();
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				evt.Dispose();
			}
		}

		private void CheckDisposed()
		{
			if (evt.disposed.Value)
			{
				throw new ObjectDisposedException("CountdownEvent");
			}
		}
	}
}
