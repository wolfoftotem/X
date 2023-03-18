using System.Net.Http;

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
        RequestUri = new Uri(requestUri);
    }
}