using System.Collections.Generic;

namespace System.Threading.Tasks;

public interface IConcurrentDeque<T>
{
	void PushBottom(T obj);

	PopResult PopBottom(out T obj);

	PopResult PopTop(out T obj);

	IEnumerable<T> GetEnumerable();
}
