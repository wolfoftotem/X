namespace System.Threading.Tasks
{
	public class UnobservedTaskExceptionEventArgs : EventArgs
	{
        private bool wasObserved;

        public AggregateException Exception { get; }

        public bool Observed => wasObserved;

		public UnobservedTaskExceptionEventArgs(AggregateException exception)
		{
			this.Exception = exception;
		}

		public void SetObserved()
		{
			wasObserved = true;
		}
	}
}
