namespace System.Threading.Tasks
{
	public struct ParallelLoopResult
	{
		public long? LowestBreakIteration { get; private set; }

		public bool IsCompleted { get; private set; }

		internal ParallelLoopResult(long? lowest, bool isCompleted)
		{
			this = default(ParallelLoopResult);
			LowestBreakIteration = lowest;
			IsCompleted = isCompleted;
		}
	}
}
