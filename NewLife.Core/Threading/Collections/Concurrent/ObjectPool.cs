using System.Threading;

namespace System.Collections.Concurrent;

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
		int num = 3;
		int num2;
		T result;
		do
		{
			num2 = removeIndex;
			if ((addIndex & -134217729) - 1 == num2 || num == 0)
			{
				return Creator();
			}
			result = buffer[num2 % 20];
		}
		while (Interlocked.CompareExchange(ref removeIndex, num2 + 1, num2) != num2 && --num > -1);
		return result;
	}

	public void Release(T obj)
	{
		if (obj == null || addIndex - removeIndex >= 19)
		{
			return;
		}
		int num = 3;
		int num2;
		while (true)
		{
			num2 = addIndex;
			if ((num2 & 0x8000000) <= 0)
			{
				if (num2 - removeIndex >= 19)
				{
					return;
				}
				if (Interlocked.CompareExchange(ref addIndex, num2 + 1 + 134217728, num2) == num2 || --num <= 0)
				{
					break;
				}
			}
		}
		buffer[num2 % 20] = obj;
		addIndex -= 134217728;
	}
}
