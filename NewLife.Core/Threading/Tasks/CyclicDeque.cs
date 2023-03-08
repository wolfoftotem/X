using System.Collections.Generic;

namespace System.Threading.Tasks
{
	internal class CyclicDeque<T> : IConcurrentDeque<T>
	{
		private const int BaseSize = 11;

		private long bottom;

		private long top;

		private long upperBound;

		private CircularArray<T> array = new CircularArray<T>(11);

		public void PushBottom(T obj)
		{
			long b = Interlocked.Read(ref bottom);
			CircularArray<T> a = array;
			if (b - upperBound >= a.Size - 1)
			{
				upperBound = Interlocked.Read(ref top);
				a = (array = a.Grow(b, upperBound));
			}
			a.segment[b % a.size] = obj;
			Interlocked.Increment(ref bottom);
		}

		public PopResult PopBottom(out T obj)
		{
			obj = default(T);
			long b = Interlocked.Decrement(ref bottom);
			CircularArray<T> a = array;
			long t = Interlocked.Read(ref top);
			long size = b - t;
			if (size < 0)
			{
				Interlocked.Add(ref bottom, t - b);
				return PopResult.Empty;
			}
			obj = a.segment[b % a.size];
			if (size > 0)
			{
				return PopResult.Succeed;
			}
			Interlocked.Add(ref bottom, t + 1 - b);
			if (Interlocked.CompareExchange(ref top, t + 1, t) != t)
			{
				return PopResult.Empty;
			}
			return PopResult.Succeed;
		}

		public PopResult PopTop(out T obj)
		{
			obj = default(T);
			long t = Interlocked.Read(ref top);
			long b = Interlocked.Read(ref bottom);
			if (b - t <= 0)
			{
				return PopResult.Empty;
			}
			if (Interlocked.CompareExchange(ref top, t + 1, t) != t)
			{
				return PopResult.Abort;
			}
			CircularArray<T> a = array;
			obj = a.segment[t % a.size];
			return PopResult.Succeed;
		}

		public IEnumerable<T> GetEnumerable()
		{
			CircularArray<T> a = array;
			return a.GetEnumerable(bottom, ref top);
		}
	}
}
