using System.Collections.Generic;

namespace System.Collections.Concurrent.Partitioners;

internal class ListPartitioner<T> : OrderablePartitioner<T>
{
	private class Range
	{
		public int Actual;

		public readonly int LastIndex;

		public Range(int frm, int lastIndex)
		{
			Actual = frm;
			LastIndex = lastIndex;
		}
	}

	private IList<T> source;

	public ListPartitioner(IList<T> source)
		: base(keysOrderedInEachPartition: true, keysOrderedAcrossPartitions: true, keysNormalized: true)
	{
		this.source = source;
	}

	public override IList<IEnumerator<KeyValuePair<long, T>>> GetOrderablePartitions(int partitionCount)
	{
		if (partitionCount <= 0)
		{
			throw new ArgumentOutOfRangeException("partitionCount");
		}
		IEnumerator<KeyValuePair<long, T>>[] array = new IEnumerator<KeyValuePair<long, T>>[partitionCount];
		int num = source.Count / partitionCount;
		int num2 = 0;
		if (source.Count < partitionCount)
		{
			num = 1;
		}
		else
		{
			num2 = source.Count % partitionCount;
			if (num2 > 0)
			{
				num++;
			}
		}
		int num3 = 0;
		Range[] array2 = new Range[array.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = new Range(num3, num3 + num);
			num3 += num;
			if (--num2 == 0)
			{
				num--;
			}
		}
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = GetEnumeratorForRange(array2, j);
		}
		return array;
	}

	private IEnumerator<KeyValuePair<long, T>> GetEnumeratorForRange(Range[] ranges, int workerIndex)
	{
		if (ranges[workerIndex].Actual >= source.Count)
		{
			return GetEmpty();
		}
		return GetEnumeratorForRangeInternal(ranges, workerIndex);
	}

	private IEnumerator<KeyValuePair<long, T>> GetEmpty()
	{
		yield break;
	}

	private IEnumerator<KeyValuePair<long, T>> GetEnumeratorForRangeInternal(Range[] ranges, int workerIndex)
	{
		Range range = ranges[workerIndex];
		int lastIndex = range.LastIndex;
		int index = range.Actual;
		for (int i = index; i < lastIndex; i = ++range.Actual)
		{
			yield return new KeyValuePair<long, T>(i, source[i]);
		}
	}
}
