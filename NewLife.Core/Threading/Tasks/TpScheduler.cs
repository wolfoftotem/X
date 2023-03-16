using System.Collections.Generic;
using System.Security;

namespace System.Threading.Tasks;

internal class TpScheduler : TaskScheduler
{
	private static readonly WaitCallback callback = TaskExecuterCallback;

	public override int MaximumConcurrencyLevel => base.MaximumConcurrencyLevel;

	[SecurityCritical]
	protected internal override void QueueTask(Task task)
	{
		ThreadPool.UnsafeQueueUserWorkItem(callback, task);
	}

	private static void TaskExecuterCallback(object obj)
	{
		Task task = (Task)obj;
		task.Execute();
	}

	protected override IEnumerable<Task> GetScheduledTasks()
	{
		throw new NotImplementedException();
	}

	[SecurityCritical]
	protected internal override bool TryDequeue(Task task)
	{
		throw new NotImplementedException();
	}

	[SecurityCritical]
	protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
	{
		return TryExecuteTask(task);
	}
}
