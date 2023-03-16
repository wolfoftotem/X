namespace System.Threading.Tasks;

internal class ActionContinuation : IContinuation
{
	private readonly Action action;

	public ActionContinuation(Action action)
	{
		this.action = action;
	}

	public void Execute()
	{
		action();
	}
}
