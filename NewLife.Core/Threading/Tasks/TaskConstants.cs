namespace System.Threading.Tasks;

internal class TaskConstants<T>
{
	internal static readonly Task<T> Canceled;

	static TaskConstants()
	{
		TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
		taskCompletionSource.SetCanceled();
		Canceled = taskCompletionSource.Task;
	}
}
internal static class TaskConstants
{
	public static readonly Task Finished;

	public static readonly Task Canceled;

	static TaskConstants()
	{
		TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
		taskCompletionSource.SetResult(null);
		Finished = taskCompletionSource.Task;
		taskCompletionSource = new TaskCompletionSource<object>();
		taskCompletionSource.SetCanceled();
		Canceled = taskCompletionSource.Task;
	}
}
