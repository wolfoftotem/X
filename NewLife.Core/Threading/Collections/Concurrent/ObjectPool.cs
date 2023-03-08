using System.Threading;

namespace System.Collections.Concurrent
{
	internal abstract class ObjectPool<T> where T : class
	{
		private const int capacity = 20;

		private const int bit = 134217728;

		private readonly T[] buffer;

		private int addIndex;

		private int removeIndex;

		public ObjectPool()
		{
			buffer = new T[20];
			for (int i = 0; i < 20; i++)
			{
				buffer[i] = Creator();
			}
			addIndex = 19;
		}

		protected abstract T Creator();

		public T Take()
		{
			if ((addIndex & -134217729) - 1 == removeIndex)
			{
				return Creator();
			}
			int tries = 3;
			int i;
			T result;
			do
			{
				i = removeIndex;
				if ((addIndex & -134217729) - 1 == i || tries == 0)
				{
					return Creator();
				}
				result = buffer[i % 20];
			}
			while (Interlocked.CompareExchange(ref removeIndex, i + 1, i) != i && --tries > -1);
			return result;
		}

		public void Release(T obj)
		{
			if (obj == null || addIndex - removeIndex >= 19)
			{
				return;
			}
			int tries = 3;
			int i;
			while (true)
			{
				i = addIndex;
				if ((i & 0x8000000) <= 0)
				{
					if (i - removeIndex >= 19)
					{
						return;
					}
					if (Interlocked.CompareExchange(ref addIndex, i + 1 + 134217728, i) == i || --tries <= 0)
					{
						break;
					}
				}
			}
			buffer[i % 20] = obj;
			addIndex -= 134217728;
		}
	}
}
