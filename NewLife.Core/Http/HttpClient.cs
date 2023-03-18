using NewLife.Http;

namespace System.Net.Http;

/// <summary>Http客户端。实质是TinyHttpClient，仅用于兼容复用高版本代码</summary>
public class HttpClient : TinyHttpClient
{
}
