namespace System.Threading.Tasks
{
	internal class ManualEventSlot : IContinuation
	{
		private ManualResetEventSlim evt;

		public ManualEventSlot(ManualResetEventSlim evt)
		{
			this.evt = evt;
		}

		public void Execute()
		{
			evt.Set();
		}
	}
}
