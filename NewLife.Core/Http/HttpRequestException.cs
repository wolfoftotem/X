using System.Net;

namespace NewLife.Http;

public class HttpRequestException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public HttpRequestException()
        : base(null, null)
    {
    }

    public HttpRequestException(String? message)
        : base(message, null)
    {
    }

    public HttpRequestException(String? message, Exception? inner, HttpStatusCode? statusCode)
        : base(message, inner) => StatusCode = statusCode;
}
