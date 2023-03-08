using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Concurrent
{
	[DebuggerTypeProxy(typeof(CollectionDebuggerView<, >))]
	[DebuggerDisplay("Count={Count}")]
	public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary, ICollection, IEnumerable
	{
		private class ConcurrentDictionaryEnumerator : IDictionaryEnumerator, IEnumerator
		{
			private IEnumerator<KeyValuePair<TKey, TValue>> internalEnum;

			public object Current => Entry;

			public DictionaryEntry Entry
			{
				get
				{
					KeyValuePair<TKey, TValue> current = internalEnum.Current;
					return new DictionaryEntry(current.Key, current.Value);
				}
			}

			public object Key => internalEnum.Current.Key;

			public object Value => internalEnum.Current.Value;

			public ConcurrentDictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> internalEnum)
			{
				this.internalEnum = internalEnum;
			}

			public bool MoveNext()
			{
				return internalEnum.MoveNext();
			}

			public void Reset()
			{
				internalEnum.Reset();
			}
		}

		private IEqualityComparer<TKey> comparer;

		private SplitOrderedList<TKey, KeyValuePair<TKey, TValue>> internalDictionary;

		public TValue this[TKey key]
		{
			get
			{
				return GetValue(key);
			}
			set
			{
				AddOrUpdate(key, value, value);
			}
		}

		object IDictionary.this[object key]
		{
			get
			{
				if (!(key is TKey))
				{
					throw new ArgumentException("key isn't of correct type", "key");
				}
				return this[(TKey)key];
			}
			set
			{
				if (!(key is TKey) || !(value is TValue))
				{
					throw new ArgumentException("key or value aren't of correct type");
				}
				this[(TKey)key] = (TValue)value;
			}
		}

		public int Count => internalDictionary.Count;

		public bool IsEmpty => Count == 0;

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

		bool IDictionary.IsReadOnly => false;

		public ICollection<TKey> Keys => GetPart((KeyValuePair<TKey, TValue> kvp) => kvp.Key);

		public ICollection<TValue> Values => GetPart((KeyValuePair<TKey, TValue> kvp) => kvp.Value);

		ICollection IDictionary.Keys => (ICollection)Keys;

		ICollection IDictionary.Values => (ICollection)Values;

		object ICollection.SyncRoot => this;

		bool IDictionary.IsFixedSize => false;

		bool ICollection.IsSynchronized => true;

		public ConcurrentDictionary()
			: this((IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
		{
		}

		public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
			: this(collection, (IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
		{
		}

		public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
		{
			this.comparer = comparer;
			internalDictionary = new SplitOrderedList<TKey, KeyValuePair<TKey, TValue>>(comparer);
		}

		public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
			: this(comparer)
		{
			foreach (KeyValuePair<TKey, TValue> pair in collection)
			{
				Add(pair.Key, pair.Value);
			}
		}

		public ConcurrentDictionary(int concurrencyLevel, int capacity)
			: this((IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
		{
		}

		public ConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
			: this(collection, comparer)
		{
		}

		public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
			: this(comparer)
		{
		}

		private void Add(TKey key, TValue value)
		{
			while (!TryAdd(key, value))
			{
			}
		}

		void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
		{
			Add(key, value);
		}

		public bool TryAdd(TKey key, TValue value)
		{
			return internalDictionary.Insert(Hash(key), key, Make(key, value));
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> pair)
		{
			Add(pair.Key, pair.Value);
		}

		public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
		{
			return internalDictionary.InsertOrUpdate(Hash(key), key, () => Make(key, addValueFactory(key)), (KeyValuePair<TKey, TValue> e) => Make(key, updateValueFactory(key, e.Value))).Value;
		}

		public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
		{
			return AddOrUpdate(key, (TKey _) => addValue, updateValueFactory);
		}

		private TValue AddOrUpdate(TKey key, TValue addValue, TValue updateValue)
		{
			return internalDictionary.InsertOrUpdate(Hash(key), key, Make(key, addValue), Make(key, updateValue)).Value;
		}

		private TValue GetValue(TKey key)
		{
			if (!TryGetValue(key, out var temp))
			{
				throw new KeyNotFoundException(key.ToString());
			}
			return temp;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			KeyValuePair<TKey, TValue> pair;
			bool result = internalDictionary.Find(Hash(key), key, out pair);
			value = pair.Value;
			return result;
		}

		public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
		{
			return internalDictionary.CompareExchange(Hash(key), key, Make(key, newValue), (KeyValuePair<TKey, TValue> e) => e.Value.Equals(comparisonValue));
		}

		public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
		{
			return internalDictionary.InsertOrGet(Hash(key), key, Make(key, default(TValue)), () => Make(key, valueFactory(key))).Value;
		}

		public TValue GetOrAdd(TKey key, TValue value)
		{
			return internalDictionary.InsertOrGet(Hash(key), key, Make(key, value), null).Value;
		}

		public bool TryRemove(TKey key, out TValue value)
		{
			KeyValuePair<TKey, TValue> data;
			bool result = internalDictionary.Delete(Hash(key), key, out data);
			value = data.Value;
			return result;
		}

		private bool Remove(TKey key)
		{
			TValue dummy;
			return TryRemove(key, out dummy);
		}

		bool IDictionary<TKey, TValue>.Remove(TKey key)
		{
			return Remove(key);
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair)
		{
			return Remove(pair.Key);
		}

		public bool ContainsKey(TKey key)
		{
			KeyValuePair<TKey, TValue> dummy;
			return internalDictionary.Find(Hash(key), key, out dummy);
		}

		bool IDictionary.Contains(object key)
		{
			if (!(key is TKey))
			{
				return false;
			}
			return ContainsKey((TKey)key);
		}

		void IDictionary.Remove(object key)
		{
			if (key is TKey)
			{
				Remove((TKey)key);
			}
		}

		void IDictionary.Add(object key, object value)
		{
			if (!(key is TKey) || !(value is TValue))
			{
				throw new ArgumentException("key or value aren't of correct type");
			}
			Add((TKey)key, (TValue)value);
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair)
		{
			return ContainsKey(pair.Key);
		}

		public KeyValuePair<TKey, TValue>[] ToArray()
		{
			return new List<KeyValuePair<TKey, TValue>>(this).ToArray();
		}

		public void Clear()
		{
			internalDictionary = new SplitOrderedList<TKey, KeyValuePair<TKey, TValue>>(comparer);
		}

		private ICollection<T> GetPart<T>(Func<KeyValuePair<TKey, TValue>, T> extractor)
		{
			List<T> temp = new List<T>();
			using (IEnumerator<KeyValuePair<TKey, TValue>> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<TKey, TValue> kvp = enumerator.Current;
					temp.Add(extractor(kvp));
				}
			}
			return temp.AsReadOnly();
		}

		void ICollection.CopyTo(Array array, int startIndex)
		{
			KeyValuePair<TKey, TValue>[] arr = array as KeyValuePair<TKey, TValue>[];
			if (arr != null)
			{
				CopyTo(arr, startIndex, Count);
			}
		}

		private void CopyTo(KeyValuePair<TKey, TValue>[] array, int startIndex)
		{
			CopyTo(array, startIndex, Count);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int startIndex)
		{
			CopyTo(array, startIndex);
		}

		private void CopyTo(KeyValuePair<TKey, TValue>[] array, int startIndex, int num)
		{
			using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<TKey, TValue> kvp = enumerator.Current;
				array[startIndex++] = kvp;
				if (--num <= 0)
				{
					break;
				}
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return GetEnumeratorInternal();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumeratorInternal();
		}

		private IEnumerator<KeyValuePair<TKey, TValue>> GetEnumeratorInternal()
		{
			return internalDictionary.GetEnumerator();
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return new ConcurrentDictionaryEnumerator(GetEnumeratorInternal());
		}

		private static KeyValuePair<U, V> Make<U, V>(U key, V value)
		{
			return new KeyValuePair<U, V>(key, value);
		}

		private uint Hash(TKey key)
		{
			return (uint)comparer.GetHashCode(key);
		}
	}
}
