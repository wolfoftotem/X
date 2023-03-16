using System.Collections.Generic;
using System.Diagnostics;
using System.Security;

namespace System.Threading.Tasks;

[DebuggerDisplay("Id={Id}")]
[DebuggerTypeProxy("System.Threading.Tasks.TaskScheduler+SystemThreadingTasks_TaskSchedulerDebugView")]
public abstract class TaskScheduler
{
	private static TaskScheduler defaultScheduler = new TpScheduler();

	[ThreadStatic]
	private static TaskScheduler currentScheduler;

	private int id;

	private static int lastId = int.MinValue;

	public static TaskScheduler Default => defaultScheduler;

	public static TaskScheduler Current
	{
		get
		{
			if (currentScheduler != null)
			{
				return currentScheduler;
			}
			return defaultScheduler;
		}
		internal set
		{
			currentScheduler = value;
		}
	}

	public int Id => id;

	public virtual int MaximumConcurrencyLevel => Environment.ProcessorCount;

	public static event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

	protected TaskScheduler()
	{
		id = Interlocked.Increment(ref lastId);
	}

	public static TaskScheduler FromCurrentSynchronizationContext()
	{
		SynchronizationContext current = SynchronizationContext.Current;
		return new SynchronizationContextScheduler(current);
	}

	[SecurityCritical]
	protected abstract IEnumerable<Task> GetScheduledTasks();

	[SecurityCritical]
	protected internal abstract void QueueTask(Task task);

	[SecurityCritical]
	protected internal virtual bool TryDequeue(Task task)
	{
		throw new NotSupportedException();
	}

	[SecurityCritical]
	protected internal bool TryExecuteTask(Task task)
	{
		if (task.IsCompleted)
		{
			return false;
		}
		if (task.Status == TaskStatus.WaitingToRun)
		{
			task.Execute();
			task.WaitOnChildren();
			return true;
		}
		return false;
	}

	[SecurityCritical]
	protected abstract bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued);

	[SecurityCritical]
	internal bool RunInline(Task task)
	{
		if (!TryExecuteTaskInline(task, taskWasPreviouslyQueued: false))
		{
			return false;
		}
		if (!task.IsCompleted)
		{
			throw new InvalidOperationException("The TryExecuteTaskInline call to the underlying scheduler succeeded, but the task body was not invoked");
		}
		return true;
	}

	internal static UnobservedTaskExceptionEventArgs FireUnobservedEvent(Task task, AggregateException e)
	{
		UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs = new UnobservedTaskExceptionEventArgs(e);
		EventHandler<UnobservedTaskExceptionEventArgs> unobservedTaskException = TaskScheduler.UnobservedTaskException;
		if (unobservedTaskException == null)
		{
			return unobservedTaskExceptionEventArgs;
		}
		unobservedTaskException(task, unobservedTaskExceptionEventArgs);
		return unobservedTaskExceptionEventArgs;
	}
}
