using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks
{
	[DebuggerDisplay("Id = {Id}, Status = {Status}")]
	[DebuggerTypeProxy(typeof(TaskDebuggerView))]
	public class Task : IDisposable, IAsyncResult
	{
		internal const TaskCreationOptions WorkerTaskNotSupportedOptions = TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning;

		private const TaskCreationOptions MaxTaskCreationOptions = TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler;

		[ThreadStatic]
		private static Task current;

		[ThreadStatic]
		private static Action<Task> childWorkAdder;

		internal readonly Task parent;
        private static int id = -1;
        private CountdownEvent childTasks;
        private TaskCreationOptions taskCreationOptions;

		internal TaskScheduler scheduler;

		private TaskExceptionSlot exSlot;

		private TaskStatus status;

		private TaskActionInvoker invoker;
        internal AtomicBooleanValue executing;

		private TaskCompletionQueue<IContinuation> continuations;

		private CancellationToken token;

		private CancellationTokenRegistration? cancellationRegistration;

        public static TaskFactory Factory { get; } = new TaskFactory();

        public static int? CurrentId => current?.Id;

		public AggregateException Exception
		{
			get
			{
				if (exSlot == null)
				{
					return null;
				}
				exSlot.Observed = true;
				return exSlot.Exception;
			}
		}

		public bool IsCanceled => status == TaskStatus.Canceled;

		public bool IsCompleted => status >= TaskStatus.RanToCompletion;

		public bool IsFaulted => status == TaskStatus.Faulted;

		public TaskCreationOptions CreationOptions => taskCreationOptions & (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler);

		public TaskStatus Status
		{
			get
			{
				return status;
			}
			internal set
			{
				status = value;
				Thread.MemoryBarrier();
			}
		}

		private TaskExceptionSlot ExceptionSlot
		{
			get
			{
				if (exSlot != null)
				{
					return exSlot;
				}
				Interlocked.CompareExchange(ref exSlot, new TaskExceptionSlot(this), null);
				return exSlot;
			}
		}

        public object AsyncState { get; private set; }

        bool IAsyncResult.CompletedSynchronously => true;

		WaitHandle IAsyncResult.AsyncWaitHandle => null;

        public int Id { get; }

        private bool IsContinuation => ContinuationAncestor != null;

        internal Task ContinuationAncestor { get; }

        internal string DisplayActionMethod
		{
			get
			{
				Delegate d = invoker.Action;
				if ((object)d != null)
				{
					return d.Method.ToString();
				}
				return "<none>";
			}
		}

		internal Task Parent => parent;

		public Task(Action action)
			: this(action, TaskCreationOptions.None)
		{
		}

		public Task(Action action, TaskCreationOptions creationOptions)
			: this(action, CancellationToken.None, creationOptions)
		{
		}

		public Task(Action action, CancellationToken cancellationToken)
			: this(action, cancellationToken, TaskCreationOptions.None)
		{
		}

		public Task(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
			: this(TaskActionInvoker.Create(action), null, cancellationToken, creationOptions, current)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			if (creationOptions > (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler) || creationOptions < TaskCreationOptions.None)
			{
				throw new ArgumentOutOfRangeException("creationOptions");
			}
		}

		public Task(Action<object> action, object state)
			: this(action, state, TaskCreationOptions.None)
		{
		}

		public Task(Action<object> action, object state, TaskCreationOptions creationOptions)
			: this(action, state, CancellationToken.None, creationOptions)
		{
		}

		public Task(Action<object> action, object state, CancellationToken cancellationToken)
			: this(action, state, cancellationToken, TaskCreationOptions.None)
		{
		}

		public Task(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
			: this(TaskActionInvoker.Create(action), state, cancellationToken, creationOptions, current)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			if (creationOptions > (TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler) || creationOptions < TaskCreationOptions.None)
			{
				throw new ArgumentOutOfRangeException("creationOptions");
			}
		}

		internal Task(TaskActionInvoker invoker, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, Task parent, Task contAncestor = null)
		{
			this.invoker = invoker;
			taskCreationOptions = creationOptions;
			this.AsyncState = state;
			Id = Interlocked.Increment(ref id);
			status = (cancellationToken.IsCancellationRequested ? TaskStatus.Canceled : TaskStatus.Created);
			token = cancellationToken;
			this.parent = parent;
			this.ContinuationAncestor = contAncestor;
			if (CheckTaskOptions(taskCreationOptions, TaskCreationOptions.AttachedToParent))
			{
				parent?.AddChild();
			}
			if (token.CanBeCanceled)
			{
				cancellationRegistration = token.Register(delegate(object l)
				{
					((Task)l).CancelReal();
				}, this);
			}
		}

		private static bool CheckTaskOptions(TaskCreationOptions opt, TaskCreationOptions member)
		{
			return (opt & member) == member;
		}

		public void Start()
		{
			Start(TaskScheduler.Current);
		}

		public void Start(TaskScheduler scheduler)
		{
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			if (status >= TaskStatus.WaitingToRun)
			{
				throw new InvalidOperationException("The Task is not in a valid state to be started.");
			}
			if (IsContinuation)
			{
				throw new InvalidOperationException("Start may not be called on a continuation task");
			}
			SetupScheduler(scheduler);
			Schedule();
		}

		internal void SetupScheduler(TaskScheduler scheduler)
		{
			this.scheduler = scheduler;
			Status = TaskStatus.WaitingForActivation;
		}

		public void RunSynchronously()
		{
			RunSynchronously(TaskScheduler.Current);
		}

		public void RunSynchronously(TaskScheduler scheduler)
		{
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			if (Status > TaskStatus.WaitingForActivation)
			{
				throw new InvalidOperationException("The task is not in a valid state to be started");
			}
			SetupScheduler(scheduler);
			TaskStatus saveStatus = status;
			Status = TaskStatus.WaitingToRun;
			try
			{
				if (scheduler.RunInline(this))
				{
					return;
				}
			}
			catch (Exception inner)
			{
				throw new TaskSchedulerException(inner);
			}
			Status = saveStatus;
			Start(scheduler);
			Wait();
		}

		public Task ContinueWith(Action<Task> continuationAction)
		{
			return ContinueWith(continuationAction, TaskContinuationOptions.None);
		}

		public Task ContinueWith(Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
		{
			return ContinueWith(continuationAction, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken)
		{
			return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task> continuationAction, TaskScheduler scheduler)
		{
			return ContinueWith(continuationAction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			if (continuationAction == null)
			{
				throw new ArgumentNullException("continuationAction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			return ContinueWith(TaskActionInvoker.Create(continuationAction), cancellationToken, continuationOptions, scheduler);
		}

		internal Task ContinueWith(TaskActionInvoker invoker, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			Task continuation = new Task(invoker, null, cancellationToken, GetCreationOptions(continuationOptions), parent, this);
			ContinueWithCore(continuation, continuationOptions, scheduler);
			return continuation;
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction)
		{
			return ContinueWith(continuationFunction, TaskContinuationOptions.None);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
		{
			return ContinueWith(continuationFunction, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
		{
			return ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskScheduler scheduler)
		{
			return ContinueWith(continuationFunction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			if (continuationFunction == null)
			{
				throw new ArgumentNullException("continuationFunction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			return ContinueWith<TResult>(TaskActionInvoker.Create(continuationFunction), cancellationToken, continuationOptions, scheduler);
		}

		internal Task<TResult> ContinueWith<TResult>(TaskActionInvoker invoker, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			Task<TResult> continuation = new Task<TResult>(invoker, null, cancellationToken, GetCreationOptions(continuationOptions), parent, this);
			ContinueWithCore(continuation, continuationOptions, scheduler);
			return continuation;
		}

		internal void ContinueWithCore(Task continuation, TaskContinuationOptions options, TaskScheduler scheduler)
		{
			if ((options & (TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnRanToCompletion)) == (TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnRanToCompletion) || (options & (TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnRanToCompletion)) == (TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnRanToCompletion) || (options & (TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnRanToCompletion)) == (TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnRanToCompletion))
			{
				throw new ArgumentException("continuationOptions", "Some options are mutually exclusive");
			}
			continuation.scheduler = scheduler;
			continuation.Status = TaskStatus.WaitingForActivation;
			ContinueWith(new TaskContinuation(continuation, options));
		}

		internal void ContinueWith(IContinuation continuation)
		{
			if (IsCompleted)
			{
				continuation.Execute();
				return;
			}
			continuations.Add(continuation);
			if (IsCompleted && continuations.Remove(continuation))
			{
				continuation.Execute();
			}
		}

		private void RemoveContinuation(IContinuation continuation)
		{
			continuations.Remove(continuation);
		}

		internal static TaskCreationOptions GetCreationOptions(TaskContinuationOptions kind)
		{
			TaskCreationOptions options = TaskCreationOptions.None;
			if ((kind & TaskContinuationOptions.AttachedToParent) > TaskContinuationOptions.None)
			{
				options |= TaskCreationOptions.AttachedToParent;
			}
			if ((kind & TaskContinuationOptions.PreferFairness) > TaskContinuationOptions.None)
			{
				options |= TaskCreationOptions.PreferFairness;
			}
			if ((kind & TaskContinuationOptions.LongRunning) > TaskContinuationOptions.None)
			{
				options |= TaskCreationOptions.LongRunning;
			}
			return options;
		}

		internal void Schedule()
		{
			Status = TaskStatus.WaitingToRun;
			if (scheduler != TaskScheduler.Current || childWorkAdder == null || CheckTaskOptions(taskCreationOptions, TaskCreationOptions.PreferFairness))
			{
				scheduler.QueueTask(this);
			}
			else
			{
				childWorkAdder(this);
			}
		}

		private void ThreadStart()
		{
			if (!executing.TryRelaxedSet())
			{
				return;
			}
			if (cancellationRegistration.HasValue)
			{
				cancellationRegistration.Value.Dispose();
				cancellationRegistration = null;
			}
			current = this;
			TaskScheduler.Current = scheduler;
			if (!token.IsCancellationRequested)
			{
				status = TaskStatus.Running;
				try
				{
					InnerInvoke();
				}
				catch (OperationCanceledException oce)
				{
					if (token != CancellationToken.None /*&& oce.CancellationToken == token*/)
					{
						CancelReal();
					}
					else
					{
						HandleGenericException(oce);
					}
				}
				catch (Exception e)
				{
					HandleGenericException(e);
				}
			}
			else
			{
				CancelReal();
			}
			Finish();
		}

		internal bool TrySetCanceled()
		{
			if (IsCompleted)
			{
				return false;
			}
			if (!executing.TryRelaxedSet())
			{
				SpinWait sw = default(SpinWait);
				while (!IsCompleted)
				{
					sw.SpinOnce();
				}
				return false;
			}
			CancelReal();
			return true;
		}

		internal bool TrySetException(AggregateException aggregate)
		{
			if (IsCompleted)
			{
				return false;
			}
			if (!executing.TryRelaxedSet())
			{
				SpinWait sw = default(SpinWait);
				while (!IsCompleted)
				{
					sw.SpinOnce();
				}
				return false;
			}
			HandleGenericException(aggregate);
			return true;
		}

		internal void Execute(Action<Task> childAdder)
		{
			childWorkAdder = childAdder;
			Execute();
		}

		internal void Execute()
		{
			ThreadStart();
		}

		internal void AddChild()
		{
			if (childTasks == null)
			{
				Interlocked.CompareExchange(ref childTasks, new CountdownEvent(1), null);
			}
			childTasks.AddCount();
		}

		internal void ChildCompleted(AggregateException childEx)
		{
			if (childEx != null)
			{
				if (ExceptionSlot.ChildExceptions == null)
				{
					Interlocked.CompareExchange(ref ExceptionSlot.ChildExceptions, new ConcurrentQueue<AggregateException>(), null);
				}
				ExceptionSlot.ChildExceptions.Enqueue(childEx);
			}
			if (childTasks.Signal() && status == TaskStatus.WaitingForChildrenToComplete)
			{
				Status = TaskStatus.RanToCompletion;
				ProcessChildExceptions();
				ProcessCompleteDelegates();
				if (CheckTaskOptions(taskCreationOptions, TaskCreationOptions.AttachedToParent) && parent != null)
				{
					parent.ChildCompleted(Exception);
				}
			}
		}

		private void InnerInvoke()
		{
			if (IsContinuation)
			{
				invoker.Invoke(ContinuationAncestor, AsyncState, this);
			}
			else
			{
				invoker.Invoke(this, AsyncState, this);
			}
		}

		internal void Finish()
		{
			if (childTasks != null)
			{
				childTasks.Signal();
			}
			if (status == TaskStatus.Running)
			{
				if (childTasks == null || childTasks.IsSet)
				{
					Status = TaskStatus.RanToCompletion;
				}
				else
				{
					Status = TaskStatus.WaitingForChildrenToComplete;
				}
			}
			if (status == TaskStatus.RanToCompletion)
			{
				ProcessCompleteDelegates();
			}
			current = null;
			TaskScheduler.Current = null;
			if (cancellationRegistration.HasValue)
			{
				cancellationRegistration.Value.Dispose();
			}
			if (CheckTaskOptions(taskCreationOptions, TaskCreationOptions.AttachedToParent) && parent != null && status != TaskStatus.WaitingForChildrenToComplete)
			{
				parent.ChildCompleted(Exception);
			}
		}

		private void ProcessCompleteDelegates()
		{
			if (continuations.HasElements)
			{
				IContinuation continuation;
				while (continuations.TryGetNextCompletion(out continuation))
				{
					continuation.Execute();
				}
			}
		}

		private void ProcessChildExceptions()
		{
			if (exSlot != null && exSlot.ChildExceptions != null)
			{
				if (ExceptionSlot.Exception == null)
				{
					exSlot.Exception = new AggregateException();
				}
				AggregateException childEx;
				while (exSlot.ChildExceptions.TryDequeue(out childEx))
				{
					exSlot.Exception.AddChildException(childEx);
				}
			}
		}

		internal void CancelReal()
		{
			Status = TaskStatus.Canceled;
			ProcessCompleteDelegates();
		}

		private void HandleGenericException(Exception e)
		{
			HandleGenericException(new AggregateException(e));
		}

		private void HandleGenericException(AggregateException e)
		{
			ExceptionSlot.Exception = e;
			Thread.MemoryBarrier();
			Status = TaskStatus.Faulted;
			ProcessCompleteDelegates();
		}

		internal void WaitOnChildren()
		{
			if (Status == TaskStatus.WaitingForChildrenToComplete && childTasks != null)
			{
				childTasks.Wait();
			}
		}

		public void Wait()
		{
			Wait(-1, CancellationToken.None);
		}

		public void Wait(CancellationToken cancellationToken)
		{
			Wait(-1, cancellationToken);
		}

		public bool Wait(TimeSpan timeout)
		{
			return Wait(CheckTimeout(timeout), CancellationToken.None);
		}

		public bool Wait(int millisecondsTimeout)
		{
			return Wait(millisecondsTimeout, CancellationToken.None);
		}

		public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout");
			}
			bool result = true;
			if (!IsCompleted)
			{
				if (Status == TaskStatus.WaitingToRun && millisecondsTimeout == -1 && scheduler != null)
				{
					Execute();
				}
				if (!IsCompleted)
				{
					ManualResetEventSlim evt = new ManualResetEventSlim();
					ManualEventSlot slot = new ManualEventSlot(evt);
					try
					{
						ContinueWith(slot);
						result = evt.Wait(millisecondsTimeout, cancellationToken);
					}
					finally
					{
						if (!result)
						{
							RemoveContinuation(slot);
						}
						evt.Dispose();
					}
				}
			}
			if (IsCanceled)
			{
				throw new AggregateException(new TaskCanceledException(this));
			}
			AggregateException exception = Exception;
			if (exception != null)
			{
				throw exception;
			}
			if (childTasks != null)
			{
				childTasks.Wait();
			}
			return result;
		}

		public static void WaitAll(params Task[] tasks)
		{
			WaitAll(tasks, -1, CancellationToken.None);
		}

		public static void WaitAll(Task[] tasks, CancellationToken cancellationToken)
		{
			WaitAll(tasks, -1, cancellationToken);
		}

		public static bool WaitAll(Task[] tasks, TimeSpan timeout)
		{
			return WaitAll(tasks, CheckTimeout(timeout), CancellationToken.None);
		}

		public static bool WaitAll(Task[] tasks, int millisecondsTimeout)
		{
			return WaitAll(tasks, millisecondsTimeout, CancellationToken.None);
		}

		public static bool WaitAll(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			bool result = true;
			foreach (Task t in tasks)
			{
				if (t == null)
				{
					throw new ArgumentNullException("tasks", "the tasks argument contains a null element");
				}
				result &= t.Status == TaskStatus.RanToCompletion;
			}
			if (!result)
			{
				CountdownEvent evt = new CountdownEvent(tasks.Length);
				CountdownEventSlot slot = new CountdownEventSlot(evt);
				try
				{
					foreach (Task t3 in tasks)
					{
						t3.ContinueWith(slot);
					}
					result = evt.Wait(millisecondsTimeout, cancellationToken);
				}
				finally
				{
					List<Exception> exceptions = null;
					foreach (Task t2 in tasks)
					{
						if (result)
						{
							if (t2.Status != TaskStatus.RanToCompletion)
							{
								if (exceptions == null)
								{
									exceptions = new List<Exception>();
								}
								if (t2.Exception != null)
								{
									exceptions.AddRange(t2.Exception.InnerExceptions);
								}
								else
								{
									exceptions.Add(new TaskCanceledException(t2));
								}
							}
						}
						else
						{
							t2.RemoveContinuation(slot);
						}
					}
					evt.Dispose();
					if (exceptions != null)
					{
						throw new AggregateException(exceptions);
					}
				}
			}
			return result;
		}

		public static int WaitAny(params Task[] tasks)
		{
			return WaitAny(tasks, -1, CancellationToken.None);
		}

		public static int WaitAny(Task[] tasks, TimeSpan timeout)
		{
			return WaitAny(tasks, CheckTimeout(timeout));
		}

		public static int WaitAny(Task[] tasks, int millisecondsTimeout)
		{
			return WaitAny(tasks, millisecondsTimeout, CancellationToken.None);
		}

		public static int WaitAny(Task[] tasks, CancellationToken cancellationToken)
		{
			return WaitAny(tasks, -1, cancellationToken);
		}

		public static int WaitAny(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout");
			}
			CheckForNullTasks(tasks);
			if (tasks.Length > 0)
			{
				ManualResetEventSlim evt = new ManualResetEventSlim();
				ManualEventSlot slot = new ManualEventSlot(evt);
				bool result = false;
				try
				{
					for (int j = 0; j < tasks.Length; j++)
					{
						Task t3 = tasks[j];
						if (t3.IsCompleted)
						{
							return j;
						}
						t3.ContinueWith(slot);
					}
					if (!(result = evt.Wait(millisecondsTimeout, cancellationToken)))
					{
						return -1;
					}
				}
				finally
				{
					if (!result)
					{
						foreach (Task t2 in tasks)
						{
							t2.RemoveContinuation(slot);
						}
					}
					evt.Dispose();
				}
			}
			int firstFinished = -1;
			for (int i = 0; i < tasks.Length; i++)
			{
				Task t = tasks[i];
				if (t.IsCompleted)
				{
					firstFinished = i;
					break;
				}
			}
			return firstFinished;
		}

		private static int CheckTimeout(TimeSpan timeout)
		{
			try
			{
				return checked((int)timeout.TotalMilliseconds);
			}
			catch (OverflowException)
			{
				throw new ArgumentOutOfRangeException("timeout");
			}
		}

		private static void CheckForNullTasks(Task[] tasks)
		{
			foreach (Task t in tasks)
			{
				if (t == null)
				{
					throw new ArgumentNullException("tasks", "the tasks argument contains a null element");
				}
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsCompleted)
			{
				throw new InvalidOperationException("A task may only be disposed if it is in a completion state");
			}
			if (disposing)
			{
				invoker = null;
				AsyncState = null;
				if (cancellationRegistration.HasValue)
				{
					cancellationRegistration.Value.Dispose();
				}
			}
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state)
		{
			return ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state, CancellationToken cancellationToken)
		{
			return ContinueWith(continuationAction, state, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state, TaskContinuationOptions continuationOptions)
		{
			return ContinueWith(continuationAction, state, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state, TaskScheduler scheduler)
		{
			return ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			if (continuationAction == null)
			{
				throw new ArgumentNullException("continuationAction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			Task continuation = new Task(TaskActionInvoker.Create(continuationAction), state, cancellationToken, GetCreationOptions(continuationOptions), parent, this);
			ContinueWithCore(continuation, continuationOptions, scheduler);
			return continuation;
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state)
		{
			return ContinueWith(continuationFunction, state, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, TaskContinuationOptions continuationOptions)
		{
			return ContinueWith(continuationFunction, state, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, CancellationToken cancellationToken)
		{
			return ContinueWith(continuationFunction, state, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, TaskScheduler scheduler)
		{
			return ContinueWith(continuationFunction, state, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			if (continuationFunction == null)
			{
				throw new ArgumentNullException("continuationFunction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			Task<TResult> t = new Task<TResult>(TaskActionInvoker.Create(continuationFunction), state, cancellationToken, GetCreationOptions(continuationOptions), parent, this);
			ContinueWithCore(t, continuationOptions, scheduler);
			return t;
		}

		public static Task<TResult> FromResult<TResult>(TResult result)
		{
			TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
			tcs.SetResult(result);
			return tcs.Task;
		}

		public static Task Run(Action action)
		{
			return Run(action, CancellationToken.None);
		}

		public static Task Run(Action action, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return TaskConstants.Canceled;
			}
			Task t = new Task(action, cancellationToken, TaskCreationOptions.DenyChildAttach);
			t.Start();
			return t;
		}

		public static Task Run(Func<Task> function)
		{
			return Run(function, CancellationToken.None);
		}

		public static Task Run(Func<Task> function, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return TaskConstants.Canceled;
			}
			Task<Task> t = new Task<Task>(function, cancellationToken);
			t.Start();
			return t;
		}

		public static Task<TResult> Run<TResult>(Func<TResult> function)
		{
			return Run(function, CancellationToken.None);
		}

		public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return TaskConstants<TResult>.Canceled;
			}
			Task<TResult> t = new Task<TResult>(function, cancellationToken, TaskCreationOptions.DenyChildAttach);
			t.Start();
			return t;
		}
	}
	[DebuggerTypeProxy(typeof(TaskDebuggerView))]
	[DebuggerDisplay("Id = {Id}, Status = {Status}, Result = {ResultAsString}")]
	public class Task<TResult> : Task
	{
        private TResult value;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TResult Result
		{
			get
			{
				if (!base.IsCompleted)
				{
					Wait();
				}
				if (base.IsCanceled)
				{
					throw new AggregateException(new TaskCanceledException(this));
				}
				if (base.Exception != null)
				{
					throw base.Exception;
				}
				return value;
			}
			internal set
			{
				this.value = value;
			}
		}

		private string ResultAsString
		{
			get
			{
				if ((base.Status & TaskStatus.RanToCompletion) != 0)
				{
					return string.Concat(value);
				}
				return "<value not available>";
			}
		}

        public new static TaskFactory<TResult> Factory { get; } = new TaskFactory<TResult>();

        public Task(Func<TResult> function)
			: this(function, TaskCreationOptions.None)
		{
		}

		public Task(Func<TResult> function, CancellationToken cancellationToken)
			: this(function, cancellationToken, TaskCreationOptions.None)
		{
		}

		public Task(Func<TResult> function, TaskCreationOptions creationOptions)
			: this(function, CancellationToken.None, creationOptions)
		{
		}

		public Task(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
			: base(TaskActionInvoker.Create(function), null, cancellationToken, creationOptions, null)
		{
			if (function == null)
			{
				throw new ArgumentNullException("function");
			}
		}

		public Task(Func<object, TResult> function, object state)
			: this(function, state, TaskCreationOptions.None)
		{
		}

		public Task(Func<object, TResult> function, object state, CancellationToken cancellationToken)
			: this(function, state, cancellationToken, TaskCreationOptions.None)
		{
		}

		public Task(Func<object, TResult> function, object state, TaskCreationOptions creationOptions)
			: this(function, state, CancellationToken.None, creationOptions)
		{
		}

		public Task(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
			: base(TaskActionInvoker.Create(function), state, cancellationToken, creationOptions, null)
		{
			if (function == null)
			{
				throw new ArgumentNullException("function");
			}
		}

		internal Task(TaskActionInvoker invoker, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, Task parent, Task contAncestor = null)
			: base(invoker, state, cancellationToken, creationOptions, parent, contAncestor)
		{
		}

		public Task ContinueWith(Action<Task<TResult>> continuationAction)
		{
			return ContinueWith(continuationAction, TaskContinuationOptions.None);
		}

		public Task ContinueWith(Action<Task<TResult>> continuationAction, TaskContinuationOptions continuationOptions)
		{
			return ContinueWith(continuationAction, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task<TResult>> continuationAction, CancellationToken cancellationToken)
		{
			return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task<TResult>> continuationAction, TaskScheduler scheduler)
		{
			return ContinueWith(continuationAction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task ContinueWith(Action<Task<TResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			if (continuationAction == null)
			{
				throw new ArgumentNullException("continuationAction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			Task t = new Task(TaskActionInvoker.Create(continuationAction), null, cancellationToken, Task.GetCreationOptions(continuationOptions), parent, this);
			ContinueWithCore(t, continuationOptions, scheduler);
			return t;
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction)
		{
			return ContinueWith(continuationFunction, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken)
		{
			return ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskContinuationOptions continuationOptions)
		{
			return ContinueWith(continuationFunction, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskScheduler scheduler)
		{
			return ContinueWith(continuationFunction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			if (continuationFunction == null)
			{
				throw new ArgumentNullException("continuationFunction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			Task<TNewResult> t = new Task<TNewResult>(TaskActionInvoker.Create(continuationFunction), null, cancellationToken, Task.GetCreationOptions(continuationOptions), parent, this);
			ContinueWithCore(t, continuationOptions, scheduler);
			return t;
		}

		internal bool TrySetResult(TResult result)
		{
			if (base.IsCompleted)
			{
				return false;
			}
			if (!executing.TryRelaxedSet())
			{
				SpinWait sw = default(SpinWait);
				while (!base.IsCompleted)
				{
					sw.SpinOnce();
				}
				return false;
			}
			base.Status = TaskStatus.Running;
			value = result;
			Thread.MemoryBarrier();
			Finish();
			return true;
		}

		public Task ContinueWith(Action<Task<TResult>, object> continuationAction, object state)
		{
			return ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task<TResult>, object> continuationAction, object state, CancellationToken cancellationToken)
		{
			return ContinueWith(continuationAction, state, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task<TResult>, object> continuationAction, object state, TaskContinuationOptions continuationOptions)
		{
			return ContinueWith(continuationAction, state, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task<TResult>, object> continuationAction, object state, TaskScheduler scheduler)
		{
			return ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task ContinueWith(Action<Task<TResult>, object> continuationAction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			if (continuationAction == null)
			{
				throw new ArgumentNullException("continuationAction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			Task t = new Task(TaskActionInvoker.Create(continuationAction), state, cancellationToken, Task.GetCreationOptions(continuationOptions), parent, this);
			ContinueWithCore(t, continuationOptions, scheduler);
			return t;
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state)
		{
			return ContinueWith(continuationFunction, state, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state, CancellationToken cancellationToken)
		{
			return ContinueWith(continuationFunction, state, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state, TaskContinuationOptions continuationOptions)
		{
			return ContinueWith(continuationFunction, state, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state, TaskScheduler scheduler)
		{
			return ContinueWith(continuationFunction, state, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object, TNewResult> continuationFunction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			if (continuationFunction == null)
			{
				throw new ArgumentNullException("continuationFunction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			Task<TNewResult> t = new Task<TNewResult>(TaskActionInvoker.Create(continuationFunction), state, cancellationToken, Task.GetCreationOptions(continuationOptions), parent, this);
			ContinueWithCore(t, continuationOptions, scheduler);
			return t;
		}
	}
}
