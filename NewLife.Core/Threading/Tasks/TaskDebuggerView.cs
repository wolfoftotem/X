namespace System.Threading.Tasks
{
	internal sealed class TaskDebuggerView
	{
		private readonly Task task;

		public object AsyncState => task.AsyncState;

		public TaskCreationOptions CreationOptions => task.CreationOptions;

		public Exception Exception => task.Exception;

		public int Id => task.Id;

		public string Method => task.DisplayActionMethod;

		public TaskStatus Status => task.Status;

		public TaskDebuggerView(Task task)
		{
			this.task = task;
		}
	}
}
