using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System;

[Serializable]
[ComVisible(true)]
public class OperationCanceledException : SystemException
{
	private const int Result = -2146233029;

	private CancellationToken? token;

	public CancellationToken CancellationToken
	{
		get
		{
			if (!token.HasValue)
			{
				return CancellationToken.None;
			}
			return token.Value;
		}
	}

	public OperationCanceledException()
		: base("The operation was canceled.")
	{
		base.HResult = -2146233029;
	}

	public OperationCanceledException(string message)
		: base(message)
	{
		base.HResult = -2146233029;
	}

	public OperationCanceledException(string message, Exception innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233029;
	}

	protected OperationCanceledException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public OperationCanceledException(CancellationToken token)
		: this()
	{
		this.token = token;
	}

	public OperationCanceledException(string message, CancellationToken token)
		: this(message)
	{
		this.token = token;
	}

	public OperationCanceledException(string message, Exception innerException, CancellationToken token)
		: base(message, innerException)
	{
		this.token = token;
	}
}
