using System.Collections.Generic;

namespace System.Collections.Concurrent.Partitioners
{
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
			IEnumerator<KeyValuePair<long, T>>[] enumerators = new IEnumerator<KeyValuePair<long, T>>[partitionCount];
			int count = source.Count / partitionCount;
			int extra = 0;
			if (source.Count < partitionCount)
			{
				count = 1;
			}
			else
			{
				extra = source.Count % partitionCount;
				if (extra > 0)
				{
					count++;
				}
			}
			int currentIndex = 0;
			Range[] ranges = new Range[enumerators.Length];
			for (int j = 0; j < ranges.Length; j++)
			{
				ranges[j] = new Range(currentIndex, currentIndex + count);
				currentIndex += count;
				if (--extra == 0)
				{
					count--;
				}
			}
			for (int i = 0; i < enumerators.Length; i++)
			{
				enumerators[i] = GetEnumeratorForRange(ranges, i);
			}
			return enumerators;
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
}
