using System.Net;

namespace NewLife.Http.Headers;

public class HttpResponseMessage : HttpResponse
{
    public HttpContent Content { get; set; }

    public String ReasonPhrase => StatusCode + "";

    public Boolean IsSuccessStatusCode => StatusCode is >= HttpStatusCode.OK and <= ((HttpStatusCode)299);

    public HttpResponseMessage EnsureSuccessStatusCode() => !IsSuccessStatusCode ? throw new HttpRequestException(StatusCode + "", null, StatusCode) : this;

    public void SetContent()
    {
        Content = new ByteArrayContent(Body);

        var headers = Content.Headers;
        headers.ContentType = ContentType;
        if (ContentLength > 0) headers.ContentLength = ContentLength;
    }
}