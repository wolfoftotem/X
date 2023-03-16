using System.Collections.Generic;

namespace System.Threading;

public class CancellationTokenSource : IDisposable
{
	private bool canceled;

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

	public bool IsCancellationRequested => canceled;

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
		CanceledSource.canceled = true;
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
		canceled = true;
		handle.Set();
		List<Exception> list = null;
		lock (syncRoot)
		{
			try
			{
				foreach (KeyValuePair<CancellationTokenRegistration, Action> callback in callbacks)
				{
					if (throwOnFirstException)
					{
						callback.Value();
						continue;
					}
					try
					{
						callback.Value();
					}
					catch (Exception item)
					{
						if (list == null)
						{
							list = new List<Exception>();
						}
						list.Add(item);
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
		if (list != null)
		{
			throw new AggregateException(list);
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
		if (canceled || millisecondsDelay == -1)
		{
			return;
		}
		if (this.timer == null)
		{
			Timer timer = new Timer(timer_callback, this, -1, -1);
			if (Interlocked.CompareExchange(ref this.timer, timer, null) != null)
			{
				timer.Dispose();
			}
		}
		this.timer.Change(millisecondsDelay, -1);
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
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		Action callback = cancellationTokenSource.Cancel;
		for (int i = 0; i < tokens.Length; i++)
		{
			CancellationToken cancellationToken = tokens[i];
			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(callback);
			}
		}
		return cancellationTokenSource;
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
		CancellationTokenRegistration cancellationTokenRegistration = new CancellationTokenRegistration(Interlocked.Increment(ref currId), this);
		if (canceled)
		{
			callback();
		}
		else
		{
			bool flag = false;
			lock (syncRoot)
			{
				if (!(flag = canceled))
				{
					callbacks.Add(cancellationTokenRegistration, callback);
				}
			}
			if (flag)
			{
				callback();
			}
		}
		return cancellationTokenRegistration;
	}

	internal void RemoveCallback(CancellationTokenRegistration tokenReg)
	{
		if (!canceled)
		{
			lock (syncRoot)
			{
				if (!canceled)
				{
					callbacks.Remove(tokenReg);
					return;
				}
			}
		}
		SpinWait spinWait = default(SpinWait);
		while (!processed)
		{
			spinWait.SpinOnce();
		}
	}
}
