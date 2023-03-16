namespace System.Threading;

internal struct AtomicBooleanValue
{
	private const int UnSet = 0;

	private const int Set = 1;

	private int flag;

	public bool Value
	{
		get
		{
			return flag == 1;
		}
		set
		{
			Exchange(value);
		}
	}

	public bool CompareAndExchange(bool expected, bool newVal)
	{
		int value = (newVal ? 1 : 0);
		int num = (expected ? 1 : 0);
		return Interlocked.CompareExchange(ref flag, value, num) == num;
	}

	public static AtomicBooleanValue FromValue(bool value)
	{
		AtomicBooleanValue result = default(AtomicBooleanValue);
		result.Value = value;
		return result;
	}

	public bool TrySet()
	{
		return !Exchange(newVal: true);
	}

	public bool TryRelaxedSet()
	{
		if (flag == 0)
		{
			return !Exchange(newVal: true);
		}
		return false;
	}

	public bool Exchange(bool newVal)
	{
		int value = (newVal ? 1 : 0);
		return Interlocked.Exchange(ref flag, value) == 1;
	}

	public bool Equals(AtomicBooleanValue rhs)
	{
		return flag == rhs.flag;
	}

	public override bool Equals(object rhs)
	{
		if (!(rhs is AtomicBooleanValue))
		{
			return false;
		}
		return Equals((AtomicBooleanValue)rhs);
	}

	public override int GetHashCode()
	{
		return flag.GetHashCode();
	}

	public static explicit operator bool(AtomicBooleanValue rhs)
	{
		return rhs.Value;
	}

	public static implicit operator AtomicBooleanValue(bool rhs)
	{
		return FromValue(rhs);
	}
}
