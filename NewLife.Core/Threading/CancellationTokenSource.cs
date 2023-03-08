using System.Collections.Generic;

namespace System.Threading
{
	public class CancellationTokenSource : IDisposable
	{
        private bool processed;

		private bool disposed;

		private int currId = int.MinValue;

		private Dictionary<CancellationTokenRegistration, Action> callbacks;

		private ManualResetEvent handle;

		private readonly object syncRoot = new object();

		internal static readonly CancellationTokenSource NoneSource;

		internal static readonly CancellationTokenSource CanceledSource;

		private static readonly TimerCallback timer_callback;

		private Timer timer;

		public CancellationToken Token
		{
			get
			{
				CheckDisposed();
				return new CancellationToken(this);
			}
		}

        public bool IsCancellationRequested { get; private set; }

        internal WaitHandle WaitHandle
		{
			get
			{
				CheckDisposed();
				return handle;
			}
		}

		static CancellationTokenSource()
		{
			NoneSource = new CancellationTokenSource();
			CanceledSource = new CancellationTokenSource();
			CanceledSource.processed = true;
			CanceledSource.IsCancellationRequested = true;
			timer_callback = delegate(object token)
			{
				CancellationTokenSource cancellationTokenSource = (CancellationTokenSource)token;
				cancellationTokenSource.Cancel();
			};
		}

		public CancellationTokenSource()
		{
			callbacks = new Dictionary<CancellationTokenRegistration, Action>();
			handle = new ManualResetEvent(initialState: false);
		}

		public CancellationTokenSource(int millisecondsDelay)
			: this()
		{
			if (millisecondsDelay < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsDelay");
			}
			if (millisecondsDelay != -1)
			{
				timer = new Timer(timer_callback, this, millisecondsDelay, -1);
			}
		}

		public CancellationTokenSource(TimeSpan delay)
			: this(CheckTimeout(delay))
		{
		}

		public void Cancel()
		{
			Cancel(throwOnFirstException: false);
		}

		public void Cancel(bool throwOnFirstException)
		{
			CheckDisposed();
			IsCancellationRequested = true;
			handle.Set();
			List<Exception> exceptions = null;
			lock (syncRoot)
			{
				try
				{
					foreach (KeyValuePair<CancellationTokenRegistration, Action> item in callbacks)
					{
						if (throwOnFirstException)
						{
							item.Value();
							continue;
						}
						try
						{
							item.Value();
						}
						catch (Exception e)
						{
							if (exceptions == null)
							{
								exceptions = new List<Exception>();
							}
							exceptions.Add(e);
						}
					}
				}
				finally
				{
					callbacks.Clear();
				}
			}
			Thread.MemoryBarrier();
			processed = true;
			if (exceptions != null)
			{
				throw new AggregateException(exceptions);
			}
		}

		public void CancelAfter(TimeSpan delay)
		{
			CancelAfter(CheckTimeout(delay));
		}

		public void CancelAfter(int millisecondsDelay)
		{
			if (millisecondsDelay < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsDelay");
			}
			CheckDisposed();
			if (IsCancellationRequested || millisecondsDelay == -1)
			{
				return;
			}
			if (timer == null)
			{
				Timer t = new Timer(timer_callback, this, -1, -1);
				if (Interlocked.CompareExchange(ref timer, t, null) != null)
				{
					t.Dispose();
				}
			}
			timer.Change(millisecondsDelay, -1);
		}

		public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token1, CancellationToken token2)
		{
			return CreateLinkedTokenSource(new CancellationToken[2] { token1, token2 });
		}

		public static CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
		{
			if (tokens == null)
			{
				throw new ArgumentNullException("tokens");
			}
			if (tokens.Length == 0)
			{
				throw new ArgumentException("Empty tokens array");
			}
			CancellationTokenSource src = new CancellationTokenSource();
			Action action = src.Cancel;
			for (int i = 0; i < tokens.Length; i++)
			{
				CancellationToken token = tokens[i];
				if (token.CanBeCanceled)
				{
					token.Register(action);
				}
			}
			return src;
		}

		private static int CheckTimeout(TimeSpan delay)
		{
			try
			{
				return checked((int)delay.TotalMilliseconds);
			}
			catch (OverflowException)
			{
				throw new ArgumentOutOfRangeException("delay");
			}
		}

		private void CheckDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				callbacks = null;
				if (timer != null)
				{
					timer.Dispose();
				}
				handle.Close();
				handle = null;
			}
		}

		internal CancellationTokenRegistration Register(Action callback, bool useSynchronizationContext)
		{
			CheckDisposed();
			CancellationTokenRegistration tokenReg = new CancellationTokenRegistration(Interlocked.Increment(ref currId), this);
			if (IsCancellationRequested)
			{
				callback();
			}
			else
			{
				bool temp = false;
				lock (syncRoot)
				{
					if (!(temp = IsCancellationRequested))
					{
						callbacks.Add(tokenReg, callback);
					}
				}
				if (temp)
				{
					callback();
				}
			}
			return tokenReg;
		}

		internal void RemoveCallback(CancellationTokenRegistration tokenReg)
		{
			if (!IsCancellationRequested)
			{
				lock (syncRoot)
				{
					if (!IsCancellationRequested)
					{
						callbacks.Remove(tokenReg);
						return;
					}
				}
			}
			SpinWait sw = default(SpinWait);
			while (!processed)
			{
				sw.SpinOnce();
			}
		}
	}
}
