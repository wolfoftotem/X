using System.Collections;
using System.Runtime.InteropServices;

namespace System.Threading
{
	[ComVisible(true)]
	public sealed class Timer : MarshalByRefObject, IDisposable
	{
		private sealed class TimerComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				Timer tx = x as Timer;
				if (tx == null)
				{
					return -1;
				}
				Timer ty = y as Timer;
				if (ty == null)
				{
					return 1;
				}
				long result = tx.next_run - ty.next_run;
				if (result == 0)
				{
					if (x != y)
					{
						return -1;
					}
					return 0;
				}
				if (result <= 0)
				{
					return -1;
				}
				return 1;
			}
		}

		private sealed class Scheduler
		{
            private SortedList list;

			private ManualResetEvent changed;

			private static WaitCallback TimerCaller;

            public static Scheduler Instance { get; private set; }

            static Scheduler()
			{
				TimerCaller = TimerCB;
				Instance = new Scheduler();
			}

			private Scheduler()
			{
				changed = new ManualResetEvent(initialState: false);
				list = new SortedList(new TimerComparer(), 1024);
				Thread thread = new Thread(SchedulerThread);
				thread.IsBackground = true;
				thread.Start();
			}

			public void Remove(Timer timer)
			{
				if (timer.next_run != 0 && timer.next_run != long.MaxValue)
				{
					lock (this)
					{
						InternalRemove(timer);
					}
				}
			}

			public void Change(Timer timer, long new_next_run)
			{
				bool wake = false;
				lock (this)
				{
					InternalRemove(timer);
					if (new_next_run == long.MaxValue)
					{
						timer.next_run = new_next_run;
						return;
					}
					if (!timer.disposed)
					{
						timer.next_run = new_next_run;
						Add(timer);
						wake = list.GetByIndex(0) == timer;
					}
				}
				if (wake)
				{
					changed.Set();
				}
			}

			private int FindByDueTime(long nr)
			{
				int min = 0;
				int max = list.Count - 1;
				if (max < 0)
				{
					return -1;
				}
				if (max < 20)
				{
					for (; min <= max; min++)
					{
						Timer t2 = (Timer)list.GetByIndex(min);
						if (t2.next_run == nr)
						{
							return min;
						}
						if (t2.next_run > nr)
						{
							return -1;
						}
					}
					return -1;
				}
				while (min <= max)
				{
					int half = min + (max - min >> 1);
					Timer t = (Timer)list.GetByIndex(half);
					if (nr == t.next_run)
					{
						return half;
					}
					if (nr > t.next_run)
					{
						min = half + 1;
					}
					else
					{
						max = half - 1;
					}
				}
				return -1;
			}

			private void Add(Timer timer)
			{
				int idx = FindByDueTime(timer.next_run);
				if (idx != -1)
				{
					bool up = ((long.MaxValue - timer.next_run > 20000) ? true : false);
					Timer t2;
					do
					{
						idx++;
						if (up)
						{
							timer.next_run++;
						}
						else
						{
							timer.next_run--;
						}
						if (idx >= list.Count)
						{
							break;
						}
						t2 = (Timer)list.GetByIndex(idx);
					}
					while (t2.next_run == timer.next_run);
				}
				list.Add(timer, timer);
			}

			private int InternalRemove(Timer timer)
			{
				int idx = list.IndexOfKey(timer);
				if (idx >= 0)
				{
					list.RemoveAt(idx);
				}
				return idx;
			}

			private static void TimerCB(object o)
			{
				Timer timer = (Timer)o;
				try
				{
					timer.callback(timer.state);
				}
				catch
				{
				}
			}

			private void SchedulerThread()
			{
				Thread.CurrentThread.Name = "Timer-Scheduler";
				ArrayList new_time = new ArrayList(512);
				while (true)
				{
					int ms_wait = -1;
					long ticks = DateTime.Now.Ticks;
					lock (this)
					{
						changed.Reset();
						int count = list.Count;
						int i;
						for (i = 0; i < count; i++)
						{
							Timer timer2 = (Timer)list.GetByIndex(i);
							if (timer2.next_run > ticks)
							{
								break;
							}
							list.RemoveAt(i);
							count--;
							i--;
							ThreadPool.QueueUserWorkItem(TimerCaller, timer2);
							long period = timer2.period_ms;
							long due_time = timer2.due_time_ms;
							if (period == -1 || ((period == 0 || period == -1) && due_time != -1))
							{
								timer2.next_run = long.MaxValue;
							}
							else
							{
								timer2.next_run = DateTime.Now.Ticks + 10000 * timer2.period_ms;
								new_time.Add(timer2);
							}
						}
						count = new_time.Count;
						for (i = 0; i < count; i++)
						{
							Timer timer = (Timer)new_time[i];
							Add(timer);
						}
						new_time.Clear();
						ShrinkIfNeeded(new_time, 512);
						int capacity = list.Capacity;
						count = list.Count;
						if (capacity > 1024 && count > 0 && capacity / count > 3)
						{
							list.Capacity = count * 2;
						}
						long min_next_run = long.MaxValue;
						if (list.Count > 0)
						{
							min_next_run = ((Timer)list.GetByIndex(0)).next_run;
						}
						ms_wait = -1;
						if (min_next_run != long.MaxValue)
						{
							long diff = min_next_run - DateTime.Now.Ticks;
							ms_wait = (int)(diff / 10000);
							if (ms_wait < 0)
							{
								ms_wait = 0;
							}
						}
					}
					changed.WaitOne(ms_wait);
				}
			}

			private void ShrinkIfNeeded(ArrayList list, int initial)
			{
				int capacity = list.Capacity;
				int count = list.Count;
				if (capacity > initial && count > 0 && capacity / count > 3)
				{
					list.Capacity = count * 2;
				}
			}
		}

		private const long MaxValue = 4294967294L;

		private static Scheduler scheduler = Scheduler.Instance;

		private TimerCallback callback;

		private object state;

		private long due_time_ms;

		private long period_ms;

		private long next_run;

		private bool disposed;

		public Timer(TimerCallback callback, object state, int dueTime, int period)
		{
			Init(callback, state, dueTime, period);
		}

		public Timer(TimerCallback callback, object state, long dueTime, long period)
		{
			Init(callback, state, dueTime, period);
		}

		public Timer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
		{
			Init(callback, state, (long)dueTime.TotalMilliseconds, (long)period.TotalMilliseconds);
		}

		[CLSCompliant(false)]
		public Timer(TimerCallback callback, object state, uint dueTime, uint period)
		{
			long d = ((dueTime == uint.MaxValue) ? (-1L) : ((long)dueTime));
			long p = ((period == uint.MaxValue) ? (-1L) : ((long)period));
			Init(callback, state, d, p);
		}

		public Timer(TimerCallback callback)
		{
			Init(callback, this, -1L, -1L);
		}

		private void Init(TimerCallback callback, object state, long dueTime, long period)
		{
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}
			this.callback = callback;
			this.state = state;
			Change(dueTime, period, first: true);
		}

		public bool Change(int dueTime, int period)
		{
			return Change(dueTime, period, first: false);
		}

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			return Change((long)dueTime.TotalMilliseconds, (long)period.TotalMilliseconds, first: false);
		}

		[CLSCompliant(false)]
		public bool Change(uint dueTime, uint period)
		{
			long d = ((dueTime == uint.MaxValue) ? (-1L) : ((long)dueTime));
			long p = ((period == uint.MaxValue) ? (-1L) : ((long)period));
			return Change(d, p, first: false);
		}

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				scheduler.Remove(this);
			}
		}

		public bool Change(long dueTime, long period)
		{
			return Change(dueTime, period, first: false);
		}

		private bool Change(long dueTime, long period, bool first)
		{
			if (dueTime > 4294967294u)
			{
				throw new ArgumentOutOfRangeException("dueTime", "Due time too large");
			}
			if (period > 4294967294u)
			{
				throw new ArgumentOutOfRangeException("period", "Period too large");
			}
			if (dueTime < -1)
			{
				throw new ArgumentOutOfRangeException("dueTime");
			}
			if (period < -1)
			{
				throw new ArgumentOutOfRangeException("period");
			}
			if (disposed)
			{
				return false;
			}
			due_time_ms = dueTime;
			period_ms = period;
			long nr;
			if (dueTime == 0)
			{
				nr = 0L;
			}
			else if (dueTime < 0)
			{
				nr = long.MaxValue;
				if (first)
				{
					next_run = nr;
					return true;
				}
			}
			else
			{
				nr = dueTime * 10000 + DateTime.Now.Ticks;
			}
			scheduler.Change(this, nr);
			return true;
		}

		public bool Dispose(WaitHandle notifyObject)
		{
			if (notifyObject == null)
			{
				throw new ArgumentNullException("notifyObject");
			}
			Dispose();
			return true;
		}
	}
}
