using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Concurrent.Partitioners;

internal class EnumerablePartitioner<T> : OrderablePartitioner<T>
{
	private class PartitionerState
	{
		public bool Finished;

		public long Index;

		public readonly object SyncLock = new object();
	}

	private const int InitialPartitionSize = 1;

	private const int PartitionMultiplier = 2;

	private IEnumerable<T> source;

	private int initialPartitionSize;

	private int partitionMultiplier;

	public EnumerablePartitioner(IEnumerable<T> source)
		: this(source, 1, 2)
	{
	}

	public EnumerablePartitioner(IEnumerable<T> source, int initialPartitionSize, int partitionMultiplier)
		: base(keysOrderedInEachPartition: true, keysOrderedAcrossPartitions: false, keysNormalized: true)
	{
		this.source = source;
		this.initialPartitionSize = initialPartitionSize;
		this.partitionMultiplier = partitionMultiplier;
	}

	public override IList<IEnumerator<KeyValuePair<long, T>>> GetOrderablePartitions(int partitionCount)
	{
		if (partitionCount <= 0)
		{
			throw new ArgumentOutOfRangeException("partitionCount");
		}
		IEnumerator<KeyValuePair<long, T>>[] array = new IEnumerator<KeyValuePair<long, T>>[partitionCount];
		PartitionerState state = new PartitionerState();
		IEnumerator<T> enumerator = source.GetEnumerator();
		bool flag = initialPartitionSize == 1 && partitionMultiplier == 1;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (flag ? GetPartitionEnumeratorSimple(enumerator, state, i == array.Length - 1) : GetPartitionEnumerator(enumerator, state));
		}
		return array;
	}

	private IEnumerator<KeyValuePair<long, T>> GetPartitionEnumeratorSimple(IEnumerator<T> src, PartitionerState state, bool last)
	{
		long index = -1L;
		T value = default(T);
		try
		{
			do
			{
				object syncLock;
				object obj = (syncLock = state.SyncLock);
				Monitor.Enter(syncLock);
				try
				{
					if (state.Finished || (state.Finished = !src.MoveNext()))
					{
						break;
					}
					index = state.Index++;
					value = src.Current;
				}
				finally
				{
					Monitor.Exit(obj);
				}
				yield return new KeyValuePair<long, T>(index, value);
			}
			while (!state.Finished);
		}
		finally
		{
			if (last)
			{
				src.Dispose();
			}
		}
	}

	private IEnumerator<KeyValuePair<long, T>> GetPartitionEnumerator(IEnumerator<T> src, PartitionerState state)
	{
		int count = initialPartitionSize;
		List<T> list = new List<T>();
		while (!state.Finished)
		{
			list.Clear();
			long ind = -1L;
			object syncLock;
			object obj = (syncLock = state.SyncLock);
			Monitor.Enter(syncLock);
			try
			{
				if (state.Finished)
				{
					break;
				}
				ind = state.Index;
				for (int j = 0; j < count; j++)
				{
					if (state.Finished = !src.MoveNext())
					{
						if (list.Count == 0)
						{
							yield break;
						}
						break;
					}
					list.Add(src.Current);
					state.Index++;
				}
			}
			finally
			{
				Monitor.Exit(obj);
			}
			for (int i = 0; i < list.Count; i++)
			{
				yield return new KeyValuePair<long, T>(ind + i, list[i]);
			}
			count *= partitionMultiplier;
		}
	}
}
