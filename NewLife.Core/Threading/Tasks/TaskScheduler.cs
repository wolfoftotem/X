using System.Collections.Generic;
using System.Diagnostics;
using System.Security;

namespace System.Threading.Tasks
{
	[DebuggerDisplay("Id={Id}")]
	[DebuggerTypeProxy("System.Threading.Tasks.TaskScheduler+SystemThreadingTasks_TaskSchedulerDebugView")]
	public abstract class TaskScheduler
	{
        [ThreadStatic]
		private static TaskScheduler currentScheduler;
        private static int lastId = int.MinValue;

        public static TaskScheduler Default { get; } = new TpScheduler();

        public static TaskScheduler Current
		{
			get
			{
				if (currentScheduler != null)
				{
					return currentScheduler;
				}
				return Default;
			}
			internal set
			{
				currentScheduler = value;
			}
		}

        public int Id { get; }

        public virtual int MaximumConcurrencyLevel => Environment.ProcessorCount;

		public static event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

		protected TaskScheduler()
		{
			Id = Interlocked.Increment(ref lastId);
		}

		public static TaskScheduler FromCurrentSynchronizationContext()
		{
			SynchronizationContext syncCtx = SynchronizationContext.Current;
			return new SynchronizationContextScheduler(syncCtx);
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
			UnobservedTaskExceptionEventArgs args = new UnobservedTaskExceptionEventArgs(e);
			EventHandler<UnobservedTaskExceptionEventArgs> temp = TaskScheduler.UnobservedTaskException;
			if (temp == null)
			{
				return args;
			}
			temp(task, args);
			return args;
		}
	}
}
