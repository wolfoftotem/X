using System.Collections;
using System.Collections.Generic;

namespace System;

public static class Tuple
{
	public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
	{
		return new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>(item1, item2, item3, item4, item5, item6, item7, new Tuple<T8>(item8));
	}

	public static Tuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
	{
		return new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
	}

	public static Tuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
	{
		return new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
	}

	public static Tuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
	{
		return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
	}

	public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
	{
		return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
	}

	public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
	{
		return new Tuple<T1, T2, T3>(item1, item2, item3);
	}

	public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
	{
		return new Tuple<T1, T2>(item1, item2);
	}

	public static Tuple<T1> Create<T1>(T1 item1)
	{
		return new Tuple<T1>(item1);
	}
}
[Serializable]
public class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> : IStructuralEquatable, IStructuralComparable, IComparable
{
	private T1 item1;

	private T2 item2;

	private T3 item3;

	private T4 item4;

	private T5 item5;

	private T6 item6;

	private T7 item7;

	private TRest rest;

	public T1 Item1 => item1;

	public T2 Item2 => item2;

	public T3 Item3 => item3;

	public T4 Item4 => item4;

	public T5 Item5 => item5;

	public T6 Item6 => item6;

	public T7 Item7 => item7;

	public TRest Rest => rest;

	public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
	{
		this.item1 = item1;
		this.item2 = item2;
		this.item3 = item3;
		this.item4 = item4;
		this.item5 = item5;
		this.item6 = item6;
		this.item7 = item7;
		this.rest = rest;
		bool flag = true;
		if (!typeof(TRest).IsGenericType)
		{
			flag = false;
		}
		if (flag)
		{
			Type genericTypeDefinition = typeof(TRest).GetGenericTypeDefinition();
			if (genericTypeDefinition != typeof(Tuple<>) && genericTypeDefinition != typeof(Tuple<, >) && genericTypeDefinition != typeof(Tuple<, , >) && genericTypeDefinition != typeof(Tuple<, , , >) && genericTypeDefinition != typeof(Tuple<, , , , >) && genericTypeDefinition != typeof(Tuple<, , , , , >) && genericTypeDefinition != typeof(Tuple<, , , , , , >) && genericTypeDefinition != typeof(Tuple<, , , , , , , >))
			{
				flag = false;
			}
		}
		if (!flag)
		{
			throw new ArgumentException("The last element of an eight element tuple must be a Tuple.");
		}
	}

	int IComparable.CompareTo(object obj)
	{
		return ((IStructuralComparable)this).CompareTo(obj, (IComparer)Comparer<object>.Default);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> tuple))
		{
			if (other == null)
			{
				return 1;
			}
			throw new ArgumentException();
		}
		int num = comparer.Compare(item1, tuple.item1);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item2, tuple.item2);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item3, tuple.item3);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item4, tuple.item4);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item5, tuple.item5);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item6, tuple.item6);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item7, tuple.item7);
		if (num != 0)
		{
			return num;
		}
		return comparer.Compare(rest, tuple.rest);
	}

	public override bool Equals(object obj)
	{
		return ((IStructuralEquatable)this).Equals(obj, (IEqualityComparer)EqualityComparer<object>.Default);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> tuple))
		{
			if (other == null)
			{
				return false;
			}
			throw new ArgumentException();
		}
		if (comparer.Equals(item1, tuple.item1) && comparer.Equals(item2, tuple.item2) && comparer.Equals(item3, tuple.item3) && comparer.Equals(item4, tuple.item4) && comparer.Equals(item5, tuple.item5) && comparer.Equals(item6, tuple.item6) && comparer.Equals(item7, tuple.item7))
		{
			return comparer.Equals(rest, tuple.rest);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((IStructuralEquatable)this).GetHashCode((IEqualityComparer)EqualityComparer<object>.Default);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		int hashCode = comparer.GetHashCode(item1);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item2);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item3);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item4);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item5);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item6);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item7);
		return (hashCode << 5) - hashCode + comparer.GetHashCode(rest);
	}

	public override string ToString()
	{
		return $"({item1}, {item2}, {item3}, {item4}, {item5}, {item6}, {item7}, {rest})";
	}
}
[Serializable]
public class Tuple<T1> : IStructuralEquatable, IStructuralComparable, IComparable
{
	private T1 item1;

	public T1 Item1 => item1;

	public Tuple(T1 item1)
	{
		this.item1 = item1;
	}

