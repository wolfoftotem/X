namespace NewLife.Http.Headers;

/// <summary>Http请求头</summary>
public class HttpRequestHeaders
{
    /// <summary>内容类型</summary>
    public String ContentType { get; set; }

    public String UserAgent { get; set; }

    public String Accept { get; set; }
}
