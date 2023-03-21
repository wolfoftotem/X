namespace NewLife.Http;

public class MultipartFormDataContent : HttpContent
{
    public Dictionary<String, HttpContent> Contents { get; set; } = new();

    private String _boundary;
    private Dictionary<String, String> _fileNames = new();

    public MultipartFormDataContent()
    {
        _boundary = Guid.NewGuid().ToString();
        Headers.ContentType = $"multipart/form-data; boundary={_boundary}";
    }

    public void Add(HttpContent content, String name) => Contents.Add(name, content);

    public void Add(HttpContent content, String name, String fileName)
    {
        Add(content, name);

        if (!fileName.IsNullOrEmpty()) _fileNames[name] = fileName;
    }

    public override async Task<Byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);

        foreach (var item in Contents)
        {
            // 段落之间分隔
            writer.WriteLine("\r\n--" + _boundary);

            if (_fileNames.TryGetValue(item.Key, out var fileName))
                writer.WriteLine($"Content-Disposition: form-data; name=\"{item.Key}\" filename=\"{fileName}\"");
            else
                writer.WriteLine($"Content-Disposition: form-data; name=\"{item.Key}\"");

            writer.WriteLine();

            // 内容
            var buf = await item.Value.ReadAsByteArrayAsync(cancellationToken);
            if (buf != null) ms.Write(buf);
        }

        // 结尾
        writer.WriteLine();
        writer.WriteLine("--" + _boundary + "--");

        return ms.ToArray();
    }
}