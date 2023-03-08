using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NewLife.Common;
using NewLife.Compression;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Xml;

namespace Test
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            //XTrace.Log = new NetworkLog();
            XTrace.UseConsole();
#if DEBUG
            XTrace.Debug = true;
#endif
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                Test2();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void Test1()
        {
            //using (var zip = new ZipFile(@"..\System.Data.SQLite.zip".GetFullPath()))
            //{
            //    foreach (var item in zip.Entries)
            //    {
            //        Console.WriteLine("{0}\t{1}\t{2}", item.Key, item.Value.FileName, item.Value.UncompressedSize);
            //    }
            //    zip.Extract("SQLite".GetFullPath());
            //}

            //ZipFile.CompressDirectory("SQLite".GetFullPath());

            var buf = Certificate.CreateSelfSignCertificatePfx("CN=新生命团队;C=China;OU=NewLife;O=开发团队;E=nnhy@vip.qq.com");
            File.WriteAllBytes("stone.pfx", buf);
        }

        static async void Test2()
        {
            var client = new TinyHttpClient("http://star.newlifex.com:6600");

            var html = client.GetString("http://newlifex.com");
            XTrace.WriteLine(html);

            var rs = await client.GetAsync<Object>("api", new { state = 1234 });
            XTrace.WriteLine(rs.ToJson(true));

            var rs2 = await client.PostAsync<Object>("node/ping", new { state = 1234 });
            //var rs2 = await client.InvokeAsync<Object>("option", "api", new { state = 1234 });
            XTrace.WriteLine(rs2.ToJson(true));
        }
    }
}