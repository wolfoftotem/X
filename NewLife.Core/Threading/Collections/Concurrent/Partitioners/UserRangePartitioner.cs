using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Concurrent.Partitioners
{
	internal class UserRangePartitioner : OrderablePartitioner<Tuple<int, int>>
	{
		private readonly int start;

		private readonly int end;

		private readonly int rangeSize;

		public UserRangePartitioner(int start, int end, int rangeSize)
			: base(keysOrderedInEachPartition: true, keysOrderedAcrossPartitions: true, keysNormalized: true)
		{
			this.start = start;
			this.end = end;
			this.rangeSize = rangeSize;
		}

		public override IList<IEnumerator<KeyValuePair<long, Tuple<int, int>>>> GetOrderablePartitions(int partitionCount)
		{
			if (partitionCount <= 0)
			{
				throw new ArgumentOutOfRangeException("partitionCount");
			}
			int currentIndex = 0;
			Func<int> getNextIndex = () => Interlocked.Increment(ref currentIndex) - 1;
			IEnumerator<KeyValuePair<long, Tuple<int, int>>>[] enumerators = new IEnumerator<KeyValuePair<long, Tuple<int, int>>>[partitionCount];
			for (int i = 0; i < partitionCount; i++)
			{
				enumerators[i] = GetEnumerator(getNextIndex);
			}
			return enumerators;
		}

		private IEnumerator<KeyValuePair<long, Tuple<int, int>>> GetEnumerator(Func<int> getNextIndex)
		{
			while (true)
			{
				int index = getNextIndex();
				int sliceStart = index * rangeSize + start;
				if (sliceStart < end)
				{
					yield return new KeyValuePair<long, Tuple<int, int>>(index, Tuple.Create(sliceStart, Math.Min(end, sliceStart + rangeSize)));
					_ = sliceStart + rangeSize;
					continue;
				}
				break;
			}
		}
	}
}
