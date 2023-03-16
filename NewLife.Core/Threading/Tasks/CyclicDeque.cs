using System.Collections.Generic;

namespace System.Threading.Tasks;

internal class CyclicDeque<T> : IConcurrentDeque<T>
{
	private const int BaseSize = 11;

	private long bottom;

	private long top;

	private long upperBound;

	private CircularArray<T> array = new CircularArray<T>(11);

	public void PushBottom(T obj)
	{
		long num = Interlocked.Read(ref bottom);
		CircularArray<T> circularArray = array;
		if (num - upperBound >= circularArray.Size - 1)
		{
			upperBound = Interlocked.Read(ref top);
			circularArray = (array = circularArray.Grow(num, upperBound));
		}
		circularArray.segment[num % circularArray.size] = obj;
		Interlocked.Increment(ref bottom);
	}

	public PopResult PopBottom(out T obj)
	{
		obj = default(T);
		long num = Interlocked.Decrement(ref bottom);
		CircularArray<T> circularArray = array;
		long num2 = Interlocked.Read(ref top);
		long num3 = num - num2;
		if (num3 < 0)
		{
			Interlocked.Add(ref bottom, num2 - num);
			return PopResult.Empty;
		}
		obj = circularArray.segment[num % circularArray.size];
		if (num3 > 0)
		{
			return PopResult.Succeed;
		}
		Interlocked.Add(ref bottom, num2 + 1 - num);
		if (Interlocked.CompareExchange(ref top, num2 + 1, num2) != num2)
		{
			return PopResult.Empty;
		}
		return PopResult.Succeed;
	}

	public PopResult PopTop(out T obj)
	{
		obj = default(T);
		long num = Interlocked.Read(ref top);
		long num2 = Interlocked.Read(ref bottom);
		if (num2 - num <= 0)
		{
			return PopResult.Empty;
		}
		if (Interlocked.CompareExchange(ref top, num + 1, num) != num)
		{
			return PopResult.Abort;
		}
		CircularArray<T> circularArray = array;
		obj = circularArray.segment[num % circularArray.size];
		return PopResult.Succeed;
	}

	public IEnumerable<T> GetEnumerable()
	{
		CircularArray<T> circularArray = array;
		return circularArray.GetEnumerable(bottom, ref top);
	}
}
