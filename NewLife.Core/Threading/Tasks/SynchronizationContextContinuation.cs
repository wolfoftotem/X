namespace System.Threading.Tasks
{
	internal class SynchronizationContextContinuation : IContinuation
	{
		private readonly Action action;

		private readonly SynchronizationContext ctx;

		public SynchronizationContextContinuation(Action action, SynchronizationContext ctx)
		{
			this.action = action;
			this.ctx = ctx;
		}

		public void Execute()
		{
			ctx.Post(delegate(object l)
			{
				((Action)l)();
			}, action);
		}
	}
}
