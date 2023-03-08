namespace System.Threading
{
	internal class AtomicBoolean
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
			int newTemp = (newVal ? 1 : 0);
			int expectedTemp = (expected ? 1 : 0);
			return Interlocked.CompareExchange(ref flag, newTemp, expectedTemp) == expectedTemp;
		}

		public static AtomicBoolean FromValue(bool value)
		{
			AtomicBoolean temp = new AtomicBoolean();
			temp.Value = value;
			return temp;
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
			int newTemp = (newVal ? 1 : 0);
			return Interlocked.Exchange(ref flag, newTemp) == 1;
		}

		public bool Equals(AtomicBoolean rhs)
		{
			return flag == rhs.flag;
		}

		public override bool Equals(object rhs)
		{
			if (!(rhs is AtomicBoolean))
			{
				return false;
			}
			return Equals((AtomicBoolean)rhs);
		}

		public override int GetHashCode()
		{
			return flag.GetHashCode();
		}

		public static explicit operator bool(AtomicBoolean rhs)
		{
			return rhs.Value;
		}

		public static implicit operator AtomicBoolean(bool rhs)
		{
			return FromValue(rhs);
		}
	}
}
