namespace System.Threading.Tasks
{
	internal abstract class TaskActionInvoker
	{
		private sealed class EmptyTaskActionInvoker : TaskActionInvoker
		{
			public override Delegate Action => null;

			public override void Invoke(Task owner, object state, Task context)
			{
			}
		}

		private sealed class ActionInvoke : TaskActionInvoker
		{
			private readonly Action action;

			public override Delegate Action => action;

			public ActionInvoke(Action action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				action();
			}
		}

		private sealed class ActionObjectInvoke : TaskActionInvoker
		{
			private readonly Action<object> action;

			public override Delegate Action => action;

			public ActionObjectInvoke(Action<object> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				action(state);
			}
		}

		private sealed class ActionTaskInvoke : TaskActionInvoker
		{
			private readonly Action<Task> action;

			public override Delegate Action => action;

			public ActionTaskInvoke(Action<Task> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				action(owner);
			}
		}

		private sealed class ActionTasksInvoke : TaskActionInvoker
		{
			private readonly Action<Task[]> action;

			private readonly Task[] tasks;

			public override Delegate Action => action;

			public ActionTasksInvoke(Action<Task[]> action, Task[] tasks)
			{
				this.action = action;
				this.tasks = tasks;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				action(tasks);
			}
		}

		private sealed class ActionTaskObjectInvoke : TaskActionInvoker
		{
			private readonly Action<Task, object> action;

			public override Delegate Action => action;

			public ActionTaskObjectInvoke(Action<Task, object> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				action(owner, state);
			}
		}

		private sealed class ActionTaskObjectInvoke<TResult> : TaskActionInvoker
		{
			private readonly Action<Task<TResult>, object> action;

			public override Delegate Action => action;

			public ActionTaskObjectInvoke(Action<Task<TResult>, object> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				action((Task<TResult>)owner, state);
			}
		}

		private sealed class ActionTaskInvoke<TResult> : TaskActionInvoker
		{
			private readonly Action<Task<TResult>> action;

			public override Delegate Action => action;

			public ActionTaskInvoke(Action<Task<TResult>> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				action((Task<TResult>)owner);
			}
		}

		private sealed class ActionTaskSelected : TaskActionInvoker
		{
			private readonly Action<Task> action;

			private readonly Task[] tasks;

			public override Delegate Action => action;

			public ActionTaskSelected(Action<Task> action, Task[] tasks)
			{
				this.action = action;
				this.tasks = tasks;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				int result = ((Task<int>)owner).Result;
				action(tasks[result]);
			}
		}

		private sealed class FuncInvoke<TResult> : TaskActionInvoker
		{
			private readonly Func<TResult> action;

			public override Delegate Action => action;

			public FuncInvoke(Func<TResult> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				((Task<TResult>)context).Result = action();
			}
		}

		private sealed class FuncTaskInvoke<TResult> : TaskActionInvoker
		{
			private readonly Func<Task, TResult> action;

			public override Delegate Action => action;

			public FuncTaskInvoke(Func<Task, TResult> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				((Task<TResult>)context).Result = action(owner);
			}
		}

		private sealed class FuncTasksInvoke<TResult> : TaskActionInvoker
		{
			private readonly Func<Task[], TResult> action;

			private readonly Task[] tasks;

			public override Delegate Action => action;

			public FuncTasksInvoke(Func<Task[], TResult> action, Task[] tasks)
			{
				this.action = action;
				this.tasks = tasks;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				((Task<TResult>)context).Result = action(tasks);
			}
		}

		private sealed class FuncTaskSelected<TResult> : TaskActionInvoker
		{
			private readonly Func<Task, TResult> action;

			private readonly Task[] tasks;

			public override Delegate Action => action;

			public FuncTaskSelected(Func<Task, TResult> action, Task[] tasks)
			{
				this.action = action;
				this.tasks = tasks;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				int result = ((Task<int>)owner).Result;
				((Task<TResult>)context).Result = action(tasks[result]);
			}
		}

		private sealed class FuncTaskInvoke<TResult, TNewResult> : TaskActionInvoker
		{
			private readonly Func<Task<TResult>, TNewResult> action;

