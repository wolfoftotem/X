using System.Diagnostics;

namespace System.Collections.Generic
{
	internal sealed class CollectionDebuggerView<T>
	{
		private readonly ICollection<T> c;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				T[] o = new T[c.Count];
				c.CopyTo(o, 0);
				return o;
			}
		}

		public CollectionDebuggerView(ICollection<T> col)
		{
			c = col;
		}
	}
	internal sealed class CollectionDebuggerView<T, U>
	{
		private readonly ICollection<KeyValuePair<T, U>> c;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public KeyValuePair<T, U>[] Items
		{
			get
			{
				KeyValuePair<T, U>[] o = new KeyValuePair<T, U>[c.Count];
				c.CopyTo(o, 0);
				return o;
			}
		}

		public CollectionDebuggerView(ICollection<KeyValuePair<T, U>> col)
		{
			c = col;
		}
	}
}
