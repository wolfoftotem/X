namespace System.Net.Http;

/// <summary>Http方法</summary>
public class HttpMethod : IEquatable<HttpMethod>
{
    private readonly Int32? _http3Index;

    private Int32 _hashcode;
    private static readonly HttpMethod s_putMethod = new("PUT", 21);
    private static readonly HttpMethod s_deleteMethod = new("DELETE", 16);
    private static readonly HttpMethod s_optionsMethod = new("OPTIONS", 19);
    private static readonly HttpMethod s_patchMethod = new("PATCH", -1);

    public static HttpMethod Get { get; } = new HttpMethod("GET", 17);

    public static HttpMethod Put => s_putMethod;

    public static HttpMethod Post { get; } = new HttpMethod("POST", 20);

    public static HttpMethod Delete => s_deleteMethod;

    public static HttpMethod Head { get; } = new HttpMethod("HEAD", 18);

    public static HttpMethod Options => s_optionsMethod;

    public static HttpMethod Trace { get; } = new HttpMethod("TRACE", -1);

    public static HttpMethod Patch => s_patchMethod;

    public static HttpMethod Connect { get; } = new HttpMethod("CONNECT", 15);

    public String Method { get; }

    internal Boolean MustHaveRequestBody => (Object)this != Get && (Object)this != Head && (Object)this != Connect && (Object)this != Options
&& (Object)this != Delete;

    public HttpMethod(String method)
    {
        if (String.IsNullOrEmpty(method)) throw new ArgumentException(nameof(method));

        Method = method;
    }

    private HttpMethod(String method, Int32 http3StaticTableIndex)
    {
        Method = method;
        _http3Index = http3StaticTableIndex;
    }

    public Boolean Equals(HttpMethod other)
    {
        if (other is null) return false;

        return (Object)Method == other!.Method || String.Equals(Method, other!.Method, StringComparison.OrdinalIgnoreCase);
    }

    public override Boolean Equals(Object obj) => Equals(obj as HttpMethod);

    public override Int32 GetHashCode()
    {
        if (_hashcode == 0)
        {
            _hashcode = StringComparer.OrdinalIgnoreCase.GetHashCode(Method);
        }
        return _hashcode;
    }

    public override String ToString() => Method;

    public static Boolean operator ==(HttpMethod left, HttpMethod right) => left is not null && right is not null ? left!.Equals(right) : (Object)left == right;

    public static Boolean operator !=(HttpMethod left, HttpMethod right) => !(left == right);
}