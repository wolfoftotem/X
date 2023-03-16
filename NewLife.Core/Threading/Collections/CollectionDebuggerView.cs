using System.Diagnostics;

namespace System.Collections;

internal sealed class CollectionDebuggerView
{
	private readonly ICollection c;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public object[] Items
	{
		get
		{
			object[] array = new object[c.Count];
			c.CopyTo(array, 0);
			return array;
		}
	}

	public CollectionDebuggerView(ICollection col)
	{
		c = col;
	}
}
