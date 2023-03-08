namespace System.Threading
{
	public struct SpinWait
	{
		private const int step = 10;

		private const int maxTime = 200;

		private static readonly bool isSingleCpu = Environment.ProcessorCount == 1;

        public bool NextSpinWillYield
		{
			get
			{
				if (!isSingleCpu)
				{
					return Count % 10 == 0;
				}
				return true;
			}
		}

        public int Count { get; private set; }

        public void SpinOnce()
		{
			Count++;
			if (isSingleCpu)
			{
				Thread.Sleep(0);
			}
			else if (Count % 10 == 0)
			{
				Thread.Sleep(0);
			}
			else
			{
				Thread.SpinWait(Math.Min(Count, 200) << 1);
			}
		}

		public static void SpinUntil(Func<bool> condition)
		{
			SpinWait sw = default(SpinWait);
			while (!condition())
			{
				sw.SpinOnce();
			}
		}

		public static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
		{
			return SpinUntil(condition, (int)timeout.TotalMilliseconds);
		}

		public static bool SpinUntil(Func<bool> condition, int millisecondsTimeout)
		{
			SpinWait sw = default(SpinWait);
			Watch watch = Watch.StartNew();
			while (!condition())
			{
				if (watch.ElapsedMilliseconds > millisecondsTimeout)
				{
					return false;
				}
				sw.SpinOnce();
			}
			return true;
		}

		public void Reset()
		{
			Count = 0;
		}
	}
}
