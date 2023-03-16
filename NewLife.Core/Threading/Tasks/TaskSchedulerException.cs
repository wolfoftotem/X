using System.Runtime.Serialization;

namespace System.Threading.Tasks;

public class TaskSchedulerException : Exception
{
	private const string exceptionDefaultMessage = "An exception was thrown by a TaskScheduler";

	public TaskSchedulerException()
		: base("An exception was thrown by a TaskScheduler")
	{
	}

	public TaskSchedulerException(string message)
		: base(message)
	{
	}

	protected TaskSchedulerException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public TaskSchedulerException(Exception innerException)
		: base("An exception was thrown by a TaskScheduler", innerException)
	{
	}

	public TaskSchedulerException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
