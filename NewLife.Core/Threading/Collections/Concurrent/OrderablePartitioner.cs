using System.Collections.Generic;

namespace System.Collections.Concurrent
{
	public abstract class OrderablePartitioner<TSource> : Partitioner<TSource>
	{
		private class ProxyEnumerator : IEnumerator<TSource>, IEnumerator, IDisposable
		{
			private IEnumerator<KeyValuePair<long, TSource>> internalEnumerator;

			object IEnumerator.Current => Current;

			public TSource Current { get; private set; }

			internal ProxyEnumerator(IEnumerator<KeyValuePair<long, TSource>> enumerator)
			{
				internalEnumerator = enumerator;
			}

			public void Dispose()
			{
				internalEnumerator.Dispose();
			}

			public bool MoveNext()
			{
				if (!internalEnumerator.MoveNext())
				{
					return false;
				}
				Current = internalEnumerator.Current.Value;
				return true;
			}

			public void Reset()
			{
				internalEnumerator.Reset();
			}
		}

        private bool keysOrderedAcrossPartitions;

		private bool keysNormalized;

        public bool KeysOrderedInEachPartition { get; }

        public bool KeysOrderedAcrossPartitions => keysOrderedAcrossPartitions;

		public bool KeysNormalized => keysNormalized;

		protected OrderablePartitioner(bool keysOrderedInEachPartition, bool keysOrderedAcrossPartitions, bool keysNormalized)
		{
			this.KeysOrderedInEachPartition = keysOrderedInEachPartition;
			this.keysOrderedAcrossPartitions = keysOrderedAcrossPartitions;
			this.keysNormalized = keysNormalized;
		}

		public override IEnumerable<TSource> GetDynamicPartitions()
		{
			foreach (KeyValuePair<long, TSource> item in GetOrderableDynamicPartitions())
			{
				KeyValuePair<long, TSource> keyValuePair = item;
				yield return keyValuePair.Value;
			}
		}

		public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
		{
			IEnumerator<TSource>[] temp = new IEnumerator<TSource>[partitionCount];
			IList<IEnumerator<KeyValuePair<long, TSource>>> enumerators = GetOrderablePartitions(partitionCount);
			for (int i = 0; i < enumerators.Count; i++)
			{
				temp[i] = new ProxyEnumerator(enumerators[i]);
			}
			return temp;
		}

		private IEnumerator<TSource> GetProxyEnumerator(IEnumerator<KeyValuePair<long, TSource>> enumerator)
		{
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current.Value;
			}
		}

		public abstract IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount);

		public virtual IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
		{
			if (!SupportsDynamicPartitions)
			{
				throw new NotSupportedException();
			}
			return null;
		}
	}
}
