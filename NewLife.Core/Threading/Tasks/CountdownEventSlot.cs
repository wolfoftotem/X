namespace System.Threading.Tasks
{
	internal class CountdownEventSlot : IContinuation
	{
		private CountdownEvent evt;

		public CountdownEventSlot(CountdownEvent evt)
		{
			this.evt = evt;
		}

		public void Execute()
		{
			evt.Signal();
		}
	}
}
