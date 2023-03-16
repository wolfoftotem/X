using System.Collections.Generic;

namespace System.Threading.Tasks;

public class TaskCompletionSource<TResult>
{
	private readonly Task<TResult> source;

	public Task<TResult> Task => source;

	public TaskCompletionSource()
		: this((object)null, TaskCreationOptions.None)
	{
	}

	public TaskCompletionSource(object state)
		: this(state, TaskCreationOptions.None)
	{
	}

	public TaskCompletionSource(TaskCreationOptions creationOptions)
		: this((object)null, creationOptions)
	{
	}

	public TaskCompletionSource(object state, TaskCreationOptions creationOptions)
	{
		if ((creationOptions & (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning)) != 0)
		{
			throw new ArgumentOutOfRangeException("creationOptions");
		}
		source = new Task<TResult>(TaskActionInvoker.Empty, state, CancellationToken.None, creationOptions, null);
		source.SetupScheduler(TaskScheduler.Current);
	}

	public void SetCanceled()
	{
		if (!TrySetCanceled())
		{
			ThrowInvalidException();
		}
	}

	public void SetException(Exception exception)
	{
		if (exception == null)
		{
			throw new ArgumentNullException("exception");
		}
		SetException(new Exception[1] { exception });
	}

	public void SetException(IEnumerable<Exception> exceptions)
	{
		if (!TrySetException(exceptions))
		{
			ThrowInvalidException();
		}
	}

	public void SetResult(TResult result)
	{
		if (!TrySetResult(result))
		{
			ThrowInvalidException();
		}
	}

	private static void ThrowInvalidException()
	{
		throw new InvalidOperationException("The underlying Task is already in one of the three final states: RanToCompletion, Faulted, or Canceled.");
	}

	public bool TrySetCanceled()
	{
		return source.TrySetCanceled();
	}

	public bool TrySetException(Exception exception)
	{
		if (exception == null)
		{
			throw new ArgumentNullException("exception");
		}
		return TrySetException(new Exception[1] { exception });
	}

	public bool TrySetException(IEnumerable<Exception> exceptions)
	{
		if (exceptions == null)
		{
			throw new ArgumentNullException("exceptions");
		}
		AggregateException ex = new AggregateException(exceptions);
		if (ex.InnerExceptions.Count == 0)
		{
			throw new ArgumentNullException("exceptions");
		}
		return source.TrySetException(ex);
	}

	public bool TrySetResult(TResult result)
	{
		return source.TrySetResult(result);
	}
}
