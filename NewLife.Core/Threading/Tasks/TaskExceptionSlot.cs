using System.Collections.Concurrent;

namespace System.Threading.Tasks
{
	internal class TaskExceptionSlot
	{
		public volatile AggregateException Exception;

		public volatile bool Observed;

		public ConcurrentQueue<AggregateException> ChildExceptions;

		private Task parent;

		public TaskExceptionSlot(Task parent)
		{
			this.parent = parent;
		}

		~TaskExceptionSlot()
		{
			if (Exception != null && (!Observed && !TaskScheduler.FireUnobservedEvent(parent, Exception).Observed))
			{
				parent = null;
				throw Exception;
			}
		}
	}
}
