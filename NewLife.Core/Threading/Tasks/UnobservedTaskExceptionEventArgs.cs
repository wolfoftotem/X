namespace System.Threading.Tasks;

public class UnobservedTaskExceptionEventArgs : EventArgs
{
	private AggregateException exception;

	private bool wasObserved;

	public AggregateException Exception => exception;

	public bool Observed => wasObserved;

	public UnobservedTaskExceptionEventArgs(AggregateException exception)
	{
		this.exception = exception;
	}

	public void SetObserved()
	{
		wasObserved = true;
	}
}
