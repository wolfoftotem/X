using System.Threading;

namespace System
{
	public static class ArrayExtensions
	{
		public static TTarget[] ConvertAll<TSrc, TTarget>(this TSrc[] src, Func<TSrc, TTarget> converter)
		{
			if (src == null || converter == null)
			{
				return null;
			}
			TTarget[] localArray = new TTarget[src.Length];
			for (int i = 0; i < localArray.Length; i++)
			{
				localArray[i] = converter(src[i]);
			}
			return localArray;
		}

		public static void CheckArray(this WaitHandle handle, WaitHandle[] handles, bool waitAll)
		{
			if (handles == null)
			{
				throw new ArgumentNullException("waitHandles");
			}
			int length = handles.Length;
			if (length > 64)
			{
				throw new NotSupportedException("Too many handles");
			}
			if (handles.Length == 0)
			{
				if (waitAll)
				{
					throw new ArgumentNullException("waitHandles");
				}
				throw new ArgumentException();
			}
			foreach (WaitHandle w in handles)
			{
				if (w == null)
				{
					throw new ArgumentNullException("waitHandles", "null handle");
				}
			}
		}
	}
}