	int IComparable.CompareTo(object obj)
	{
		return ((IStructuralComparable)this).CompareTo(obj, (IComparer)Comparer<object>.Default);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (!(other is Tuple<T1> tuple))
		{
			if (other == null)
			{
				return 1;
			}
			throw new ArgumentException();
		}
		return comparer.Compare(item1, tuple.item1);
	}

	public override bool Equals(object obj)
	{
		return ((IStructuralEquatable)this).Equals(obj, (IEqualityComparer)EqualityComparer<object>.Default);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is Tuple<T1> tuple))
		{
			if (other == null)
			{
				return false;
			}
			throw new ArgumentException();
		}
		return comparer.Equals(item1, tuple.item1);
	}

	public override int GetHashCode()
	{
		return ((IStructuralEquatable)this).GetHashCode((IEqualityComparer)EqualityComparer<object>.Default);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return comparer.GetHashCode(item1);
	}

	public override string ToString()
	{
		return $"({item1})";
	}
}
[Serializable]
public class Tuple<T1, T2> : IStructuralEquatable, IStructuralComparable, IComparable
{
	private T1 item1;

	private T2 item2;

	public T1 Item1 => item1;

	public T2 Item2 => item2;

	public Tuple(T1 item1, T2 item2)
	{
		this.item1 = item1;
		this.item2 = item2;
	}

	int IComparable.CompareTo(object obj)
	{
		return ((IStructuralComparable)this).CompareTo(obj, (IComparer)Comparer<object>.Default);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (!(other is Tuple<T1, T2> tuple))
		{
			if (other == null)
			{
				return 1;
			}
			throw new ArgumentException();
		}
		int num = comparer.Compare(item1, tuple.item1);
		if (num != 0)
		{
			return num;
		}
		return comparer.Compare(item2, tuple.item2);
	}

	public override bool Equals(object obj)
	{
		return ((IStructuralEquatable)this).Equals(obj, (IEqualityComparer)EqualityComparer<object>.Default);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is Tuple<T1, T2> tuple))
		{
			if (other == null)
			{
				return false;
			}
			throw new ArgumentException();
		}
		if (comparer.Equals(item1, tuple.item1))
		{
			return comparer.Equals(item2, tuple.item2);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((IStructuralEquatable)this).GetHashCode((IEqualityComparer)EqualityComparer<object>.Default);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		int hashCode = comparer.GetHashCode(item1);
		return (hashCode << 5) - hashCode + comparer.GetHashCode(item2);
	}

	public override string ToString()
	{
		return $"({item1}, {item2})";
	}
}
[Serializable]
public class Tuple<T1, T2, T3> : IStructuralEquatable, IStructuralComparable, IComparable
{
	private T1 item1;

	private T2 item2;

	private T3 item3;

	public T1 Item1 => item1;

	public T2 Item2 => item2;

	public T3 Item3 => item3;

	public Tuple(T1 item1, T2 item2, T3 item3)
	{
		this.item1 = item1;
		this.item2 = item2;
		this.item3 = item3;
	}

	int IComparable.CompareTo(object obj)
	{
		return ((IStructuralComparable)this).CompareTo(obj, (IComparer)Comparer<object>.Default);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3> tuple))
		{
			if (other == null)
			{
				return 1;
			}
			throw new ArgumentException();
		}
		int num = comparer.Compare(item1, tuple.item1);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item2, tuple.item2);
		if (num != 0)
		{
			return num;
		}
		return comparer.Compare(item3, tuple.item3);
	}

	public override bool Equals(object obj)
	{
		return ((IStructuralEquatable)this).Equals(obj, (IEqualityComparer)EqualityComparer<object>.Default);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3> tuple))
		{
			if (other == null)
			{
				return false;
			}
			throw new ArgumentException();
		}
		if (comparer.Equals(item1, tuple.item1) && comparer.Equals(item2, tuple.item2))
		{
			return comparer.Equals(item3, tuple.item3);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((IStructuralEquatable)this).GetHashCode((IEqualityComparer)EqualityComparer<object>.Default);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		int hashCode = comparer.GetHashCode(item1);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item2);
		return (hashCode << 5) - hashCode + comparer.GetHashCode(item3);
	}

	public override string ToString()
	{
		return $"({item1}, {item2}, {item3})";
	}
}
[Serializable]
public class Tuple<T1, T2, T3, T4> : IStructuralEquatable, IStructuralComparable, IComparable
{
	private T1 item1;

