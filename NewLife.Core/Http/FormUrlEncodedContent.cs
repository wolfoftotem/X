using System.Text;

namespace NewLife.Http;

public class FormUrlEncodedContent : ByteArrayContent
{
    public FormUrlEncodedContent(IEnumerable<KeyValuePair<String, String>> nameValueCollection)
        : base(GetContentByteArray(nameValueCollection)) => Headers.ContentType = "application/x-www-form-urlencoded";

    private static Byte[] GetContentByteArray(IEnumerable<KeyValuePair<String, String>> nameValueCollection)
    {
        //ArgumentNullException.ThrowIfNull(nameValueCollection, "nameValueCollection");
        var stringBuilder = new StringBuilder();
        foreach (var item in nameValueCollection)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append('&');
            }
            stringBuilder.Append(Encode(item.Key));
            stringBuilder.Append('=');
            stringBuilder.Append(Encode(item.Value));
        }
        return Encoding.Default.GetBytes(stringBuilder.ToString());
    }

    private static String Encode(String data) => String.IsNullOrEmpty(data) ? String.Empty : Uri.EscapeDataString(data).Replace("%20", "+");
}