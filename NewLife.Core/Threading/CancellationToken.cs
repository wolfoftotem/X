using System.Diagnostics;

namespace System.Threading;

[DebuggerDisplay("IsCancellationRequested = {IsCancellationRequested}")]
public struct CancellationToken
{
	private readonly CancellationTokenSource source;

	public static CancellationToken None => default(CancellationToken);

	public bool CanBeCanceled => source != null;

	public bool IsCancellationRequested => Source.IsCancellationRequested;

	public WaitHandle WaitHandle => Source.WaitHandle;

	private CancellationTokenSource Source => source ?? CancellationTokenSource.NoneSource;

	public CancellationToken(bool canceled)
		: this(canceled ? CancellationTokenSource.CanceledSource : null)
	{
	}

	internal CancellationToken(CancellationTokenSource source)
	{
		this.source = source;
	}

	public CancellationTokenRegistration Register(Action callback)
	{
		return Register(callback, useSynchronizationContext: false);
	}

	public CancellationTokenRegistration Register(Action callback, bool useSynchronizationContext)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		return Source.Register(callback, useSynchronizationContext);
	}

	public CancellationTokenRegistration Register(Action<object> callback, object state)
	{
		return Register(callback, state, useSynchronizationContext: false);
	}

	public CancellationTokenRegistration Register(Action<object> callback, object state, bool useSynchronizationContext)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		return Register(delegate
		{
			callback(state);
		}, useSynchronizationContext);
	}

	public void ThrowIfCancellationRequested()
	{
		if (Source.IsCancellationRequested)
		{
			throw new OperationCanceledException(this);
		}
	}

	public bool Equals(CancellationToken other)
	{
		return Source == other.Source;
	}

	public override bool Equals(object other)
	{
		if (!(other is CancellationToken))
		{
			return false;
		}
		return Equals((CancellationToken)other);
	}

	public override int GetHashCode()
	{
		return Source.GetHashCode();
	}

	public static bool operator ==(CancellationToken left, CancellationToken right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CancellationToken left, CancellationToken right)
	{
		return !left.Equals(right);
	}
}
