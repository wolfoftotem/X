using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Threading.Tasks;

public class ThreadWorker : IDisposable
{
	private const int maxRetry = 3;

	private const int sleepThreshold = 100;

	private Thread workerThread;

	[ThreadStatic]
	private static ThreadWorker autoReference;

	private readonly IConcurrentDeque<Task> dDeque;

	private readonly ThreadWorker[] others;

	private readonly ManualResetEvent waitHandle;

	private readonly IProducerConsumerCollection<Task> sharedWorkQueue;

	private readonly ThreadPriority threadPriority;

	private int started;

	private readonly int workerLength;

	private readonly int workerPosition;

	private int deepSleepTime = 8;

	private readonly Action<Task> adder;

	private Task currentTask;

	public bool Finished => started == 0;

	public int Id => workerThread.ManagedThreadId;

	public ThreadWorker(ThreadWorker[] others, int workerPosition, IProducerConsumerCollection<Task> sharedWorkQueue, IConcurrentDeque<Task> dDeque, ThreadPriority priority, ManualResetEvent handle)
	{
		this.others = others;
		this.dDeque = dDeque;
		this.sharedWorkQueue = sharedWorkQueue;
		workerLength = others.Length;
		this.workerPosition = workerPosition;
		waitHandle = handle;
		threadPriority = priority;
		adder = ChildWorkAdder;
		InitializeUnderlyingThread();
	}

	private void InitializeUnderlyingThread()
	{
		workerThread = new Thread(WorkerMethodWrapper);
		workerThread.IsBackground = true;
		workerThread.Name = "ParallelFxThreadWorker";
	}

	public void Dispose()
	{
		Stop();
		if (workerThread.ThreadState != ThreadState.Stopped)
		{
			workerThread.Abort();
		}
	}

	public void Pulse()
	{
		if (started != 1 && Interlocked.Exchange(ref started, 1) == 0)
		{
			if (workerThread.ThreadState != ThreadState.Unstarted)
			{
				InitializeUnderlyingThread();
			}
			workerThread.Start();
		}
	}

	public void Stop()
	{
		started = 0;
	}

	private void WorkerMethodWrapper()
	{
		int num = 0;
		autoReference = this;
		bool flag = false;
		while (started == 1)
		{
			bool flag2 = false;
			flag2 = WorkerMethod();
			if (!flag2 && flag)
			{
				waitHandle.Reset();
			}
			flag = false;
			Thread.Sleep(0);
			if (flag2)
			{
				deepSleepTime = 8;
				num = 0;
			}
			else if (++num > 100 && sharedWorkQueue.Count == 0)
			{
				flag = waitHandle.WaitOne(deepSleepTime = ((deepSleepTime >= 16384) ? 16384 : (deepSleepTime << 1)));
			}
		}
		started = 0;
	}

	private bool WorkerMethod()
	{
		bool result = false;
		bool flag;
		do
		{
			flag = false;
			Task obj;
			while (sharedWorkQueue.Count > 0)
			{
				waitHandle.Set();
				while (sharedWorkQueue.TryTake(out obj))
				{
					dDeque.PushBottom(obj);
				}
				while (dDeque.PopBottom(out obj) == PopResult.Succeed)
				{
					waitHandle.Set();
					ExecuteTask(obj, ref result);
				}
			}
			for (int i = 0; i < 3; i++)
			{
				int num = workerLength + workerPosition;
				for (int j = workerPosition + 1; j < num; j++)
				{
					int num2 = j % workerLength;
					ThreadWorker threadWorker;
					if ((threadWorker = others[num2]) == null || threadWorker == this)
					{
						continue;
					}
					while (threadWorker.dDeque.PopTop(out obj) == PopResult.Succeed)
					{
						if (!flag)
						{
							waitHandle.Set();
						}
						flag = true;
						ExecuteTask(obj, ref result);
					}
				}
			}
		}
		while (sharedWorkQueue.Count > 0 || flag);
		return result;
	}

	private void ExecuteTask(Task value, ref bool result)
	{
		if (value != null)
		{
			Task task = currentTask;
			currentTask = value;
			value.Execute(adder);
			result = true;
			currentTask = task;
		}
	}

