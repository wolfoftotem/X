namespace System.Threading.Tasks
{
	internal class TaskConstants<T>
	{
		internal static readonly Task<T> Canceled;

		static TaskConstants()
		{
			TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
			tcs.SetCanceled();
			Canceled = tcs.Task;
		}
	}
	internal static class TaskConstants
	{
		public static readonly Task Finished;

		public static readonly Task Canceled;

		static TaskConstants()
		{
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			tcs.SetResult(null);
			Finished = tcs.Task;
			tcs = new TaskCompletionSource<object>();
			tcs.SetCanceled();
			Canceled = tcs.Task;
		}
	}
}
