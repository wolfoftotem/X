namespace System.Threading;

internal struct Watch
{
	private long startTicks;

	public long ElapsedMilliseconds => (TicksNow() - startTicks) / 10000;

	public static Watch StartNew()
	{
		Watch result = default(Watch);
		result.Start();
		return result;
	}

	public void Start()
	{
		startTicks = TicksNow();
	}

	public void Stop()
	{
	}

	private static long TicksNow()
	{
		return DateTime.Now.Ticks;
	}
}
