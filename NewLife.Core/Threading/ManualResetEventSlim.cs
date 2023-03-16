using System.Diagnostics;

namespace System.Threading;

[DebuggerDisplay("Set = {IsSet}")]
public class ManualResetEventSlim : IDisposable
{
	private readonly int spinCount;

	private ManualResetEvent handle;

	internal AtomicBooleanValue disposed;

	private bool used;

	private bool set;

	public bool IsSet => set;

	public int SpinCount => spinCount;

	public WaitHandle WaitHandle
	{
		get
		{
			ThrowIfDisposed();
			if (handle != null)
			{
				return handle;
			}
			bool flag = set;
			ManualResetEvent manualResetEvent = new ManualResetEvent(flag);
			if (Interlocked.CompareExchange(ref handle, manualResetEvent, null) == null)
			{
				if (flag != set)
				{
					if (set)
					{
						manualResetEvent.Set();
					}
					else
					{
						manualResetEvent.Reset();
					}
				}
			}
			else
			{
				manualResetEvent.Close();
				manualResetEvent = null;
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
		set = initialState;
		this.spinCount = spinCount;
	}

	public void Reset()
	{
		ThrowIfDisposed();
		set = false;
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
		set = true;
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
		if (!set)
		{
			SpinWait spinWait = default(SpinWait);
			while (!set)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (spinWait.Count >= spinCount)
				{
					break;
				}
				spinWait.SpinOnce();
			}
			if (set)
			{
				return true;
			}
			WaitHandle waitHandle = WaitHandle;
			if (cancellationToken.CanBeCanceled)
			{
				if (WaitHandle.WaitAny(new WaitHandle[2] { waitHandle, cancellationToken.WaitHandle }, millisecondsTimeout) == 0)
				{
					return false;
				}
				cancellationToken.ThrowIfCancellationRequested();
			}
			else if (!waitHandle.WaitOne(millisecondsTimeout))
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
		ManualResetEvent manualResetEvent = Interlocked.Exchange(ref handle, null);
		if (used)
		{
			SpinWait spinWait = default(SpinWait);
			while (used)
			{
				spinWait.SpinOnce();
			}
		}
		manualResetEvent.Close();
		manualResetEvent = null;
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
