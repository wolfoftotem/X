namespace System.Threading.Tasks;

internal class TaskContinuation : IContinuation
{
	private readonly Task task;

	private readonly TaskContinuationOptions continuationOptions;

	public TaskContinuation(Task task, TaskContinuationOptions continuationOptions)
	{
		this.task = task;
		this.continuationOptions = continuationOptions;
	}

	private bool ContinuationStatusCheck(TaskContinuationOptions kind)
	{
		if (kind == TaskContinuationOptions.None)
		{
			return true;
		}
		int num = (int)kind;
		TaskStatus status = task.ContinuationAncestor.Status;
		if (num >= 65536)
		{
			kind &= ~(TaskContinuationOptions.PreferFairness | TaskContinuationOptions.LongRunning | TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
			switch (status)
			{
			case TaskStatus.Canceled:
				switch (kind)
				{
				case TaskContinuationOptions.NotOnCanceled:
					return false;
				case TaskContinuationOptions.OnlyOnFaulted:
					return false;
				case TaskContinuationOptions.OnlyOnRanToCompletion:
					return false;
				}
				break;
			case TaskStatus.Faulted:
				switch (kind)
				{
				case TaskContinuationOptions.NotOnFaulted:
					return false;
				case TaskContinuationOptions.OnlyOnCanceled:
					return false;
				case TaskContinuationOptions.OnlyOnRanToCompletion:
					return false;
				}
				break;
			case TaskStatus.RanToCompletion:
				switch (kind)
				{
				case TaskContinuationOptions.NotOnRanToCompletion:
					return false;
				case TaskContinuationOptions.OnlyOnFaulted:
					return false;
				case TaskContinuationOptions.OnlyOnCanceled:
					return false;
				}
				break;
			}
		}
		return true;
	}

	public void Execute()
	{
		if (!ContinuationStatusCheck(continuationOptions))
		{
			task.CancelReal();
			task.Dispose();
		}
		else if ((continuationOptions & TaskContinuationOptions.ExecuteSynchronously) != 0)
		{
			task.RunSynchronously(task.scheduler);
		}
		else
		{
			task.Schedule();
		}
	}
}
