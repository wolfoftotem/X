using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Concurrent.Partitioners;

internal class UserLongRangePartitioner : OrderablePartitioner<Tuple<long, long>>
{
	private readonly long start;

	private readonly long end;

	private readonly long rangeSize;

	public UserLongRangePartitioner(long start, long end, long rangeSize)
		: base(keysOrderedInEachPartition: true, keysOrderedAcrossPartitions: true, keysNormalized: true)
	{
		this.start = start;
		this.end = end;
		this.rangeSize = rangeSize;
	}

	public override IList<IEnumerator<KeyValuePair<long, Tuple<long, long>>>> GetOrderablePartitions(int partitionCount)
	{
		if (partitionCount <= 0)
		{
			throw new ArgumentOutOfRangeException("partitionCount");
		}
		long currentIndex = 0L;
		Func<long> getNextIndex = () => Interlocked.Increment(ref currentIndex) - 1;
		IEnumerator<KeyValuePair<long, Tuple<long, long>>>[] array = new IEnumerator<KeyValuePair<long, Tuple<long, long>>>[partitionCount];
		for (int i = 0; i < partitionCount; i++)
		{
			array[i] = GetEnumerator(getNextIndex);
		}
		return array;
	}

	private IEnumerator<KeyValuePair<long, Tuple<long, long>>> GetEnumerator(Func<long> getNextIndex)
	{
		while (true)
		{
			long index = getNextIndex();
			long sliceStart = index * rangeSize + start;
			if (sliceStart < end)
			{
				yield return new KeyValuePair<long, Tuple<long, long>>(index, Tuple.Create(sliceStart, Math.Min(end, sliceStart + rangeSize)));
				_ = sliceStart + rangeSize;
				continue;
			}
			break;
		}
	}
}
