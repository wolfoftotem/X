using System.Diagnostics;

namespace System.Threading
{
	[DebuggerDisplay("Set = {IsSet}")]
	public class ManualResetEventSlim : IDisposable
	{
        private ManualResetEvent handle;

		internal AtomicBooleanValue disposed;

		private bool used;

        public bool IsSet { get; private set; }

        public int SpinCount { get; }

        public WaitHandle WaitHandle
		{
			get
			{
				ThrowIfDisposed();
				if (handle != null)
				{
					return handle;
				}
				bool isSet = IsSet;
				ManualResetEvent mre = new ManualResetEvent(isSet);
				if (Interlocked.CompareExchange(ref handle, mre, null) == null)
				{
					if (isSet != IsSet)
					{
						if (IsSet)
						{
							mre.Set();
						}
						else
						{
							mre.Reset();
						}
					}
				}
				else
				{
					mre.Close();
					mre = null;
				}
				return handle;
			}
		}

		public ManualResetEventSlim()
			: this(initialState: false, 10)
		{
		}

		public ManualResetEventSlim(bool initialState)
			: this(initialState, 10)
		{
		}

		public ManualResetEventSlim(bool initialState, int spinCount)
		{
			if (spinCount < 0 || spinCount > 2047)
			{
				throw new ArgumentOutOfRangeException("spinCount");
			}
			IsSet = initialState;
			this.SpinCount = spinCount;
		}

		public void Reset()
		{
			ThrowIfDisposed();
			IsSet = false;
			Thread.MemoryBarrier();
			if (handle != null)
			{
				used = true;
				Thread.MemoryBarrier();
				handle?.Reset();
				Thread.MemoryBarrier();
				used = false;
			}
		}

		public void Set()
		{
			IsSet = true;
			Thread.MemoryBarrier();
			if (handle != null)
			{
				used = true;
				Thread.MemoryBarrier();
				handle?.Set();
				Thread.MemoryBarrier();
				used = false;
			}
		}

		public void Wait()
		{
			Wait(CancellationToken.None);
		}

		public bool Wait(int millisecondsTimeout)
		{
			return Wait(millisecondsTimeout, CancellationToken.None);
		}

		public bool Wait(TimeSpan timeout)
		{
			return Wait(CheckTimeout(timeout), CancellationToken.None);
		}

		public void Wait(CancellationToken cancellationToken)
		{
			Wait(-1, cancellationToken);
		}

		public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout");
			}
			ThrowIfDisposed();
			if (!IsSet)
			{
				SpinWait wait = default(SpinWait);
				while (!IsSet)
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (wait.Count >= SpinCount)
					{
						break;
					}
					wait.SpinOnce();
				}
				if (IsSet)
				{
					return true;
				}
				WaitHandle handle = WaitHandle;
				if (cancellationToken.CanBeCanceled)
				{
					if (WaitHandle.WaitAny(new WaitHandle[2] { handle, cancellationToken.WaitHandle }, millisecondsTimeout) == 0)
					{
						return false;
					}
					cancellationToken.ThrowIfCancellationRequested();
				}
				else if (!handle.WaitOne(millisecondsTimeout))
				{
					return false;
				}
			}
			return true;
		}

		public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
		{
			return Wait(CheckTimeout(timeout), cancellationToken);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed.TryRelaxedSet() || handle == null)
			{
				return;
			}
			ManualResetEvent tmpHandle = Interlocked.Exchange(ref handle, null);
			if (used)
			{
				SpinWait wait = default(SpinWait);
				while (used)
				{
					wait.SpinOnce();
				}
			}
			tmpHandle.Close();
			tmpHandle = null;
		}

		private void ThrowIfDisposed()
		{
			if (disposed.Value)
			{
				throw new ObjectDisposedException("ManualResetEventSlim");
			}
		}

		private static int CheckTimeout(TimeSpan timeout)
		{
			try
			{
				return checked((int)timeout.TotalMilliseconds);
			}
			catch (OverflowException)
			{
				throw new ArgumentOutOfRangeException("timeout");
			}
		}
	}
}
