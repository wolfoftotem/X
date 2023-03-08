namespace System.Threading.Tasks
{
	public static class TaskExtensions
	{
		private const TaskContinuationOptions opt = TaskContinuationOptions.ExecuteSynchronously;

		public static Task<TResult> Unwrap<TResult>(this Task<Task<TResult>> task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}
			TaskCompletionSource<TResult> src = new TaskCompletionSource<TResult>();
			task.ContinueWith(delegate(Task<Task<TResult>> t1)
			{
				CopyCat(t1, src, delegate
				{
					t1.Result.ContinueWith(delegate(Task<TResult> t2)
					{
						CopyCat(t2, src, delegate
						{
							src.SetResult(t2.Result);
						});
					}, TaskContinuationOptions.ExecuteSynchronously);
				});
			}, TaskContinuationOptions.ExecuteSynchronously);
			return src.Task;
		}

		public static Task Unwrap(this Task<Task> task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}
			TaskCompletionSource<object> src = new TaskCompletionSource<object>();
			task.ContinueWith(delegate(Task<Task> t1)
			{
				CopyCat(t1, src, delegate
				{
					t1.Result.ContinueWith(delegate(Task t2)
					{
						CopyCat(t2, src, delegate
						{
							src.SetResult(null);
						});
					}, TaskContinuationOptions.ExecuteSynchronously);
				});
			}, TaskContinuationOptions.ExecuteSynchronously);
			return src.Task;
		}

		private static void CopyCat<TResult>(Task source, TaskCompletionSource<TResult> dest, Action normalAction)
		{
			if (source.IsCanceled)
			{
				dest.SetCanceled();
			}
			else if (source.IsFaulted)
			{
				dest.SetException(source.Exception.InnerExceptions);
			}
			else
			{
				normalAction();
			}
		}
	}
}
