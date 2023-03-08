namespace System.Threading
{
	public struct SpinWait
	{
		private const int step = 10;

		private const int maxTime = 200;

		private static readonly bool isSingleCpu = Environment.ProcessorCount == 1;

		private int ntime;

		public bool NextSpinWillYield
		{
			get
			{
				if (!isSingleCpu)
				{
					return ntime % 10 == 0;
				}
				return true;
			}
		}

		public int Count => ntime;

		public void SpinOnce()
		{
			ntime++;
			if (isSingleCpu)
			{
				Thread.Sleep(0);
			}
			else if (ntime % 10 == 0)
			{
				Thread.Sleep(0);
			}
			else
			{
				Thread.SpinWait(Math.Min(ntime, 200) << 1);
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
			ntime = 0;
		}
	}
}
