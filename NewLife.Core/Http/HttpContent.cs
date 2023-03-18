using NewLife.Http.Headers;

namespace NewLife.Http;

public abstract class HttpContent
{
    public HttpContentHeaders Headers { get; set; } = new();

    public virtual Task<String> ReadAsStringAsync() => ReadAsStringAsync(default);

    public virtual async Task<String> ReadAsStringAsync(CancellationToken cancellationToken) => (await ReadAsByteArrayAsync(cancellationToken)).ToStr();

    public virtual async Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken) => new MemoryStream(await ReadAsByteArrayAsync(cancellationToken));

    public virtual Task<Byte[]> ReadAsByteArrayAsync() => ReadAsByteArrayAsync(default);

    public abstract Task<Byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken);
}
