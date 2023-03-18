using System;
using System.Threading.Tasks;

namespace Test;

internal class Assert
{
    public static void True(Boolean obj)
    {
        if (!obj) throw new ArgumentNullException(nameof(obj));
    }

    public static void False(Boolean obj)
    {
        if (obj) throw new ArgumentNullException(nameof(obj));
    }

    public static void NotNull(Object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
    }

    public static void Null(Object obj)
    {
        if (obj != null) throw new ArgumentNullException(nameof(obj));
    }

    public static void Equal(Int32 a, Int32 b)
    {
        if (!Object.Equals(a, b)) throw new ArgumentException(nameof(a), nameof(b));
    }

    public static void Equal(String a, String b)
    {
        if (!Object.Equals(a, b)) throw new ArgumentException(nameof(a), nameof(b));
    }

    public static void Equal(String a, Object b)
    {
        if (!Object.Equals(a, b)) throw new ArgumentException(nameof(a), nameof(b));
    }

    public static void EndsWith(String a, String b)
    {
        if (!b.StartsWith(a)) throw new ArgumentException(nameof(a), nameof(b));
    }

    public static async Task<T> ThrowsAsync<T>(Func<Task> func) where T : Exception
    {
        try
        {
            await func();
        }
        catch (T ex)
        {
            return ex;
        }

        throw new InvalidOperationException();
    }
}