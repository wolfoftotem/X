namespace NewLife.Http;

public abstract class HttpContent
{
    public HttpContentHeaders Headers { get; set; }

    public virtual Task<String> ReadAsStringAsync() => ReadAsStringAsync(default);

    public virtual async Task<String> ReadAsStringAsync(CancellationToken cancellationToken) => (await ReadAsByteArrayAsync(cancellationToken)).ToStr();

    public virtual Task<Byte[]> ReadAsByteArrayAsync() => ReadAsByteArrayAsync(default);

    public abstract Task<Byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken);
}