			public override Delegate Action => action;

			public FuncTaskInvoke(Func<Task<TResult>, TNewResult> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				((Task<TNewResult>)context).Result = action((Task<TResult>)owner);
			}
		}

		private sealed class FuncObjectInvoke<TResult> : TaskActionInvoker
		{
			private readonly Func<object, TResult> action;

			public override Delegate Action => action;

			public FuncObjectInvoke(Func<object, TResult> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				((Task<TResult>)context).Result = action(state);
			}
		}

		private sealed class FuncTaskObjectInvoke<TResult> : TaskActionInvoker
		{
			private readonly Func<Task, object, TResult> action;

			public override Delegate Action => action;

			public FuncTaskObjectInvoke(Func<Task, object, TResult> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				((Task<TResult>)context).Result = action(owner, state);
			}
		}

		private sealed class FuncTaskObjectInvoke<TResult, TNewResult> : TaskActionInvoker
		{
			private readonly Func<Task<TResult>, object, TNewResult> action;

			public override Delegate Action => action;

			public FuncTaskObjectInvoke(Func<Task<TResult>, object, TNewResult> action)
			{
				this.action = action;
			}

			public override void Invoke(Task owner, object state, Task context)
			{
				((Task<TNewResult>)context).Result = action((Task<TResult>)owner, state);
			}
		}

		public static readonly TaskActionInvoker Empty = new EmptyTaskActionInvoker();

		public abstract Delegate Action { get; }

		public static TaskActionInvoker Create(Action action)
		{
			return new ActionInvoke(action);
		}

		public static TaskActionInvoker Create(Action<object> action)
		{
			return new ActionObjectInvoke(action);
		}

		public static TaskActionInvoker Create(Action<Task> action)
		{
			return new ActionTaskInvoke(action);
		}

		public static TaskActionInvoker Create(Action<Task, object> action)
		{
			return new ActionTaskObjectInvoke(action);
		}

		public static TaskActionInvoker Create<TResult>(Action<Task<TResult>> action)
		{
			return new ActionTaskInvoke<TResult>(action);
		}

		public static TaskActionInvoker Create<TResult>(Action<Task<TResult>, object> action)
		{
			return new ActionTaskObjectInvoke<TResult>(action);
		}

		public static TaskActionInvoker Create<TResult>(Func<TResult> action)
		{
			return new FuncInvoke<TResult>(action);
		}

		public static TaskActionInvoker Create<TResult>(Func<object, TResult> action)
		{
			return new FuncObjectInvoke<TResult>(action);
		}

		public static TaskActionInvoker Create<TResult>(Func<Task, TResult> action)
		{
			return new FuncTaskInvoke<TResult>(action);
		}

		public static TaskActionInvoker Create<TResult>(Func<Task, object, TResult> action)
		{
			return new FuncTaskObjectInvoke<TResult>(action);
		}

		public static TaskActionInvoker Create<TResult, TNewResult>(Func<Task<TResult>, TNewResult> action)
		{
			return new FuncTaskInvoke<TResult, TNewResult>(action);
		}

		public static TaskActionInvoker Create<TResult, TNewResult>(Func<Task<TResult>, object, TNewResult> action)
		{
			return new FuncTaskObjectInvoke<TResult, TNewResult>(action);
		}

		public static TaskActionInvoker Create(Action<Task[]> action, Task[] tasks)
		{
			return new ActionTasksInvoke(action, tasks);
		}

		public static TaskActionInvoker Create<TResult>(Func<Task[], TResult> action, Task[] tasks)
		{
			return new FuncTasksInvoke<TResult>(action, tasks);
		}

		public static TaskActionInvoker Create(Action<Task> action, Task[] tasks)
		{
			return new ActionTaskSelected(action, tasks);
		}

		public static TaskActionInvoker Create<TResult>(Func<Task, TResult> action, Task[] tasks)
		{
			return new FuncTaskSelected<TResult>(action, tasks);
		}

		public abstract void Invoke(Task owner, object state, Task context);
	}
}
