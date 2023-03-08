using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
	[Serializable]
	[DebuggerDisplay("Count = {InnerExceptions.Count}")]
	public class AggregateException : Exception
	{
		private const string defaultMessage = "One or more errors occured";

		private List<Exception> innerExceptions = new List<Exception>();

		public ReadOnlyCollection<Exception> InnerExceptions => innerExceptions.AsReadOnly();

		public AggregateException()
			: base("One or more errors occured")
		{
		}

		public AggregateException(string message)
			: base(message)
		{
		}

		public AggregateException(string message, Exception innerException)
			: base(message, innerException)
		{
			if (innerException == null)
			{
				throw new ArgumentNullException("innerException");
			}
			innerExceptions.Add(innerException);
		}

		protected AggregateException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public AggregateException(params Exception[] innerExceptions)
			: this(string.Empty, innerExceptions)
		{
		}

		public AggregateException(string message, params Exception[] innerExceptions)
			: base(message, (innerExceptions == null || innerExceptions.Length == 0) ? null : innerExceptions[0])
		{
			if (innerExceptions == null)
			{
				throw new ArgumentNullException("innerExceptions");
			}
			foreach (Exception exception in innerExceptions)
			{
				if (exception == null)
				{
					throw new ArgumentException("One of the inner exception is null", "innerExceptions");
				}
			}
			this.innerExceptions.AddRange(innerExceptions);
		}

		public AggregateException(IEnumerable<Exception> innerExceptions)
			: this("One or more errors occured", innerExceptions)
		{
		}

		public AggregateException(string message, IEnumerable<Exception> innerExceptions)
			: this(message, new List<Exception>(innerExceptions).ToArray())
		{
		}

		public AggregateException Flatten()
		{
			List<Exception> inner = new List<Exception>();
			foreach (Exception e in innerExceptions)
			{
				AggregateException aggEx = e as AggregateException;
				if (aggEx != null)
				{
					inner.AddRange(aggEx.Flatten().InnerExceptions);
				}
				else
				{
					inner.Add(e);
				}
			}
			return new AggregateException(inner);
		}

		public void Handle(Func<Exception, bool> predicate)
		{
			List<Exception> failed = new List<Exception>();
			foreach (Exception e in innerExceptions)
			{
				try
				{
					if (!predicate(e))
					{
						failed.Add(e);
					}
				}
				catch
				{
					throw new AggregateException(failed);
				}
			}
			if (failed.Count > 0)
			{
				throw new AggregateException(failed);
			}
		}

		internal void AddChildException(AggregateException childEx)
		{
			if (innerExceptions == null)
			{
				innerExceptions = new List<Exception>();
			}
			if (childEx != null)
			{
				innerExceptions.Add(childEx);
			}
		}

		public override string ToString()
		{
			StringBuilder finalMessage = new StringBuilder(base.ToString());
			int currentIndex = -1;
			foreach (Exception e in innerExceptions)
			{
				finalMessage.Append(Environment.NewLine);
				finalMessage.Append(" --> (Inner exception ");
				finalMessage.Append(++currentIndex);
				finalMessage.Append(") ");
				finalMessage.Append(e.ToString());
				finalMessage.Append(Environment.NewLine);
			}
			return finalMessage.ToString();
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotImplementedException();
		}

		public override Exception GetBaseException()
		{
			if (innerExceptions == null || innerExceptions.Count == 0)
			{
				return this;
			}
			return innerExceptions[0].GetBaseException();
		}
	}
}
