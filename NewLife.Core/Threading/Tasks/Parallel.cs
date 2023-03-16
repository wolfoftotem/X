using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks;

public static class Parallel
{
	[StructLayout(LayoutKind.Explicit)]
	private struct StealValue64
	{
		[FieldOffset(0)]
		public long Value;

		[FieldOffset(0)]
		public int Actual;

		[FieldOffset(4)]
		public int Stolen;
	}

	private class StealRange
	{
		public StealValue64 V64 = default(StealValue64);

		public StealRange(int fromInclusive, int i, int step)
		{
			V64.Actual = fromInclusive + i * step;
		}
	}

	private static readonly bool sixtyfour = IntPtr.Size == 8;

	internal static int GetBestWorkerNumber()
	{
		return GetBestWorkerNumber(TaskScheduler.Current);
	}

	internal static int GetBestWorkerNumber(TaskScheduler scheduler)
	{
		return scheduler.MaximumConcurrencyLevel;
	}

	private static int GetBestWorkerNumber(int from, int to, ParallelOptions options, out int step)
	{
		int num = GetBestWorkerNumber(options.TaskScheduler);
		if (options != null && options.MaxDegreeOfParallelism != -1)
		{
			num = options.MaxDegreeOfParallelism;
		}
		if ((step = (to - from) / num) < 5)
		{
			step = 5;
			num = (to - from) / 5;
			if (num < 1)
			{
				num = 1;
			}
		}
		return num;
	}

	private static void HandleExceptions(IEnumerable<Task> tasks)
	{
		HandleExceptions(tasks, null);
	}

	private static void HandleExceptions(IEnumerable<Task> tasks, ParallelLoopState.ExternalInfos infos)
	{
		List<Exception> list = new List<Exception>();
		foreach (Task task in tasks)
		{
			if (task.Exception != null)
			{
				list.Add(task.Exception);
			}
		}
		if (list.Count > 0)
		{
			if (infos != null)
			{
				infos.IsExceptional = true;
			}
			throw new AggregateException(list).Flatten();
		}
	}

	private static void InitTasks(Task[] tasks, int count, Action action, ParallelOptions options)
	{
		TaskCreationOptions creationOptions = TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent;
		for (int i = 0; i < count; i++)
		{
			if (options == null)
			{
				tasks[i] = Task.Factory.StartNew(action, creationOptions);
			}
			else
			{
				tasks[i] = Task.Factory.StartNew(action, options.CancellationToken, creationOptions, options.TaskScheduler);
			}
		}
	}

