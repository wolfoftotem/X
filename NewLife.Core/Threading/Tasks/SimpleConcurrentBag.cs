namespace System.Threading.Tasks;

internal class SimpleConcurrentBag<T>
{
	private readonly IConcurrentDeque<T>[] deques;

	private readonly bool unique;

	private int index = -1;

	[ThreadStatic]
	private int stealIndex;

	public SimpleConcurrentBag(int num)
	{
		deques = new CyclicDeque<T>[num];
		for (int i = 0; i < deques.Length; i++)
		{
			deques[i] = new CyclicDeque<T>();
		}
		unique = num <= 1;
	}

	public int GetNextIndex()
	{
		return Interlocked.Increment(ref index);
	}

	public bool TryTake(int index, out T value)
	{
		value = default(T);
		return deques[index].PopBottom(out value) == PopResult.Succeed;
	}

	public bool TrySteal(int index, out T value)
	{
		value = default(T);
		if (unique)
		{
			return false;
		}
		for (int i = 0; i < 3; i++)
		{
			if (stealIndex == index)
			{
				stealIndex = (stealIndex + 1) % deques.Length;
			}
			if (deques[stealIndex = (stealIndex + 1) % deques.Length].PopTop(out value) == PopResult.Succeed)
			{
				return true;
			}
		}
		return false;
	}

	public void Add(int index, T value)
	{
		deques[index].PushBottom(value);
	}
}
