namespace System.Threading.Tasks
{
	[Serializable]
	public class TaskCanceledException : OperationCanceledException
	{
		private Task task;

		public Task Task => task;

		public TaskCanceledException()
		{
		}

		public TaskCanceledException(string message)
			: base(message)
		{
		}

		public TaskCanceledException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public TaskCanceledException(Task task)
			: base("The Task was canceled")
		{
			this.task = task;
		}
	}
}
