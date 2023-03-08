using System.Diagnostics;
using System.Net;
using System.Reflection;
using NewLife.Log;

namespace NewLife.Web;

/// <summary>扩展的Web客户端</summary>
public class WebClientX : WebClient
{
    #region 静态
    static WebClientX()
    {
        // 设置默认最大连接为20，关闭默认代理，提高响应速度
        ServicePointManager.DefaultConnectionLimit = 20;
        WebRequest.DefaultWebProxy = null;

        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls;
        }
        catch { }
    }
    #endregion

    #region 为了Cookie而重写
    private CookieContainer _Cookie;
    /// <summary>Cookie容器</summary>
    public CookieContainer Cookie { get { return _Cookie ?? (_Cookie = new CookieContainer()); } set { _Cookie = value; } }

    #endregion

    #region 属性
    /// <summary>可接受类型</summary>
    public String Accept { get; set; }

    /// <summary>可接受语言</summary>
    public String AcceptLanguage { get; set; }

    /// <summary>引用页面</summary>
    public String Referer { get; set; }

    /// <summary>自动解压缩模式。</summary>
    public DecompressionMethods AutomaticDecompression { get; set; }

    /// <summary>User-Agent 标头，指定有关客户端代理的信息</summary>
    public String UserAgent { get; set; }

    /// <summary>超时，默认15000毫秒</summary>
    public Int32 Timeout { get; set; } = 15000;

    /// <summary>最后使用的连接名</summary>
    public Link LastLink { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public WebClientX() { }

    /// <summary>初始化常用的东西</summary>
    /// <param name="ie">是否模拟ie</param>
    /// <param name="iscompress">是否压缩</param>
    public WebClientX(Boolean ie, Boolean iscompress)
    {
        if (ie)
        {
            Accept = "text/html, */*";
            AcceptLanguage = "zh-CN";
            //Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
            var name = "";
            var asm = Assembly.GetEntryAssembly();
            if (asm != null) name = asm.GetName().Name;
            if (String.IsNullOrEmpty(name))
            {
                try
                {
                    name = Process.GetCurrentProcess().ProcessName;
                }
                catch { }
            }
            UserAgent = $"Mozilla/5.0 (compatible; MSIE 11.0; Windows NT 6.1; Trident/7.0; SLCC2; .NET CLR 2.0.50727; .NET4.0C; .NET4.0E; {name})";
        }
        if (iscompress) AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
    }
    #endregion

    #region 重载设置属性
    /// <summary>重写获取请求</summary>
    /// <param name="address"></param>
    /// <returns></returns>
    protected override WebRequest GetWebRequest(Uri address)
    {
        var request = base.GetWebRequest(address);

        var hr = request as HttpWebRequest;
        if (hr != null)
        {
            hr.CookieContainer = Cookie;
            hr.AutomaticDecompression = AutomaticDecompression;

            if (!String.IsNullOrEmpty(Accept)) hr.Accept = Accept;
            if (!String.IsNullOrEmpty(AcceptLanguage)) hr.Headers[HttpRequestHeader.AcceptLanguage] = AcceptLanguage;
            if (!String.IsNullOrEmpty(UserAgent)) hr.UserAgent = UserAgent;
            if (!String.IsNullOrEmpty(Accept)) hr.Accept = Accept;
        }

        if (Timeout > 0) request.Timeout = Timeout;

        return request;
    }

    /// <summary>重写获取响应</summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected override WebResponse GetWebResponse(WebRequest request)
    {
        var response = base.GetWebResponse(request);
        var http = response as HttpWebResponse;
        if (http != null)
        {
            Cookie.Add(http.Cookies);
            if (!String.IsNullOrEmpty(http.CharacterSet)) Encoding = System.Text.Encoding.GetEncoding(http.CharacterSet);
        }

        return response;
    }
    #endregion

    #region 方法
    /// <summary>获取指定地址的Html，自动处理文本编码</summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public String GetHtml(String url)
    {
        var buf = DownloadData(url);
        Referer = url;
        if (buf == null || buf.Length == 0) return null;

        // 处理编码
        var enc = Encoding;
        //if (ResponseHeaders[HttpResponseHeader.ContentType].Contains("utf-8")) enc = System.Text.Encoding.UTF8;

        return buf.ToStr(enc);
    }

    public String GetString(String url) => GetHtml(url);

    /// <summary>获取指定地址的Html，分析所有超链接</summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public Link[] GetLinks(String url)
    {
        var html = GetHtml(url);
        if (html.IsNullOrWhiteSpace()) return new Link[0];

        return Link.Parse(html, url);
    }

    /// <summary>分析指定页面指定名称的链接，并下载到目标目录，返回目标文件</summary>
    /// <remarks>
    /// 根据版本或时间降序排序选择
    /// </remarks>
    /// <param name="urls">指定页面</param>
    /// <param name="name">页面上指定名称的链接</param>
    /// <param name="destdir">要下载到的目标目录</param>
    /// <returns>返回已下载的文件，无效时返回空</returns>
    public String DownloadLink(String urls, String name, String destdir)
    {
        Log.Info("下载链接 {0}，目标 {1}", urls, name);

        var names = name.Split(",", ";");

        var file = "";
        Link link = null;
        Exception lastError = null;
        foreach (var url in urls.Split(",", ";"))
        {
            try
            {
                var ls = GetLinks(url);
                if (ls.Length == 0) return file;

                // 过滤名称后降序排序，多名称时，先确保前面的存在，即使后面名称也存在并且也时间更新都不能用
                //foreach (var item in names)
                //{
                //    link = ls.Where(e => !e.Url.IsNullOrWhiteSpace())
                //       .Where(e => e.Name.EqualIgnoreCase(item) || e.FullName.Equals(item))
                //       .OrderByDescending(e => e.Version)
                //       .ThenByDescending(e => e.Time)
                //       .FirstOrDefault();
                //    if (link != null) break;
                //}
                ls = ls.Where(e => e.Name.EqualIgnoreCase(names) || e.FullName.EqualIgnoreCase(names)).ToArray();
                link = ls.OrderByDescending(e => e.Version).ThenByDescending(e => e.Time).FirstOrDefault();
            }
            catch (WebException ex)
            {
                Log.Error(ex.Message);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
            if (link != null) break;
        }
        if (link == null)
        {
            if (lastError != null) throw lastError;

            return file;
        }

        LastLink = link;
        var linkName = link.FullName;
        var file2 = destdir.CombinePath(linkName).EnsureDirectory();

        // 已经提前检查过，这里几乎不可能有文件存在
        if (File.Exists(file2))
        {
            // 如果连接名所表示的文件存在，并且带有时间，那么就智能是它啦
            var p = linkName.LastIndexOf("_");
            if (p > 0 && (p + 8 + 1 == linkName.Length || p + 14 + 1 == linkName.Length))
            {
                Log.Info("分析得到文件 {0}，目标文件已存在，无需下载 {1}", linkName, link.Url);
                return file2;
            }
        }

        Log.Info("分析得到文件 {0}，准备下载 {1}，保存到 {2}", linkName, link.Url, file2);
        // 开始下载文件，注意要提前建立目录，否则会报错
        file2 = file2.EnsureDirectory();

        var sw = Stopwatch.StartNew();
        Task.Run(() => DownloadFileAsync(new Uri(link.Url), file2)).Wait();
        sw.Stop();

        if (File.Exists(file2))
        {
            Log.Info("下载完成，共{0:n0}字节，耗时{1:n0}毫秒", file2.AsFile().Length, sw.ElapsedMilliseconds);
            file = file2;
        }

        return file;
    }

    FileInfo CheckCache(String name, String dir)
    {
        var di = dir.AsDirectory();
        if (di != null && di.Exists)
        {
            var fi = di.GetFiles(name + ".*").FirstOrDefault();
            if (fi == null || !fi.Exists) fi = di.GetFiles(name + "_*.*").FirstOrDefault();
            if (fi != null && fi.Exists)
            {
                Log.Info("目标文件{0}已存在，更新于{1}", fi.FullName, fi.LastWriteTime);
                return fi;
            }
        }

        return null;
    }

    /// <summary>分析指定页面指定名称的链接，并下载到目标目录，解压Zip后返回目标文件</summary>
    /// <param name="urls">提供下载地址的多个目标页面</param>
    /// <param name="name">页面上指定名称的链接</param>
    /// <param name="destdir">要下载到的目标目录</param>
    /// <param name="overwrite">是否覆盖目标同名文件</param>
    /// <returns></returns>
    public String DownloadLinkAndExtract(String urls, String name, String destdir, Boolean overwrite = false)
    {
        var file = "";

        // 下载
        try
        {
            file = DownloadLink(urls, name, destdir);
        }
        catch (Exception ex)
        {
            Log.Error(ex?.GetTrue()?.ToString());

            // 这个时候出现异常，删除zip
            if (!file.IsNullOrEmpty() && File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        if (file.IsNullOrEmpty()) return null;

        // 解压缩
        try
        {
            var fi = CheckCache(name, destdir);

            Log.Info("解压缩到 {0}", destdir);
            file.AsFile().Extract(destdir, overwrite);

            // 删除zip
            File.Delete(file);

            return file;
        }
        catch (Exception ex)
        {
            Log.Error(ex?.GetTrue()?.ToString());
        }

        return null;
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;
    #endregion
}