	private T2 item2;

	private T3 item3;

	private T4 item4;

	public T1 Item1 => item1;

	public T2 Item2 => item2;

	public T3 Item3 => item3;

	public T4 Item4 => item4;

	public Tuple(T1 item1, T2 item2, T3 item3, T4 item4)
	{
		this.item1 = item1;
		this.item2 = item2;
		this.item3 = item3;
		this.item4 = item4;
	}

	int IComparable.CompareTo(object obj)
	{
		return ((IStructuralComparable)this).CompareTo(obj, (IComparer)Comparer<object>.Default);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4> tuple))
		{
			if (other == null)
			{
				return 1;
			}
			throw new ArgumentException();
		}
		int num = comparer.Compare(item1, tuple.item1);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item2, tuple.item2);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item3, tuple.item3);
		if (num != 0)
		{
			return num;
		}
		return comparer.Compare(item4, tuple.item4);
	}

	public override bool Equals(object obj)
	{
		return ((IStructuralEquatable)this).Equals(obj, (IEqualityComparer)EqualityComparer<object>.Default);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4> tuple))
		{
			if (other == null)
			{
				return false;
			}
			throw new ArgumentException();
		}
		if (comparer.Equals(item1, tuple.item1) && comparer.Equals(item2, tuple.item2) && comparer.Equals(item3, tuple.item3))
		{
			return comparer.Equals(item4, tuple.item4);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((IStructuralEquatable)this).GetHashCode((IEqualityComparer)EqualityComparer<object>.Default);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		int hashCode = comparer.GetHashCode(item1);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item2);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item3);
		return (hashCode << 5) - hashCode + comparer.GetHashCode(item4);
	}

	public override string ToString()
	{
		return $"({item1}, {item2}, {item3}, {item4})";
	}
}
[Serializable]
public class Tuple<T1, T2, T3, T4, T5> : IStructuralEquatable, IStructuralComparable, IComparable
{
	private T1 item1;

	private T2 item2;

	private T3 item3;

	private T4 item4;

	private T5 item5;

	public T1 Item1 => item1;

	public T2 Item2 => item2;

	public T3 Item3 => item3;

	public T4 Item4 => item4;

	public T5 Item5 => item5;

	public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
	{
		this.item1 = item1;
		this.item2 = item2;
		this.item3 = item3;
		this.item4 = item4;
		this.item5 = item5;
	}

	int IComparable.CompareTo(object obj)
	{
		return ((IStructuralComparable)this).CompareTo(obj, (IComparer)Comparer<object>.Default);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4, T5> tuple))
		{
			if (other == null)
			{
				return 1;
			}
			throw new ArgumentException();
		}
		int num = comparer.Compare(item1, tuple.item1);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item2, tuple.item2);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item3, tuple.item3);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item4, tuple.item4);
		if (num != 0)
		{
			return num;
		}
		return comparer.Compare(item5, tuple.item5);
	}

	public override bool Equals(object obj)
	{
		return ((IStructuralEquatable)this).Equals(obj, (IEqualityComparer)EqualityComparer<object>.Default);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4, T5> tuple))
		{
			if (other == null)
			{
				return false;
			}
			throw new ArgumentException();
		}
		if (comparer.Equals(item1, tuple.item1) && comparer.Equals(item2, tuple.item2) && comparer.Equals(item3, tuple.item3) && comparer.Equals(item4, tuple.item4))
		{
			return comparer.Equals(item5, tuple.item5);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((IStructuralEquatable)this).GetHashCode((IEqualityComparer)EqualityComparer<object>.Default);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		int hashCode = comparer.GetHashCode(item1);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item2);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item3);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item4);
		return (hashCode << 5) - hashCode + comparer.GetHashCode(item5);
	}

	public override string ToString()
	{
		return $"({item1}, {item2}, {item3}, {item4}, {item5})";
	}
}
[Serializable]
public class Tuple<T1, T2, T3, T4, T5, T6> : IStructuralEquatable, IStructuralComparable, IComparable
{
	private T1 item1;

	private T2 item2;

	private T3 item3;

	private T4 item4;

	private T5 item5;

	private T6 item6;

	public T1 Item1 => item1;

	public T2 Item2 => item2;

	public T3 Item3 => item3;

	public T4 Item4 => item4;

	public T5 Item5 => item5;

	public T6 Item6 => item6;

