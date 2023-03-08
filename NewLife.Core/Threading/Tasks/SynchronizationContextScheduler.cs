using System.Collections.Generic;
using System.Security;

namespace System.Threading.Tasks
{
	internal sealed class SynchronizationContextScheduler : TaskScheduler
	{
		private readonly SynchronizationContext ctx;

		private readonly SendOrPostCallback callback;

		public override int MaximumConcurrencyLevel => base.MaximumConcurrencyLevel;

		public SynchronizationContextScheduler(SynchronizationContext ctx)
		{
			this.ctx = ctx;
			callback = TaskLaunchWrapper;
		}

		[SecurityCritical]
		protected internal override void QueueTask(Task task)
		{
			ctx.Post(callback, task);
		}

		private void TaskLaunchWrapper(object obj)
		{
			TryExecuteTask((Task)obj);
		}

		[SecurityCritical]
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return null;
		}

		protected internal override bool TryDequeue(Task task)
		{
			return false;
		}

		[SecurityCritical]
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			ctx.Send(callback, task);
			return true;
		}
	}
}
