using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;

namespace NewLife.Http;

public class StreamContent : HttpContent
{
    #region 属性
    public Stream Data { get; set; }
    #endregion

    #region 构造
    public StreamContent(Stream stream) => Data = stream;
    #endregion

    #region 方法
    public override Task<Byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken) => Task.FromResult(Data.ReadBytes());
    #endregion
}