	public static void ParticipativeWorkerMethod(Task self, ManualResetEventSlim predicateEvt, int millisecondsTimeout, IProducerConsumerCollection<Task> sharedWorkQueue, ThreadWorker[] others, ManualResetEvent evt)
	{
		int num = 50;
		WaitHandle[] waitHandles = null;
		Watch watch = Watch.StartNew();
		if (millisecondsTimeout == -1)
		{
			millisecondsTimeout = int.MaxValue;
		}
		bool flag = false;
		bool flag2 = autoReference != null;
		Action<Task> action = null;
		while (!predicateEvt.IsSet && watch.ElapsedMilliseconds < millisecondsTimeout && !self.IsCompleted)
		{
			if (self.Status == TaskStatus.WaitingToRun)
			{
				self.Execute(flag2 ? autoReference.adder : null);
				if (predicateEvt.IsSet || watch.ElapsedMilliseconds > millisecondsTimeout)
				{
					break;
				}
			}
			if (flag2)
			{
				IEnumerable<Task> enumerable = autoReference.dDeque.GetEnumerable();
				if (action == null)
				{
					action = (flag2 ? autoReference.adder : null);
				}
				if (enumerable != null)
				{
					foreach (Task item2 in enumerable)
					{
						if (item2 != null)
						{
							if (CheckTaskFitness(self, item2))
							{
								item2.Execute(action);
							}
							if (predicateEvt.IsSet || watch.ElapsedMilliseconds > millisecondsTimeout)
							{
								return;
							}
						}
					}
				}
			}
			int num2 = sharedWorkQueue.Count;
			Task item;
			while (--num2 >= 0 && sharedWorkQueue.TryTake(out item) && item != null)
			{
				evt.Set();
				if (CheckTaskFitness(self, item) || flag)
				{
					item.Execute(null);
				}
				else
				{
					if (autoReference == null)
					{
						sharedWorkQueue.TryAdd(item);
					}
					else
					{
						autoReference.dDeque.PushBottom(item);
					}
					evt.Set();
				}
				if (predicateEvt.IsSet || watch.ElapsedMilliseconds > millisecondsTimeout)
				{
					return;
				}
			}
			if (predicateEvt.IsSet || watch.ElapsedMilliseconds > millisecondsTimeout)
			{
				break;
			}
			for (int i = 0; i < others.Length; i++)
			{
				ThreadWorker threadWorker;
				if ((threadWorker = others[i]) == autoReference || threadWorker == null)
				{
					continue;
				}
				if (threadWorker.dDeque.PopTop(out item) == PopResult.Succeed && item != null)
				{
					evt.Set();
					if (CheckTaskFitness(self, item) || flag)
					{
						item.Execute(null);
					}
					else
					{
						if (autoReference == null)
						{
							sharedWorkQueue.TryAdd(item);
						}
						else
						{
							autoReference.dDeque.PushBottom(item);
						}
						evt.Set();
					}
				}
				if (predicateEvt.IsSet || watch.ElapsedMilliseconds > millisecondsTimeout)
				{
					return;
				}
			}
			if (--num > 5)
			{
				Thread.Sleep(0);
				continue;
			}
			if (num >= 0)
			{
				predicateEvt.Wait(ComputeTimeout(5, millisecondsTimeout, watch));
				continue;
			}
			if (num == -1)
			{
				waitHandles = new WaitHandle[2] { predicateEvt.WaitHandle, evt };
			}
			WaitHandle.WaitAny(waitHandles, ComputeTimeout(1000, millisecondsTimeout, watch));
			if (num == -10)
			{
				flag = true;
			}
		}
	}

	private static bool CheckTaskFitness(Task self, Task t)
	{
		if (((t.CreationOptions & TaskCreationOptions.LongRunning) != 0 || t.Id >= self.Id) && t.Parent != self && t != self)
		{
			if (autoReference != null && autoReference.currentTask != null)
			{
				return autoReference.currentTask == t.Parent;
			}
			return false;
		}
		return true;
	}

	internal void ChildWorkAdder(Task t)
	{
		dDeque.PushBottom(t);
		waitHandle.Set();
	}

	private static int ComputeTimeout(int proposed, int timeout, Watch watch)
	{
		if (timeout != int.MaxValue)
		{
			return Math.Min(proposed, Math.Max(0, (int)(timeout - watch.ElapsedMilliseconds)));
		}
		return proposed;
	}

	public virtual bool Equals(ThreadWorker other)
	{
		if (other != null)
		{
			return object.ReferenceEquals(dDeque, other.dDeque);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ThreadWorker other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return workerThread.ManagedThreadId.GetHashCode();
	}
}
