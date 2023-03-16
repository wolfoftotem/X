using System.Collections.Generic;

namespace System.Collections;

public static class StructuralComparisons
{
	private sealed class ComparerImpl : IComparer, IEqualityComparer
	{
		int IComparer.Compare(object x, object y)
		{
			if (x is IStructuralComparable structuralComparable)
			{
				return structuralComparable.CompareTo(y, this);
			}
			return Comparer.Default.Compare(x, y);
		}

		int IEqualityComparer.GetHashCode(object obj)
		{
			if (obj is IEqualityComparer equalityComparer)
			{
				return equalityComparer.GetHashCode(this);
			}
			return EqualityComparer<object>.Default.GetHashCode(obj);
		}

		bool IEqualityComparer.Equals(object x, object y)
		{
			if (x is IEqualityComparer equalityComparer)
			{
				return equalityComparer.Equals(y, this);
			}
			return EqualityComparer<object>.Default.Equals(x, y);
		}
	}

	private static readonly ComparerImpl comparer = new ComparerImpl();

	public static IComparer StructuralComparer => comparer;

	public static IEqualityComparer StructuralEqualityComparer => comparer;
}
