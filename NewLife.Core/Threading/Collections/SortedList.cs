using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Collections
{
	[Serializable]
	[DebuggerDisplay("Count={Count}")]
	[ComVisible(true)]
	[DebuggerTypeProxy(typeof(CollectionDebuggerView))]
	public class SortedList : IDictionary, ICollection, IEnumerable
	{
		[Serializable]
		internal struct Slot
		{
			internal object key;

			internal object value;
		}

		private enum EnumeratorMode
		{
			KEY_MODE,
			VALUE_MODE,
			ENTRY_MODE
		}

		private sealed class Enumerator : IDictionaryEnumerator, IEnumerator
		{
			private SortedList host;

			private object currentKey;

			private object currentValue;

			private int stamp;

			private int pos;

			private int size;

			private EnumeratorMode mode;

			private bool invalid;

			private static readonly string xstr = "SortedList.Enumerator: snapshot out of sync.";

			public DictionaryEntry Entry
			{
				get
				{
					if (invalid || pos >= size || pos == -1)
					{
						throw new InvalidOperationException(xstr);
					}
					return new DictionaryEntry(currentKey, currentValue);
				}
			}

			public object Key
			{
				get
				{
					if (invalid || pos >= size || pos == -1)
					{
						throw new InvalidOperationException(xstr);
					}
					return currentKey;
				}
			}

			public object Value
			{
				get
				{
					if (invalid || pos >= size || pos == -1)
					{
						throw new InvalidOperationException(xstr);
					}
					return currentValue;
				}
			}

			public object Current
			{
				get
				{
					if (invalid || pos >= size || pos == -1)
					{
						throw new InvalidOperationException(xstr);
					}
					return mode switch
					{
						EnumeratorMode.KEY_MODE => currentKey, 
						EnumeratorMode.VALUE_MODE => currentValue, 
						EnumeratorMode.ENTRY_MODE => Entry, 
						_ => throw new NotSupportedException(string.Concat(mode, " is not a supported mode.")), 
					};
				}
			}

			public Enumerator(SortedList host, EnumeratorMode mode)
			{
				this.host = host;
				stamp = host.modificationCount;
				size = host.Count;
				this.mode = mode;
				Reset();
			}

			public Enumerator(SortedList host)
				: this(host, EnumeratorMode.ENTRY_MODE)
			{
			}

			public void Reset()
			{
				if (host.modificationCount != stamp || invalid)
				{
					throw new InvalidOperationException(xstr);
				}
				pos = -1;
				currentKey = null;
				currentValue = null;
			}

			public bool MoveNext()
			{
				if (host.modificationCount != stamp || invalid)
				{
					throw new InvalidOperationException(xstr);
				}
				Slot[] table = host.table;
				if (++pos < size)
				{
					Slot entry = table[pos];
					currentKey = entry.key;
					currentValue = entry.value;
					return true;
				}
				currentKey = null;
				currentValue = null;
				return false;
			}

			public object Clone()
			{
				Enumerator e = new Enumerator(host, mode);
				e.stamp = stamp;
				e.pos = pos;
				e.size = size;
				e.currentKey = currentKey;
				e.currentValue = currentValue;
				e.invalid = invalid;
				return e;
			}
		}

		[Serializable]
		private class ListKeys : IList, ICollection, IEnumerable
		{
			private SortedList host;

			public virtual int Count => host.Count;

			public virtual bool IsSynchronized => host.IsSynchronized;

			public virtual object SyncRoot => host.SyncRoot;

			public virtual bool IsFixedSize => true;

			public virtual bool IsReadOnly => true;

			public virtual object this[int index]
			{
				get
				{
					return host.GetKey(index);
				}
				set
				{
					throw new NotSupportedException("attempt to modify a key");
				}
			}

			public ListKeys(SortedList host)
			{
				if (host == null)
				{
					throw new ArgumentNullException();
				}
				this.host = host;
			}

			public virtual void CopyTo(Array array, int arrayIndex)
			{
				host.CopyToArray(array, arrayIndex, EnumeratorMode.KEY_MODE);
			}

			public virtual int Add(object value)
			{
				throw new NotSupportedException("IList::Add not supported");
			}

			public virtual void Clear()
			{
				throw new NotSupportedException("IList::Clear not supported");
			}

			public virtual bool Contains(object key)
			{
				return host.Contains(key);
			}

			public virtual int IndexOf(object key)
			{
				return host.IndexOfKey(key);
			}

			public virtual void Insert(int index, object value)
			{
				throw new NotSupportedException("IList::Insert not supported");
			}

			public virtual void Remove(object value)
			{
				throw new NotSupportedException("IList::Remove not supported");
			}

			public virtual void RemoveAt(int index)
			{
				throw new NotSupportedException("IList::RemoveAt not supported");
			}

			public virtual IEnumerator GetEnumerator()
			{
				return new Enumerator(host, EnumeratorMode.KEY_MODE);
			}
		}

		[Serializable]
		private class ListValues : IList, ICollection, IEnumerable
		{
			private SortedList host;

			public virtual int Count => host.Count;

			public virtual bool IsSynchronized => host.IsSynchronized;

			public virtual object SyncRoot => host.SyncRoot;

			public virtual bool IsFixedSize => true;

			public virtual bool IsReadOnly => true;

			public virtual object this[int index]
			{
				get
				{
					return host.GetByIndex(index);
				}
				set
				{
					throw new NotSupportedException("This operation is not supported on GetValueList return");
				}
			}

			public ListValues(SortedList host)
			{
				if (host == null)
				{
					throw new ArgumentNullException();
				}
				this.host = host;
			}

			public virtual void CopyTo(Array array, int arrayIndex)
			{
				host.CopyToArray(array, arrayIndex, EnumeratorMode.VALUE_MODE);
			}

			public virtual int Add(object value)
			{
				throw new NotSupportedException("IList::Add not supported");
			}

			public virtual void Clear()
			{
				throw new NotSupportedException("IList::Clear not supported");
			}

			public virtual bool Contains(object value)
			{
				return host.ContainsValue(value);
			}

			public virtual int IndexOf(object value)
			{
				return host.IndexOfValue(value);
			}

			public virtual void Insert(int index, object value)
			{
				throw new NotSupportedException("IList::Insert not supported");
			}

			public virtual void Remove(object value)
			{
				throw new NotSupportedException("IList::Remove not supported");
			}

			public virtual void RemoveAt(int index)
			{
				throw new NotSupportedException("IList::RemoveAt not supported");
			}

			public virtual IEnumerator GetEnumerator()
			{
				return new Enumerator(host, EnumeratorMode.VALUE_MODE);
			}
		}

		private class SynchedSortedList : SortedList
		{
			private SortedList host;

			public override int Capacity
			{
				get
				{
					lock (host.SyncRoot)
					{
						return host.Capacity;
					}
				}
				set
				{
					lock (host.SyncRoot)
					{
						host.Capacity = value;
					}
				}
			}

			public override int Count => host.Count;

			public override bool IsSynchronized => true;

			public override object SyncRoot => host.SyncRoot;

			public override bool IsFixedSize => host.IsFixedSize;

			public override bool IsReadOnly => host.IsReadOnly;

			public override ICollection Keys
			{
				get
				{
					ICollection keys = null;
					lock (host.SyncRoot)
					{
						return host.Keys;
					}
				}
			}

			public override ICollection Values
			{
				get
				{
					ICollection vals = null;
					lock (host.SyncRoot)
					{
						return host.Values;
					}
				}
			}

			public override object this[object key]
			{
				get
				{
					lock (host.SyncRoot)
					{
						return host.GetImpl(key);
					}
				}
				set
				{
					lock (host.SyncRoot)
					{
						host.PutImpl(key, value, overwrite: true);
					}
				}
			}

			public SynchedSortedList(SortedList host)
			{
				if (host == null)
				{
					throw new ArgumentNullException();
				}
				this.host = host;
			}

			public override void CopyTo(Array array, int arrayIndex)
			{
				lock (host.SyncRoot)
				{
					host.CopyTo(array, arrayIndex);
				}
			}

			public override void Add(object key, object value)
			{
				lock (host.SyncRoot)
				{
					host.PutImpl(key, value, overwrite: false);
				}
			}

			public override void Clear()
			{
				lock (host.SyncRoot)
				{
					host.Clear();
				}
			}

			public override bool Contains(object key)
			{
				lock (host.SyncRoot)
				{
					return host.Find(key) >= 0;
				}
			}

			public override IDictionaryEnumerator GetEnumerator()
			{
				lock (host.SyncRoot)
				{
					return host.GetEnumerator();
				}
			}

			public override void Remove(object key)
			{
				lock (host.SyncRoot)
				{
					host.Remove(key);
				}
			}

			public override bool ContainsKey(object key)
			{
				lock (host.SyncRoot)
				{
					return host.Contains(key);
				}
			}

			public override bool ContainsValue(object value)
			{
				lock (host.SyncRoot)
				{
					return host.ContainsValue(value);
				}
			}

			public override object Clone()
			{
				lock (host.SyncRoot)
				{
					return host.Clone() as SortedList;
				}
			}

			public override object GetByIndex(int index)
			{
				lock (host.SyncRoot)
				{
					return host.GetByIndex(index);
				}
			}

			public override object GetKey(int index)
			{
				lock (host.SyncRoot)
				{
					return host.GetKey(index);
				}
			}

			public override IList GetKeyList()
			{
				lock (host.SyncRoot)
				{
					return new ListKeys(host);
				}
			}

			public override IList GetValueList()
			{
				lock (host.SyncRoot)
				{
					return new ListValues(host);
				}
			}

			public override void RemoveAt(int index)
			{
				lock (host.SyncRoot)
				{
					host.RemoveAt(index);
				}
			}

			public override int IndexOfKey(object key)
			{
				lock (host.SyncRoot)
				{
					return host.IndexOfKey(key);
				}
			}

			public override int IndexOfValue(object val)
			{
				lock (host.SyncRoot)
				{
					return host.IndexOfValue(val);
				}
			}

			public override void SetByIndex(int index, object value)
			{
				lock (host.SyncRoot)
				{
					host.SetByIndex(index, value);
				}
			}

			public override void TrimToSize()
			{
				lock (host.SyncRoot)
				{
					host.TrimToSize();
				}
			}
		}

		private const int INITIAL_SIZE = 16;

		private Slot[] table;

		private IComparer comparer;

		private int inUse;

		private int modificationCount;

		private int defaultCapacity;

		public virtual int Count => inUse;

		public virtual bool IsSynchronized => false;

		public virtual object SyncRoot => this;

		public virtual bool IsFixedSize => false;

		public virtual bool IsReadOnly => false;

		public virtual ICollection Keys => new ListKeys(this);

		public virtual ICollection Values => new ListValues(this);

		public virtual object this[object key]
		{
			get
			{
				if (key == null)
				{
					throw new ArgumentNullException();
				}
				return GetImpl(key);
			}
			set
			{
				if (key == null)
				{
					throw new ArgumentNullException();
				}
				if (IsReadOnly)
				{
					throw new NotSupportedException("SortedList is Read Only.");
				}
				if (Find(key) < 0 && IsFixedSize)
				{
					throw new NotSupportedException("Key not found and SortedList is fixed size.");
				}
				PutImpl(key, value, overwrite: true);
			}
		}

		public virtual int Capacity
		{
			get
			{
				return table.Length;
			}
			set
			{
				int current = table.Length;
				if (inUse > value)
				{
					throw new ArgumentOutOfRangeException("capacity too small");
				}
				if (value == 0)
				{
					Slot[] newTable3 = new Slot[defaultCapacity];
					Array.Copy(table, newTable3, inUse);
					table = newTable3;
				}
				else if (value > inUse)
				{
					Slot[] newTable2 = new Slot[value];
					Array.Copy(table, newTable2, inUse);
					table = newTable2;
				}
				else if (value > current)
				{
					Slot[] newTable = new Slot[value];
					Array.Copy(table, newTable, current);
					table = newTable;
				}
			}
		}

		public SortedList()
			: this(null, 16)
		{
		}

		public SortedList(int initialCapacity)
			: this(null, initialCapacity)
		{
		}

		public SortedList(IComparer comparer, int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity");
			}
			if (capacity == 0)
			{
				defaultCapacity = 0;
			}
			else
			{
				defaultCapacity = 16;
			}
			this.comparer = comparer;
			InitTable(capacity, forceSize: true);
		}

		public SortedList(IComparer comparer)
		{
			this.comparer = comparer;
			InitTable(16, forceSize: true);
		}

		public SortedList(IDictionary d)
			: this(d, null)
		{
		}

		public SortedList(IDictionary d, IComparer comparer)
		{
			if (d == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			InitTable(d.Count, forceSize: true);
			this.comparer = comparer;
			IDictionaryEnumerator it = d.GetEnumerator();
			while (it.MoveNext())
			{
				Add(it.Key, it.Value);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this, EnumeratorMode.ENTRY_MODE);
		}

		public virtual void Add(object key, object value)
		{
			PutImpl(key, value, overwrite: false);
		}

		public virtual void Clear()
		{
			defaultCapacity = 16;
			table = new Slot[defaultCapacity];
			inUse = 0;
			modificationCount++;
		}

		public virtual bool Contains(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException();
			}
			try
			{
				return Find(key) >= 0;
			}
			catch (Exception)
			{
				throw new InvalidOperationException();
			}
		}

		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return new Enumerator(this, EnumeratorMode.ENTRY_MODE);
		}

		public virtual void Remove(object key)
		{
			int i = IndexOfKey(key);
			if (i >= 0)
			{
				RemoveAt(i);
			}
		}

		public virtual void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException();
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			if (array.Rank > 1)
			{
				throw new ArgumentException("array is multi-dimensional");
			}
			if (arrayIndex >= array.Length)
			{
				throw new ArgumentNullException("arrayIndex is greater than or equal to array.Length");
			}
			if (Count > array.Length - arrayIndex)
			{
				throw new ArgumentNullException("Not enough space in array from arrayIndex to end of array");
			}
			IDictionaryEnumerator it = GetEnumerator();
			int i = arrayIndex;
			while (it.MoveNext())
			{
				array.SetValue(it.Entry, i++);
			}
		}

		public virtual object Clone()
		{
			SortedList sl = new SortedList(this, comparer);
			sl.modificationCount = modificationCount;
			return sl;
		}

		public virtual IList GetKeyList()
		{
			return new ListKeys(this);
		}

		public virtual IList GetValueList()
		{
			return new ListValues(this);
		}

		public virtual void RemoveAt(int index)
		{
			Slot[] table = this.table;
			int cnt = Count;
			if (index >= 0 && index < cnt)
			{
				if (index != cnt - 1)
				{
					Array.Copy(table, index + 1, table, index, cnt - 1 - index);
				}
				else
				{
					table[index].key = null;
					table[index].value = null;
				}
				inUse--;
				modificationCount++;
				return;
			}
			throw new ArgumentOutOfRangeException("index out of range");
		}

		public virtual int IndexOfKey(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException();
			}
			int indx = 0;
			try
			{
				indx = Find(key);
			}
			catch (Exception)
			{
				throw new InvalidOperationException();
			}
			return indx | (indx >> 31);
		}

		public virtual int IndexOfValue(object value)
		{
			if (inUse == 0)
			{
				return -1;
			}
			for (int i = 0; i < inUse; i++)
			{
				Slot current = table[i];
				if (object.Equals(value, current.value))
				{
					return i;
				}
			}
			return -1;
		}

		public virtual bool ContainsKey(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException();
			}
			try
			{
				return Contains(key);
			}
			catch (Exception)
			{
				throw new InvalidOperationException();
			}
		}

		public virtual bool ContainsValue(object value)
		{
			return IndexOfValue(value) >= 0;
		}

		public virtual object GetByIndex(int index)
		{
			if (index >= 0 && index < Count)
			{
				return table[index].value;
			}
			throw new ArgumentOutOfRangeException("index out of range");
		}

		public virtual void SetByIndex(int index, object value)
		{
			if (index >= 0 && index < Count)
			{
				table[index].value = value;
				return;
			}
			throw new ArgumentOutOfRangeException("index out of range");
		}

		public virtual object GetKey(int index)
		{
			if (index >= 0 && index < Count)
			{
				return table[index].key;
			}
			throw new ArgumentOutOfRangeException("index out of range");
		}

		public static SortedList Synchronized(SortedList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("Base list is null.");
			}
			return new SynchedSortedList(list);
		}

		public virtual void TrimToSize()
		{
			if (Count == 0)
			{
				Resize(defaultCapacity, copy: false);
			}
			else
			{
				Resize(Count, copy: true);
			}
		}

		private void Resize(int n, bool copy)
		{
			Slot[] table = this.table;
			Slot[] newTable = new Slot[n];
			if (copy)
			{
				Array.Copy(table, 0, newTable, 0, n);
			}
			this.table = newTable;
		}

		private void EnsureCapacity(int n, int free)
		{
			Slot[] table = this.table;
			Slot[] newTable = null;
			int cap = Capacity;
			bool gap = free >= 0 && free < Count;
			if (n > cap)
			{
				newTable = new Slot[n << 1];
			}
			if (newTable != null)
			{
				if (gap)
				{
					int copyLen = free;
					if (copyLen > 0)
					{
						Array.Copy(table, 0, newTable, 0, copyLen);
					}
					copyLen = Count - free;
					if (copyLen > 0)
					{
						Array.Copy(table, free, newTable, free + 1, copyLen);
					}
				}
				else
				{
					Array.Copy(table, newTable, Count);
				}
				this.table = newTable;
			}
			else if (gap)
			{
				Array.Copy(table, free, table, free + 1, Count - free);
			}
		}

		private void PutImpl(object key, object value, bool overwrite)
		{
			if (key == null)
			{
				throw new ArgumentNullException("null key");
			}
			Slot[] table = this.table;
			int freeIndx = -1;
			try
			{
				freeIndx = Find(key);
			}
			catch (Exception)
			{
				throw new InvalidOperationException();
			}
			if (freeIndx >= 0)
			{
				if (!overwrite)
				{
					string msg = $"Key '{key}' already exists in list.";
					throw new ArgumentException(msg);
				}
				table[freeIndx].value = value;
				modificationCount++;
				return;
			}
			freeIndx = ~freeIndx;
			if (freeIndx > Capacity + 1)
			{
				throw new Exception(string.Concat("SortedList::internal error (", key, ", ", value, ") at [", freeIndx, "]"));
			}
			EnsureCapacity(Count + 1, freeIndx);
			table = this.table;
			table[freeIndx].key = key;
			table[freeIndx].value = value;
			inUse++;
			modificationCount++;
		}

		private object GetImpl(object key)
		{
			int i = Find(key);
			if (i >= 0)
			{
				return table[i].value;
			}
			return null;
		}

		private void InitTable(int capacity, bool forceSize)
		{
			if (!forceSize && capacity < defaultCapacity)
			{
				capacity = defaultCapacity;
			}
			table = new Slot[capacity];
			inUse = 0;
			modificationCount = 0;
		}

		private void CopyToArray(Array arr, int i, EnumeratorMode mode)
		{
			if (arr == null)
			{
				throw new ArgumentNullException("arr");
			}
			if (i < 0 || i + Count > arr.Length)
			{
				throw new ArgumentOutOfRangeException("i");
			}
			IEnumerator it = new Enumerator(this, mode);
			while (it.MoveNext())
			{
				arr.SetValue(it.Current, i++);
			}
		}

		private int Find(object key)
		{
			Slot[] table = this.table;
			int len = Count;
			if (len == 0)
			{
				return -1;
			}
			IComparer comparer = ((this.comparer == null) ? Comparer.Default : this.comparer);
			int left = 0;
			int right = len - 1;
			while (left <= right)
			{
				int guess = left + right >> 1;
				int cmp = comparer.Compare(table[guess].key, key);
				if (cmp == 0)
				{
					return guess;
				}
				if (cmp < 0)
				{
					left = guess + 1;
				}
				else
				{
					right = guess - 1;
				}
			}
			return ~left;
		}
	}
}
