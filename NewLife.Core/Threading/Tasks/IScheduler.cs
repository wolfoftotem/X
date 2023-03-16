namespace System.Threading.Tasks;

internal interface IScheduler : IDisposable
{
	void AddWork(Task t);

	void ParticipateUntil(Task task);

	bool ParticipateUntil(Task task, ManualResetEventSlim predicateEvt, int millisecondsTimeout);

	void PulseAll();
}
