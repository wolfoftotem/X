using System.Diagnostics;

namespace System.Collections
{
	internal sealed class CollectionDebuggerView
	{
		private readonly ICollection c;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public object[] Items
		{
			get
			{
				object[] o = new object[c.Count];
				c.CopyTo(o, 0);
				return o;
			}
		}

		public CollectionDebuggerView(ICollection col)
		{
			c = col;
		}
	}
}
