using System.Net.Http;
using NewLife.Data;

namespace NewLife.Http.Headers;

public class HttpRequestMessage : HttpRequest
{
    public HttpContent Content { get; set; }

    //public HttpRequestHeaders Headers { get; set; }

    public HttpRequestMessage() { }

    public HttpRequestMessage(HttpMethod method, Uri requestUri)
    {
        Method = method + "";
        RequestUri = requestUri;
    }

    public HttpRequestMessage(HttpMethod method, String requestUri)
    {
        Method = method + "";
        RequestUri = new Uri(requestUri, UriKind.RelativeOrAbsolute);
    }

    /// <summary>序列化请求前，把Content内容放到头部</summary>
    /// <returns></returns>
    public override Packet Build()
    {
        var content = Content;
        if (content != null)
        {
            if (Body == null)
            {
                if (content is ByteArrayContent btc)
                    Body = btc.Data;
                else
                    Body = content.ReadAsByteArrayAsync().Result;
            }
            if (ContentType.IsNullOrEmpty() && content.Headers != null && !content.Headers.ContentType.IsNullOrEmpty())
            {
                ContentType = content.Headers.ContentType;
            }
        }

        return base.Build();
    }
}