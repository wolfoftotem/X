using System.Diagnostics;

namespace System.Threading.Tasks;

[DebuggerDisplay("ShouldExitCurrentIteration = {ShouldExitCurrentIteration}")]
public class ParallelLoopState
{
	internal class ExternalInfos
	{
		public bool IsStopped;

		public AtomicBooleanValue IsBroken = default(AtomicBooleanValue);

		public volatile bool IsExceptional;

		public long? LowestBreakIteration;
	}

	private ExternalInfos extInfos;

	public bool IsStopped => extInfos.IsStopped;

	public bool IsExceptional => extInfos.IsExceptional;

	public long? LowestBreakIteration => extInfos.LowestBreakIteration;

	internal int CurrentIteration { get; set; }

	public bool ShouldExitCurrentIteration
	{
		get
		{
			if (!IsExceptional)
			{
				return IsStopped;
			}
			return true;
		}
	}

	internal ParallelLoopState(ExternalInfos extInfos)
	{
		this.extInfos = extInfos;
	}

	public void Break()
	{
		if (extInfos.IsStopped)
		{
			throw new InvalidOperationException("The Stop method was previously called. Break and Stop may not be used in combination by iterations of the same loop.");
		}
		if (!extInfos.IsBroken.Exchange(newVal: true))
		{
			extInfos.LowestBreakIteration = CurrentIteration;
		}
	}

	public void Stop()
	{
		if (extInfos.IsBroken.Value)
		{
			throw new InvalidOperationException("The Break method was previously called. Break and Stop may not be used in combination by iterations of the same loop.");
		}
		extInfos.IsStopped = true;
	}
}