	public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
	{
		this.item1 = item1;
		this.item2 = item2;
		this.item3 = item3;
		this.item4 = item4;
		this.item5 = item5;
		this.item6 = item6;
	}

	int IComparable.CompareTo(object obj)
	{
		return ((IStructuralComparable)this).CompareTo(obj, (IComparer)Comparer<object>.Default);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4, T5, T6> tuple))
		{
			if (other == null)
			{
				return 1;
			}
			throw new ArgumentException();
		}
		int num = comparer.Compare(item1, tuple.item1);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item2, tuple.item2);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item3, tuple.item3);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item4, tuple.item4);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item5, tuple.item5);
		if (num != 0)
		{
			return num;
		}
		return comparer.Compare(item6, tuple.item6);
	}

	public override bool Equals(object obj)
	{
		return ((IStructuralEquatable)this).Equals(obj, (IEqualityComparer)EqualityComparer<object>.Default);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4, T5, T6> tuple))
		{
			if (other == null)
			{
				return false;
			}
			throw new ArgumentException();
		}
		if (comparer.Equals(item1, tuple.item1) && comparer.Equals(item2, tuple.item2) && comparer.Equals(item3, tuple.item3) && comparer.Equals(item4, tuple.item4) && comparer.Equals(item5, tuple.item5))
		{
			return comparer.Equals(item6, tuple.item6);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((IStructuralEquatable)this).GetHashCode((IEqualityComparer)EqualityComparer<object>.Default);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		int hashCode = comparer.GetHashCode(item1);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item2);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item3);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item4);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item5);
		return (hashCode << 5) - hashCode + comparer.GetHashCode(item6);
	}

	public override string ToString()
	{
		return $"({item1}, {item2}, {item3}, {item4}, {item5}, {item6})";
	}
}
[Serializable]
public class Tuple<T1, T2, T3, T4, T5, T6, T7> : IStructuralEquatable, IStructuralComparable, IComparable
{
	private T1 item1;

	private T2 item2;

	private T3 item3;

	private T4 item4;

	private T5 item5;

	private T6 item6;

	private T7 item7;

	public T1 Item1 => item1;

	public T2 Item2 => item2;

	public T3 Item3 => item3;

	public T4 Item4 => item4;

	public T5 Item5 => item5;

	public T6 Item6 => item6;

	public T7 Item7 => item7;

	public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
	{
		this.item1 = item1;
		this.item2 = item2;
		this.item3 = item3;
		this.item4 = item4;
		this.item5 = item5;
		this.item6 = item6;
		this.item7 = item7;
	}

	int IComparable.CompareTo(object obj)
	{
		return ((IStructuralComparable)this).CompareTo(obj, (IComparer)Comparer<object>.Default);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4, T5, T6, T7> tuple))
		{
			if (other == null)
			{
				return 1;
			}
			throw new ArgumentException();
		}
		int num = comparer.Compare(item1, tuple.item1);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item2, tuple.item2);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item3, tuple.item3);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item4, tuple.item4);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item5, tuple.item5);
		if (num != 0)
		{
			return num;
		}
		num = comparer.Compare(item6, tuple.item6);
		if (num != 0)
		{
			return num;
		}
		return comparer.Compare(item7, tuple.item7);
	}

	public override bool Equals(object obj)
	{
		return ((IStructuralEquatable)this).Equals(obj, (IEqualityComparer)EqualityComparer<object>.Default);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is Tuple<T1, T2, T3, T4, T5, T6, T7> tuple))
		{
			if (other == null)
			{
				return false;
			}
			throw new ArgumentException();
		}
		if (comparer.Equals(item1, tuple.item1) && comparer.Equals(item2, tuple.item2) && comparer.Equals(item3, tuple.item3) && comparer.Equals(item4, tuple.item4) && comparer.Equals(item5, tuple.item5) && comparer.Equals(item6, tuple.item6))
		{
			return comparer.Equals(item7, tuple.item7);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((IStructuralEquatable)this).GetHashCode((IEqualityComparer)EqualityComparer<object>.Default);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		int hashCode = comparer.GetHashCode(item1);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item2);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item3);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item4);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item5);
		hashCode = (hashCode << 5) - hashCode + comparer.GetHashCode(item6);
		return (hashCode << 5) - hashCode + comparer.GetHashCode(item7);
	}

	public override string ToString()
	{
		return $"({item1}, {item2}, {item3}, {item4}, {item5}, {item6}, {item7})";
	}
}
