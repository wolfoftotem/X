using System.Collections.Generic;

namespace System.Collections
{
	public static class StructuralComparisons
	{
		private sealed class ComparerImpl : IComparer, IEqualityComparer
		{
			int IComparer.Compare(object x, object y)
			{
				return (x as IStructuralComparable)?.CompareTo(y, this) ?? Comparer.Default.Compare(x, y);
			}

			int IEqualityComparer.GetHashCode(object obj)
			{
				return (obj as IEqualityComparer)?.GetHashCode(this) ?? EqualityComparer<object>.Default.GetHashCode(obj);
			}

			bool IEqualityComparer.Equals(object x, object y)
			{
				return (x as IEqualityComparer)?.Equals(y, this) ?? EqualityComparer<object>.Default.Equals(x, y);
			}
		}

		private static readonly ComparerImpl comparer = new ComparerImpl();

		public static IComparer StructuralComparer => comparer;

		public static IEqualityComparer StructuralEqualityComparer => comparer;
	}
}
