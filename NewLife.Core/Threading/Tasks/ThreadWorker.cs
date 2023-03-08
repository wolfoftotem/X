using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Threading.Tasks
{
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
			int sleepTime = 0;
			autoReference = this;
			bool wasWokenUp = false;
			while (started == 1)
			{
				bool result = false;
				result = WorkerMethod();
				if (!result && wasWokenUp)
				{
					waitHandle.Reset();
				}
				wasWokenUp = false;
				Thread.Sleep(0);
				if (result)
				{
					deepSleepTime = 8;
					sleepTime = 0;
				}
				else if (++sleepTime > 100 && sharedWorkQueue.Count == 0)
				{
					wasWokenUp = waitHandle.WaitOne(deepSleepTime = ((deepSleepTime >= 16384) ? 16384 : (deepSleepTime << 1)));
				}
			}
			started = 0;
		}

		private bool WorkerMethod()
		{
			bool result = false;
			bool hasStolenFromOther;
			do
			{
				hasStolenFromOther = false;
				Task value;
				while (sharedWorkQueue.Count > 0)
				{
					waitHandle.Set();
					while (sharedWorkQueue.TryTake(out value))
					{
						dDeque.PushBottom(value);
					}
					while (dDeque.PopBottom(out value) == PopResult.Succeed)
					{
						waitHandle.Set();
						ExecuteTask(value, ref result);
					}
				}
				for (int j = 0; j < 3; j++)
				{
					int len = workerLength + workerPosition;
					for (int it = workerPosition + 1; it < len; it++)
					{
						int i = it % workerLength;
						ThreadWorker other;
						if ((other = others[i]) == null || other == this)
						{
							continue;
						}
						while (other.dDeque.PopTop(out value) == PopResult.Succeed)
						{
							if (!hasStolenFromOther)
							{
								waitHandle.Set();
							}
							hasStolenFromOther = true;
							ExecuteTask(value, ref result);
						}
					}
				}
			}
			while (sharedWorkQueue.Count > 0 || hasStolenFromOther);
			return result;
		}

		private void ExecuteTask(Task value, ref bool result)
		{
			if (value != null)
			{
				Task saveCurrent = currentTask;
				currentTask = value;
				value.Execute(adder);
				result = true;
				currentTask = saveCurrent;
			}
		}

		public static void ParticipativeWorkerMethod(Task self, ManualResetEventSlim predicateEvt, int millisecondsTimeout, IProducerConsumerCollection<Task> sharedWorkQueue, ThreadWorker[] others, ManualResetEvent evt)
		{
			int tries = 50;
			WaitHandle[] handles = null;
			Watch watch = Watch.StartNew();
			if (millisecondsTimeout == -1)
			{
				millisecondsTimeout = int.MaxValue;
			}
			bool aggressive = false;
			bool hasAutoReference = autoReference != null;
			Action<Task> adder = null;
			while (!predicateEvt.IsSet && watch.ElapsedMilliseconds < millisecondsTimeout && !self.IsCompleted)
			{
				if (self.Status == TaskStatus.WaitingToRun)
				{
					self.Execute(hasAutoReference ? autoReference.adder : null);
					if (predicateEvt.IsSet || watch.ElapsedMilliseconds > millisecondsTimeout)
					{
						break;
					}
				}
				if (hasAutoReference)
				{
					IEnumerable<Task> enumerable = autoReference.dDeque.GetEnumerable();
					if (adder == null)
					{
						adder = (hasAutoReference ? autoReference.adder : null);
					}
					if (enumerable != null)
					{
						foreach (Task t in enumerable)
						{
							if (t != null)
							{
								if (CheckTaskFitness(self, t))
								{
									t.Execute(adder);
								}
								if (predicateEvt.IsSet || watch.ElapsedMilliseconds > millisecondsTimeout)
								{
									return;
								}
							}
						}
					}
				}
				int count = sharedWorkQueue.Count;
				Task value;
				while (--count >= 0 && sharedWorkQueue.TryTake(out value) && value != null)
				{
					evt.Set();
					if (CheckTaskFitness(self, value) || aggressive)
					{
						value.Execute(null);
					}
					else
					{
						if (autoReference == null)
						{
							sharedWorkQueue.TryAdd(value);
						}
						else
						{
							autoReference.dDeque.PushBottom(value);
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
					ThreadWorker other;
					if ((other = others[i]) == autoReference || other == null)
					{
						continue;
					}
					if (other.dDeque.PopTop(out value) == PopResult.Succeed && value != null)
					{
						evt.Set();
						if (CheckTaskFitness(self, value) || aggressive)
						{
							value.Execute(null);
						}
						else
						{
							if (autoReference == null)
							{
								sharedWorkQueue.TryAdd(value);
							}
							else
							{
								autoReference.dDeque.PushBottom(value);
							}
							evt.Set();
						}
					}
					if (predicateEvt.IsSet || watch.ElapsedMilliseconds > millisecondsTimeout)
					{
						return;
					}
				}
				if (--tries > 5)
				{
					Thread.Sleep(0);
					continue;
				}
				if (tries >= 0)
				{
					predicateEvt.Wait(ComputeTimeout(5, millisecondsTimeout, watch));
					continue;
				}
				if (tries == -1)
				{
					handles = new WaitHandle[2] { predicateEvt.WaitHandle, evt };
				}
				WaitHandle.WaitAny(handles, ComputeTimeout(1000, millisecondsTimeout, watch));
				if (tries == -10)
				{
					aggressive = true;
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
			ThreadWorker temp = obj as ThreadWorker;
			if (temp != null)
			{
				return Equals(temp);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return workerThread.ManagedThreadId.GetHashCode();
		}
	}
}