	public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body)
	{
		return For(fromInclusive, toExclusive, ParallelOptions.Default, body);
	}

	public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int, ParallelLoopState> body)
	{
		return For(fromInclusive, toExclusive, ParallelOptions.Default, body);
	}

	public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int> body)
	{
		return For(fromInclusive, toExclusive, parallelOptions, delegate(int index, ParallelLoopState state)
		{
			body(index);
		});
	}

	public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int, ParallelLoopState> body)
	{
		return For(fromInclusive, toExclusive, parallelOptions, () => null, delegate(int i, ParallelLoopState s, object l)
		{
			body(i, s);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, Func<TLocal> localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		return For(fromInclusive, toExclusive, ParallelOptions.Default, localInit, body, localFinally);
	}

	public static ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		if (localInit == null)
		{
			throw new ArgumentNullException("localInit");
		}
		if (localFinally == null)
		{
			throw new ArgumentNullException("localFinally");
		}
		if (parallelOptions == null)
		{
			throw new ArgumentNullException("options");
		}
		if (fromInclusive >= toExclusive)
		{
			return new ParallelLoopResult(null, isCompleted: true);
		}
		int step;
		int num = GetBestWorkerNumber(fromInclusive, toExclusive, parallelOptions, out step);
		Task[] tasks = new Task[num];
		StealRange[] ranges = new StealRange[num];
		for (int i = 0; i < num; i++)
		{
			ranges[i] = new StealRange(fromInclusive, i, step);
		}
		ParallelLoopState.ExternalInfos infos = new ParallelLoopState.ExternalInfos();
		int currentIndex = -1;
		Action action = delegate
		{
			int num2 = Interlocked.Increment(ref currentIndex);
			StealRange stealRange = ranges[num2];
			int actual = stealRange.V64.Actual;
			int num3 = ((num2 + 1 == num) ? toExclusive : Math.Min(toExclusive, actual + step));
			TLocal val = localInit();
			ParallelLoopState parallelLoopState = new ParallelLoopState(infos);
			CancellationToken cancellationToken = parallelOptions.CancellationToken;
			try
			{
				for (int num4 = actual; num4 < num3; num4 = (stealRange.V64.Actual = num4 + 1))
				{
					if (infos.IsStopped)
					{
						return;
					}
					cancellationToken.ThrowIfCancellationRequested();
					if (num4 >= num3 - stealRange.V64.Stolen)
					{
						break;
					}
					if (infos.LowestBreakIteration.HasValue && infos.LowestBreakIteration > num4)
					{
						return;
					}
					parallelLoopState.CurrentIteration = num4;
					val = body(num4, parallelLoopState, val);
					if (num4 + 1 >= num3 - stealRange.V64.Stolen)
					{
						break;
					}
				}
				int num5 = num + num2;
				for (int j = num2 + 1; j < num5; j++)
				{
					int num6 = j % num;
					stealRange = ranges[num6];
					num3 = ((num6 + 1 == num) ? toExclusive : Math.Min(toExclusive, fromInclusive + (num6 + 1) * step));
					int num7 = -1;
					while (true)
					{
						StealValue64 stealValue = default(StealValue64);
						long num8 = (stealValue.Value = (sixtyfour ? stealRange.V64.Value : Interlocked.CompareExchange(ref stealRange.V64.Value, 0L, 0L)));
						if (stealValue.Actual >= num3 - stealValue.Stolen - 2)
						{
							break;
						}
						num7 = ++stealValue.Stolen;
						if (Interlocked.CompareExchange(ref stealRange.V64.Value, stealValue.Value, num8) == num8)
						{
							num7 = num3 - num7;
							if (num7 <= stealRange.V64.Actual)
							{
								break;
							}
							val = body(num7, parallelLoopState, val);
						}
					}
				}
			}
			finally
			{
				localFinally(val);
			}
		};
		InitTasks(tasks, num, action, parallelOptions);
		try
		{
			Task.WaitAll(tasks);
		}
		catch
		{
			HandleExceptions(tasks, infos);
		}
		return new ParallelLoopResult(infos.LowestBreakIteration, !infos.IsStopped && !infos.IsExceptional);
	}

	public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long> body)
	{
		return For(fromInclusive, toExclusive, ParallelOptions.Default, body);
	}

	public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long, ParallelLoopState> body)
	{
		return For(fromInclusive, toExclusive, ParallelOptions.Default, body);
	}

	public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long> body)
	{
		return For(fromInclusive, toExclusive, parallelOptions, delegate(long index, ParallelLoopState state)
		{
			body(index);
		});
	}

	public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long, ParallelLoopState> body)
	{
		return For(fromInclusive, toExclusive, parallelOptions, () => null, delegate(long i, ParallelLoopState s, object l)
		{
			body(i, s);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, Func<TLocal> localInit, Func<long, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		return For(fromInclusive, toExclusive, ParallelOptions.Default, localInit, body, localFinally);
	}

	public static ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<long, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		if (localInit == null)
		{
			throw new ArgumentNullException("localInit");
		}
		if (localFinally == null)
		{
			throw new ArgumentNullException("localFinally");
		}
		if (parallelOptions == null)
		{
			throw new ArgumentNullException("options");
		}
		if (fromInclusive >= toExclusive)
		{
			return new ParallelLoopResult(null, isCompleted: true);
		}
		throw new NotImplementedException();
	}

	private static ParallelLoopResult ForEach<TSource, TLocal>(Func<int, IList<IEnumerator<TSource>>> enumerable, ParallelOptions options, Func<TLocal> init, Func<TSource, ParallelLoopState, TLocal, TLocal> action, Action<TLocal> destruct)
	{
		if (enumerable == null)
		{
			throw new ArgumentNullException("source");
		}
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		if (init == null)
		{
			throw new ArgumentNullException("init");
		}
		if (destruct == null)
		{
			throw new ArgumentNullException("destruct");
		}
		int num = Math.Min(GetBestWorkerNumber(), (options != null && options.MaxDegreeOfParallelism != -1) ? options.MaxDegreeOfParallelism : int.MaxValue);
		Task[] tasks = new Task[num];
		ParallelLoopState.ExternalInfos infos = new ParallelLoopState.ExternalInfos();
		SimpleConcurrentBag<TSource> bag = new SimpleConcurrentBag<TSource>(num);
		IList<IEnumerator<TSource>> slices = enumerable(num);
		int sliceIndex = -1;
		Action action2 = delegate
		{
			IEnumerator<TSource> enumerator = slices[Interlocked.Increment(ref sliceIndex)];
			TLocal val = init();
			ParallelLoopState arg = new ParallelLoopState(infos);
			int nextIndex = bag.GetNextIndex();
			CancellationToken cancellationToken = options.CancellationToken;
			try
			{
				bool flag = true;
				TSource value;
				while (flag)
				{
					if (infos.IsStopped || infos.IsBroken.Value)
					{
						return;
					}
					cancellationToken.ThrowIfCancellationRequested();
					for (int i = 0; i < 5; i++)
					{
						if (!(flag = enumerator.MoveNext()))
						{
							break;
						}
						bag.Add(nextIndex, enumerator.Current);
					}
					for (int j = 0; j < 5; j++)
					{
						if (!bag.TryTake(nextIndex, out value))
						{
							break;
						}
						if (infos.IsStopped)
						{
							return;
						}
						cancellationToken.ThrowIfCancellationRequested();
						val = action(value, arg, val);
					}
				}
				while (bag.TrySteal(nextIndex, out value))
				{
					cancellationToken.ThrowIfCancellationRequested();
					val = action(value, arg, val);
					if (infos.IsStopped || infos.IsBroken.Value)
					{
						break;
					}
				}
			}
			finally
			{
				destruct(val);
			}
		};
		InitTasks(tasks, num, action2, options);
		try
		{
			Task.WaitAll(tasks);
		}
		catch
		{
			HandleExceptions(tasks, infos);
		}
		return new ParallelLoopResult(infos.LowestBreakIteration, !infos.IsStopped && !infos.IsExceptional);
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(Partitioner.Create(source), ParallelOptions.Default, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState> body)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(Partitioner.Create(source), ParallelOptions.Default, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e, s);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState, long> body)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(Partitioner.Create(source), ParallelOptions.Default, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e, s, -1L);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource, ParallelLoopState> body)
	{
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(source, ParallelOptions.Default, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e, s);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, Action<TSource, ParallelLoopState, long> body)
	{
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(source, ParallelOptions.Default, () => null, delegate(TSource e, ParallelLoopState s, long i, object l)
		{
			body(e, s, i);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource> body)
	{
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(source, ParallelOptions.Default, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(Partitioner.Create(source), parallelOptions, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState> body)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(Partitioner.Create(source), parallelOptions, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e, s);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState, long> body)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(Partitioner.Create(source), parallelOptions, () => null, delegate(TSource e, ParallelLoopState s, long i, object l)
		{
			body(e, s, i);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState, long> body)
	{
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(source, parallelOptions, () => null, delegate(TSource e, ParallelLoopState s, long i, object l)
		{
			body(e, s, i);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
	{
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(source, parallelOptions, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState> body)
	{
		return ForEach(source, parallelOptions, () => null, delegate(TSource e, ParallelLoopState s, object l)
		{
			body(e, s);
			return null;
		}, delegate
		{
		});
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return ForEach(Partitioner.Create(source), ParallelOptions.Default, localInit, body, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		return ForEach(Partitioner.Create(source), ParallelOptions.Default, localInit, body, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		return ForEach(source, ParallelOptions.Default, localInit, body, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		return ForEach(source, ParallelOptions.Default, localInit, body, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return ForEach(Partitioner.Create(source), parallelOptions, localInit, body, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return ForEach(Partitioner.Create(source), parallelOptions, localInit, body, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(source.GetPartitions, parallelOptions, localInit, body, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (body == null)
		{
			throw new ArgumentNullException("body");
		}
		return ForEach(source.GetOrderablePartitions, parallelOptions, localInit, (KeyValuePair<long, TSource> e, ParallelLoopState s, TLocal l) => body(e.Value, s, e.Key, l), localFinally);
	}

	public static void Invoke(params Action[] actions)
	{
		if (actions == null)
		{
			throw new ArgumentNullException("actions");
		}
		Invoke(ParallelOptions.Default, actions);
	}

	public static void Invoke(ParallelOptions parallelOptions, params Action[] actions)
	{
		if (parallelOptions == null)
		{
			throw new ArgumentNullException("parallelOptions");
		}
		if (actions == null)
		{
			throw new ArgumentNullException("actions");
		}
		if (actions.Length == 0)
		{
			throw new ArgumentException("actions is empty");
		}
		foreach (Action action in actions)
		{
			if (action == null)
			{
				throw new ArgumentException("One action in actions is null", "actions");
			}
		}
		if (actions.Length == 1)
		{
			actions[0]();
			return;
		}
		Task[] array = new Task[actions.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = Task.Factory.StartNew(actions[j], parallelOptions.CancellationToken, TaskCreationOptions.None, parallelOptions.TaskScheduler);
		}
		try
		{
			Task.WaitAll(array, parallelOptions.CancellationToken);
		}
		catch
		{
			HandleExceptions(array);
		}
	}

	internal static Task[] SpawnBestNumber(Action action, Action callback)
	{
		return SpawnBestNumber(action, -1, callback);
	}

	internal static Task[] SpawnBestNumber(Action action, int dop, Action callback)
	{
		return SpawnBestNumber(action, dop, wait: false, callback);
	}

	internal static Task[] SpawnBestNumber(Action action, int dop, bool wait, Action callback)
	{
		int num = ((dop != -1) ? dop : (wait ? (GetBestWorkerNumber() + 1) : GetBestWorkerNumber()));
		CountdownEvent evt = new CountdownEvent(num);
		Task[] array = new Task[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = Task.Factory.StartNew(delegate
			{
				action();
				evt.Signal();
				if (callback != null && evt.IsSet)
				{
					callback();
				}
			});
		}
		if (wait)
		{
			Task.WaitAll(array);
		}
		return array;
	}
}
