namespace System.Threading.Tasks;

public class ParallelOptions
{
	internal static readonly ParallelOptions Default = new ParallelOptions();

	public CancellationToken CancellationToken { get; set; }

	public int MaxDegreeOfParallelism { get; set; }

	public TaskScheduler TaskScheduler { get; set; }

	public ParallelOptions()
	{
		MaxDegreeOfParallelism = -1;
		CancellationToken = CancellationToken.None;
		TaskScheduler = TaskScheduler.Current;
	}
}
