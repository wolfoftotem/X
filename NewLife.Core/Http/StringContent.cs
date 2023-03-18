using System.Text;

namespace NewLife.Http;

public class StringContent : ByteArrayContent
{
    public StringContent(String content) : base(content.GetBytes()) { }

    public StringContent(String content, Encoding encoding, String contentType) : base(content.GetBytes(encoding)) => Headers.ContentType = contentType;
}
