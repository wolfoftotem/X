using System.Collections.Generic;

namespace System.Threading.Tasks;

internal class CircularArray<T>
{
	private readonly int baseSize;

	public readonly int size;

	public readonly T[] segment;

	public long Size => size;

	public T this[long index]
	{
		get
		{
			return segment[index % size];
		}
		set
		{
			segment[index % size] = value;
		}
	}

	public CircularArray(int baseSize)
	{
		this.baseSize = baseSize;
		size = 1 << baseSize;
		segment = new T[size];
	}

	public CircularArray<T> Grow(long bottom, long top)
	{
		CircularArray<T> circularArray = new CircularArray<T>(baseSize + 1);
		for (long num = top; num < bottom; num++)
		{
			circularArray.segment[num] = segment[num % size];
		}
		return circularArray;
	}

	public IEnumerable<T> GetEnumerable(long bottom, ref long top)
	{
		long num = top;
		T[] array = new T[bottom - num];
		int num2 = -1;
		for (long num3 = num; num3 < bottom; num3++)
		{
			array[++num2] = segment[num3 % size];
		}
		return RealGetEnumerable(array, bottom, top, num);
	}

	private IEnumerable<T> RealGetEnumerable(T[] slice, long bottom, long realTop, long initialTop)
	{
		int destIndex = (int)(realTop - initialTop - 1);
		for (long i = realTop; i < bottom; i++)
		{
			int num;
			destIndex = (num = destIndex + 1);
			yield return slice[num];
		}
	}
}
