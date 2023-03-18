using NewLife.Data;

namespace NewLife.Http;

public class ByteArrayContent : HttpContent
{
    #region 属性
    public Packet Data { get; set; }
    #endregion

    #region 构造
    public ByteArrayContent(Packet packet) => Data = packet;

    public ByteArrayContent(Byte[] data) => Data = data;

    public ByteArrayContent(Byte[] data, Int32 offset, Int32 length) => Data = new Packet(data, offset, length);
    #endregion

    #region 方法
    public override Task<Byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken) => Task.FromResult(Data.ReadBytes());
    #endregion
}