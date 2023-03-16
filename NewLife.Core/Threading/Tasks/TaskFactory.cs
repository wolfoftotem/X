namespace System.Threading.Tasks;

public class TaskFactory
{
	private readonly TaskScheduler scheduler;

	private TaskCreationOptions creationOptions;

	private TaskContinuationOptions continuationOptions;

	private CancellationToken cancellationToken;

	public TaskScheduler Scheduler => scheduler;

	public TaskContinuationOptions ContinuationOptions => continuationOptions;

	public TaskCreationOptions CreationOptions => creationOptions;

	public CancellationToken CancellationToken => cancellationToken;

	public TaskFactory()
		: this(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, null)
	{
	}

	public TaskFactory(CancellationToken cancellationToken)
		: this(cancellationToken, TaskCreationOptions.None, TaskContinuationOptions.None, null)
	{
	}

	public TaskFactory(TaskScheduler scheduler)
		: this(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, scheduler)
	{
	}

	public TaskFactory(TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions)
		: this(CancellationToken.None, creationOptions, continuationOptions, null)
	{
	}

	public TaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		this.cancellationToken = cancellationToken;
		this.scheduler = scheduler;
		this.creationOptions = creationOptions;
		this.continuationOptions = continuationOptions;
		CheckContinuationOptions(continuationOptions);
	}

	internal static void CheckContinuationOptions(TaskContinuationOptions continuationOptions)
	{
		if ((continuationOptions & (TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnRanToCompletion)) != 0)
		{
			throw new ArgumentOutOfRangeException("continuationOptions");
		}
		if ((continuationOptions & (TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously)) == (TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously))
		{
			throw new ArgumentOutOfRangeException("continuationOptions", "Synchronous continuations cannot be long running");
		}
	}

	public Task StartNew(Action action)
	{
		return StartNew(action, cancellationToken, creationOptions, GetScheduler());
	}

	public Task StartNew(Action action, CancellationToken cancellationToken)
	{
		return StartNew(action, cancellationToken, creationOptions, GetScheduler());
	}

	public Task StartNew(Action action, TaskCreationOptions creationOptions)
	{
		return StartNew(action, cancellationToken, creationOptions, GetScheduler());
	}

	public Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		Task task = new Task(action, cancellationToken, creationOptions);
		if (!task.IsCompleted)
		{
			task.Start(scheduler);
		}
		return task;
	}

	public Task StartNew(Action<object> action, object state)
	{
		return StartNew(action, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken)
	{
		return StartNew(action, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task StartNew(Action<object> action, object state, TaskCreationOptions creationOptions)
	{
		return StartNew(action, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		Task task = new Task(action, state, cancellationToken, creationOptions);
		if (!task.IsCompleted)
		{
			task.Start(scheduler);
		}
		return task;
	}

	public Task<TResult> StartNew<TResult>(Func<TResult> function)
	{
		return StartNew(function, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions)
	{
		return StartNew(function, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken)
	{
		return StartNew(function, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		Task<TResult> task = new Task<TResult>(function, cancellationToken, creationOptions);
		if (!task.IsCompleted)
		{
			task.Start(scheduler);
		}
		return task;
	}

	public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state)
	{
		return StartNew(function, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken)
	{
		return StartNew(function, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions)
	{
		return StartNew(function, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		Task<TResult> task = new Task<TResult>(function, state, cancellationToken, creationOptions);
		task.Start(scheduler);
		return task;
	}

	public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction)
	{
		return ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken)
	{
		return ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		if (tasks == null)
		{
			throw new ArgumentNullException("tasks");
		}
		if (tasks.Length == 0)
		{
			throw new ArgumentException("The tasks argument contains no tasks", "tasks");
		}
		foreach (Task task in tasks)
		{
			if (task == null)
			{
				throw new ArgumentException("The tasks argument constains a null value", "tasks");
			}
		}
		if (continuationAction == null)
		{
			throw new ArgumentNullException("continuationAction");
		}
		Task<int> task2 = new Task<int>(delegate(object l)
		{
			Tuple<Task[], CancellationToken> tuple = (Tuple<Task[], CancellationToken>)l;
			return Task.WaitAny(tuple.Item1, tuple.Item2);
		}, Tuple.Create(tasks, cancellationToken));
		Task result = task2.ContinueWith(TaskActionInvoker.Create(continuationAction, tasks), cancellationToken, continuationOptions, scheduler);
		task2.Start(scheduler);
		return result;
	}

	public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction)
	{
		return ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken)
	{
		return ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		return ContinueWhenAny(tasks, delegate(Task o)
		{
			continuationAction((Task<TAntecedentResult>)o);
		}, cancellationToken, continuationOptions, scheduler);
	}

	public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		Task<int> task = new Task<int>(delegate(object l)
		{
			Tuple<Task[], CancellationToken> tuple = (Tuple<Task[], CancellationToken>)l;
			return Task.WaitAny(tuple.Item1, tuple.Item2);
		}, Tuple.Create(tasks, cancellationToken));
		Task<TResult> result = task.ContinueWith<TResult>(TaskActionInvoker.Create(continuationFunction, tasks), cancellationToken, continuationOptions, scheduler);
		task.Start(scheduler);
		return result;
	}

	public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		return ContinueWhenAny(tasks, (Task t) => continuationFunction((Task<TAntecedentResult>)t), cancellationToken, continuationOptions, scheduler);
	}

	public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction)
	{
		return ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken)
	{
		return ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		Task task = new Task(delegate(object l)
		{
			Tuple<Task[], CancellationToken> tuple = (Tuple<Task[], CancellationToken>)l;
			Task.WaitAll(tuple.Item1, tuple.Item2);
		}, Tuple.Create(tasks, cancellationToken));
		Task result = task.ContinueWith(TaskActionInvoker.Create(continuationAction, tasks), cancellationToken, continuationOptions, scheduler);
		task.Start(scheduler);
		return result;
	}

	public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction)
	{
		return ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken)
	{
		return ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		return ContinueWhenAll((Task[])tasks, (Action<Task[]>)delegate
		{
			continuationAction(tasks);
		}, cancellationToken, continuationOptions, scheduler);
	}

	public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		Task task = new Task(delegate(object l)
		{
			Tuple<Task[], CancellationToken> tuple = (Tuple<Task[], CancellationToken>)l;
			Task.WaitAll(tuple.Item1, tuple.Item2);
		}, Tuple.Create(tasks, cancellationToken));
		Task<TResult> result = task.ContinueWith<TResult>(TaskActionInvoker.Create(continuationFunction, tasks), cancellationToken, continuationOptions, scheduler);
		task.Start(scheduler);
		return result;
	}

	public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		return ContinueWhenAll(tasks, (Task[] o) => continuationFunction(tasks), cancellationToken, continuationOptions, scheduler);
	}

	public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod)
	{
		return FromAsync(asyncResult, endMethod, creationOptions);
	}

	public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions)
	{
		return FromAsync(asyncResult, endMethod, creationOptions, GetScheduler());
	}

	public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		return TaskFactory<object>.FromIAsyncResult(asyncResult, delegate
		{
			endMethod(asyncResult);
			return null;
		}, creationOptions, scheduler);
	}

	public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod)
	{
		return FromAsync(asyncResult, endMethod, creationOptions);
	}

	public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions)
	{
		return FromAsync(asyncResult, endMethod, creationOptions, GetScheduler());
	}

	public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		return TaskFactory<TResult>.FromIAsyncResult(asyncResult, endMethod, creationOptions, scheduler);
	}

	public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state)
	{
		return FromAsync(beginMethod, endMethod, state, creationOptions);
	}

	public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions)
	{
		return TaskFactory<object>.FromAsyncBeginEnd(beginMethod, delegate(IAsyncResult l)
		{
			endMethod(l);
			return null;
		}, state, creationOptions);
	}

	public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, state, creationOptions);
	}

	public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions)
	{
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		return TaskFactory<object>.FromAsyncBeginEnd(beginMethod, delegate(IAsyncResult l)
		{
			endMethod(l);
			return null;
		}, arg1, state, creationOptions);
	}

	public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);
	}

	public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
	{
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		return TaskFactory<object>.FromAsyncBeginEnd(beginMethod, delegate(IAsyncResult l)
		{
			endMethod(l);
			return null;
		}, arg1, arg2, state, creationOptions);
	}

	public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
	}

	public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
	{
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		return TaskFactory<object>.FromAsyncBeginEnd(beginMethod, delegate(IAsyncResult l)
		{
			endMethod(l);
			return null;
		}, arg1, arg2, arg3, state, creationOptions);
	}

	public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state)
	{
		return FromAsync(beginMethod, endMethod, state, creationOptions);
	}

	public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions)
	{
		return TaskFactory<TResult>.FromAsyncBeginEnd(beginMethod, endMethod, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions)
	{
		return TaskFactory<TResult>.FromAsyncBeginEnd(beginMethod, endMethod, arg1, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
	{
		return TaskFactory<TResult>.FromAsyncBeginEnd(beginMethod, endMethod, arg1, arg2, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
	{
		return TaskFactory<TResult>.FromAsyncBeginEnd(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
	}

	private TaskScheduler GetScheduler()
	{
		return scheduler ?? TaskScheduler.Current;
	}
}
public class TaskFactory<TResult>
{
	private readonly TaskScheduler scheduler;

	private TaskCreationOptions creationOptions;

	private TaskContinuationOptions continuationOptions;

	private CancellationToken cancellationToken;

	private TaskFactory parent;

	public TaskScheduler Scheduler => scheduler;

	public TaskContinuationOptions ContinuationOptions => continuationOptions;

	public TaskCreationOptions CreationOptions => creationOptions;

	public CancellationToken CancellationToken => cancellationToken;

	public TaskFactory()
		: this(CancellationToken.None)
	{
	}

	public TaskFactory(TaskScheduler scheduler)
		: this(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, scheduler)
	{
	}

	public TaskFactory(CancellationToken cancellationToken)
		: this(cancellationToken, TaskCreationOptions.None, TaskContinuationOptions.None, (TaskScheduler)null)
	{
	}

	public TaskFactory(TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions)
		: this(CancellationToken.None, creationOptions, continuationOptions, (TaskScheduler)null)
	{
	}

	public TaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		this.cancellationToken = cancellationToken;
		this.scheduler = scheduler;
		this.creationOptions = creationOptions;
		this.continuationOptions = continuationOptions;
		TaskFactory.CheckContinuationOptions(continuationOptions);
		parent = new TaskFactory(cancellationToken, creationOptions, continuationOptions, scheduler);
	}

	public Task<TResult> StartNew(Func<TResult> function)
	{
		return StartNew(function, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions)
	{
		return StartNew(function, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken)
	{
		return StartNew(function, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		return StartNew((object o) => function(), null, cancellationToken, creationOptions, scheduler);
	}

	public Task<TResult> StartNew(Func<object, TResult> function, object state)
	{
		return StartNew(function, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew(Func<object, TResult> function, object state, TaskCreationOptions creationOptions)
	{
		return StartNew(function, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken)
	{
		return StartNew(function, state, cancellationToken, creationOptions, GetScheduler());
	}

	public Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		return parent.StartNew(function, state, cancellationToken, creationOptions, scheduler);
	}

	public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		return parent.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
	}

	public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		return parent.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
	}

	public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		return parent.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
	}

	public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken)
	{
		return ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, GetScheduler());
	}

	public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
	{
		return parent.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
	}

	public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod)
	{
		return FromAsync(asyncResult, endMethod, creationOptions);
	}

	public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions)
	{
		return FromAsync(asyncResult, endMethod, creationOptions, GetScheduler());
	}

	public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		return FromIAsyncResult(asyncResult, endMethod, creationOptions, scheduler);
	}

	internal static Task<TResult> FromIAsyncResult(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler)
	{
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if ((creationOptions & (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning)) != 0)
		{
			throw new ArgumentOutOfRangeException("creationOptions");
		}
		CancellationTokenSource source = new CancellationTokenSource();
		Task<TResult> task = new Task<TResult>(delegate
		{
			try
			{
				return endMethod(asyncResult);
			}
			catch (OperationCanceledException)
			{
				source.Cancel();
				source.Token.ThrowIfCancellationRequested();
			}
			return default(TResult);
		}, null, source.Token, creationOptions);
		if (asyncResult.IsCompleted)
		{
			task.RunSynchronously(scheduler);
		}
		else
		{
			ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, delegate
			{
				task.RunSynchronously(scheduler);
			}, null, -1, executeOnlyOnce: true);
		}
		return task;
	}

	public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state)
	{
		return FromAsync(beginMethod, endMethod, state, creationOptions);
	}

	public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions)
	{
		return FromAsyncBeginEnd(beginMethod, endMethod, state, creationOptions);
	}

	internal static Task<TResult> FromAsyncBeginEnd(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions)
	{
		if (beginMethod == null)
		{
			throw new ArgumentNullException("beginMethod");
		}
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		if ((creationOptions & (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning)) != 0)
		{
			throw new ArgumentOutOfRangeException("creationOptions");
		}
		TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state, creationOptions);
		beginMethod(delegate(IAsyncResult l)
		{
			InnerInvoke(tcs, endMethod, l);
		}, state);
		return tcs.Task;
	}

	public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions)
	{
		return FromAsyncBeginEnd(beginMethod, endMethod, arg1, state, creationOptions);
	}

	internal static Task<TResult> FromAsyncBeginEnd<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions)
	{
		if (beginMethod == null)
		{
			throw new ArgumentNullException("beginMethod");
		}
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		if ((creationOptions & (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning)) != 0)
		{
			throw new ArgumentOutOfRangeException("creationOptions");
		}
		TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state, creationOptions);
		beginMethod(arg1, delegate(IAsyncResult l)
		{
			InnerInvoke(tcs, endMethod, l);
		}, state);
		return tcs.Task;
	}

	public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
	{
		return FromAsyncBeginEnd(beginMethod, endMethod, arg1, arg2, state, creationOptions);
	}

	internal static Task<TResult> FromAsyncBeginEnd<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
	{
		if (beginMethod == null)
		{
			throw new ArgumentNullException("beginMethod");
		}
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		if ((creationOptions & (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning)) != 0)
		{
			throw new ArgumentOutOfRangeException("creationOptions");
		}
		TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state, creationOptions);
		beginMethod(arg1, arg2, delegate(IAsyncResult l)
		{
			InnerInvoke(tcs, endMethod, l);
		}, state);
		return tcs.Task;
	}

	public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state)
	{
		return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
	}

	public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
	{
		return FromAsyncBeginEnd(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
	}

	internal static Task<TResult> FromAsyncBeginEnd<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
	{
		if (beginMethod == null)
		{
			throw new ArgumentNullException("beginMethod");
		}
		if (endMethod == null)
		{
			throw new ArgumentNullException("endMethod");
		}
		if ((creationOptions & (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning)) != 0)
		{
			throw new ArgumentOutOfRangeException("creationOptions");
		}
		TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state, creationOptions);
		beginMethod(arg1, arg2, arg3, delegate(IAsyncResult l)
		{
			InnerInvoke(tcs, endMethod, l);
		}, state);
		return tcs.Task;
	}

	private TaskScheduler GetScheduler()
	{
		return scheduler ?? TaskScheduler.Current;
	}

	private static void InnerInvoke(TaskCompletionSource<TResult> tcs, Func<IAsyncResult, TResult> endMethod, IAsyncResult l)
	{
		try
		{
			tcs.SetResult(endMethod(l));
		}
		catch (OperationCanceledException)
		{
			tcs.SetCanceled();
		}
		catch (Exception exception)
		{
			tcs.SetException(exception);
		}
	}
}
