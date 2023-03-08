using System.Collections.Generic;

namespace System.Threading.Tasks
{
	internal class CircularArray<T>
	{
		private readonly int baseSize;

		public readonly int size;

		public readonly T[] segment;

		public long Size => size;

		public T this[long index]
		{
			get
			{
				return segment[index % size];
			}
			set
			{
				segment[index % size] = value;
			}
		}

		public CircularArray(int baseSize)
		{
			this.baseSize = baseSize;
			size = 1 << baseSize;
			segment = new T[size];
		}

		public CircularArray<T> Grow(long bottom, long top)
		{
			CircularArray<T> grow = new CircularArray<T>(baseSize + 1);
			for (long i = top; i < bottom; i++)
			{
				grow.segment[i] = segment[i % size];
			}
			return grow;
		}

		public IEnumerable<T> GetEnumerable(long bottom, ref long top)
		{
			long instantTop = top;
			T[] slice = new T[bottom - instantTop];
			int destIndex = -1;
			for (long i = instantTop; i < bottom; i++)
			{
				slice[++destIndex] = segment[i % size];
			}
			return RealGetEnumerable(slice, bottom, top, instantTop);
		}

		private IEnumerable<T> RealGetEnumerable(T[] slice, long bottom, long realTop, long initialTop)
		{
			int destIndex = (int)(realTop - initialTop - 1);
			for (long i = realTop; i < bottom; i++)
			{
				int num;
				destIndex = (num = destIndex + 1);
				yield return slice[num];
			}
		}
	}
}
