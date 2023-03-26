using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using NewLife;
using System.Text;
using NewLife.Log;

namespace XAgent;

/// <summary>服务助手</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceHelper
{
    #region 服务安装和启动
    /// <summary>Exe程序名</summary>
    public static String ExeName
    {
        get
        {
            //String filename= AppDomain.CurrentDomain.FriendlyName.Replace(".vshost.", ".");
            //if (filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return filename;

            //filename = Assembly.GetExecutingAssembly().Location;
            //return filename;
            //String filename = Assembly.GetEntryAssembly().Location;
            var p = Process.GetCurrentProcess();
            var filename = p.MainModule.FileName;
            filename = Path.GetFileName(filename);
            filename = filename.Replace(".vshost.", ".");
            return filename;
        }
    }

    /// <summary>安装、卸载 服务</summary>
    /// <param name="service">服务对象</param>
    /// <param name="isinstall">是否安装</param>
    public static void Install(this IAgentService service, Boolean isinstall = true)
    {
        var name = service.ServiceName;
        if (String.IsNullOrEmpty(name)) throw new Exception("未指定服务名！");

        name = name.Replace(" ", "_");
        // win7及以上系统时才提示
        if (Environment.OSVersion.Version.Major >= 6) WriteLine("在win7/win2008及更高系统中，可能需要管理员权限执行才能安装/卸载服务。");
        if (isinstall)
        {
            RunSC("create " + name + " BinPath= \"" + ExeName.GetFullPath() + " -s\" start= auto DisplayName= \"" + service.DisplayName + "\"");
            if (!String.IsNullOrEmpty(service.Description)) RunSC("description " + name + " \"" + service.Description + "\"");
        }
        else
        {
            service.ControlService(false);

            RunSC("Delete " + name);
        }
    }

    /// <summary>启动、停止 服务</summary>
    /// <param name="service">服务对象</param>
    /// <param name="isstart"></param>
    public static void ControlService(this IAgentService service, Boolean isstart = true)
    {
        var name = service.ServiceName;
        if (String.IsNullOrEmpty(name)) throw new Exception("未指定服务名！");

        if (isstart)
            RunCmd("net start " + name, false, true);
        else
            RunCmd("net stop " + name, false, true);
    }

    /// <summary>重启服务</summary>
    /// <param name="service">服务</param>
    public static Boolean Restart(this IAgentService service)
    {
        var serviceName = service.ServiceName;
        XTrace.WriteLine("{0}.Restart {1}", service.GetType().Name, serviceName);

#if !NETSTANDARD
        if (!IsAdministrator()) return RunAsAdministrator("-restart");
#endif

        var cmd = $"/c net stop {serviceName} & ping 127.0.0.1 -n 5 & net start {serviceName}";
        Process.Start("cmd.exe", cmd);

        return true;
    }

    /// <summary>执行一个命令</summary>
    /// <param name="cmd"></param>
    /// <param name="showWindow"></param>
    /// <param name="waitForExit"></param>
    internal static void RunCmd(String cmd, Boolean showWindow, Boolean waitForExit)
    {
        WriteLine("RunCmd " + cmd);

        var p = new Process();
        var si = new ProcessStartInfo();
        var path = Environment.SystemDirectory;
        path = Path.Combine(path, @"cmd.exe");
        si.FileName = path;
        if (!cmd.StartsWith(@"/")) cmd = @"/c " + cmd;
        si.Arguments = cmd;
        si.UseShellExecute = false;
        si.CreateNoWindow = !showWindow;
        si.RedirectStandardOutput = true;
        si.RedirectStandardError = true;
        p.StartInfo = si;

        p.Start();
        if (waitForExit)
        {
            p.WaitForExit();

            var str = p.StandardOutput.ReadToEnd();
            if (!String.IsNullOrEmpty(str)) WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
            str = p.StandardError.ReadToEnd();
            if (!String.IsNullOrEmpty(str)) WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
        }
    }

    /// <summary>执行SC命令</summary>
    /// <param name="cmd"></param>
    internal static void RunSC(String cmd)
    {
        var path = Environment.SystemDirectory;
        path = Path.Combine(path, @"sc.exe");
        if (!File.Exists(path)) path = "sc.exe";
        if (!File.Exists(path)) return;
        RunCmd(path + " " + cmd, false, true);
    }
    #endregion

    #region 服务操作辅助函数
    /// <summary>是否已安装</summary>
    public static Boolean IsInstalled(this IAgentService service) => IsServiceInstalled(service.ServiceName);

    /// <summary>是否已启动</summary>
    public static Boolean IsRunning(this IAgentService service) => IsServiceRunning(service.ServiceName);

    /// <summary>取得服务</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static ServiceController GetService(String name)
    {
        var list = new List<ServiceController>(ServiceController.GetServices());
        if (list == null || list.Count < 1) return null;

        //return list.Find(delegate(ServiceController item) { return item.ServiceName == name; });
        foreach (var item in list)
        {
            if (item.ServiceName == name) return item;
        }
        return null;
    }

    /// <summary>是否已安装</summary>
    public static Boolean IsServiceInstalled(String name)
    {
        // 取的时候就抛异常，是不知道是否安装的
        using var control = GetService(name);
        if (control == null) return false;

        try
        {
            // 尝试访问一下才知道是否已安装
            var b = control.CanShutdown;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>是否已启动</summary>
    public static Boolean IsServiceRunning(String name)
    {
        try
        {
            using var control = GetService(name);
            if (control == null) return false;

            // 尝试访问一下才知道是否已安装
            var b = control.CanShutdown;

            control.Refresh();

            return control.Status == ServiceControllerStatus.Running;
        }
        catch
        {
            return false;
        }
    }

    static Boolean RunAsAdministrator(String argument)
    {
        var exe = ExecutablePath;
        if (exe.IsNullOrEmpty()) return false;

        var startInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = argument,
            Verb = "runas",
            UseShellExecute = true,
        };

        var p = Process.Start(startInfo);
        return !p.WaitForExit(5_000) || p.ExitCode == 0;
    }

    static String _executablePath;
    static String ExecutablePath
    {
        get
        {
            if (_executablePath == null)
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var codeBase = entryAssembly.CodeBase;
                    var uri = new Uri(codeBase);
                    _executablePath = uri.IsFile ? uri.LocalPath + Uri.UnescapeDataString(uri.Fragment) : uri.ToString();
                }
                else
                {
                    var moduleFileNameLongPath = GetModuleFileNameLongPath(new HandleRef(null, IntPtr.Zero));
                    _executablePath = moduleFileNameLongPath.ToString().GetFullPath();
                }
            }

            return _executablePath;
        }
    }

    static StringBuilder GetModuleFileNameLongPath(HandleRef hModule)
    {
        var sb = new StringBuilder(260);
        var num = 1;
        var num2 = 0;
        while ((num2 = GetModuleFileName(hModule, sb, sb.Capacity)) == sb.Capacity && Marshal.GetLastWin32Error() == 122 && sb.Capacity < 32767)
        {
            num += 2;
            var capacity = (num * 260 < 32767) ? (num * 260) : 32767;
            sb.EnsureCapacity(capacity);
        }
        sb.Length = num2;
        return sb;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern Int32 GetModuleFileName(HandleRef hModule, StringBuilder buffer, Int32 length);

    public static Boolean IsAdministrator()
    {
        var current = WindowsIdentity.GetCurrent();
        var windowsPrincipal = new WindowsPrincipal(current);
        return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    #endregion

    #region 日志
    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public static void WriteLine(String format, params Object[] args)
    {
        if (XTrace.Debug) XTrace.WriteLine(format, args);
    }

    /// <summary>写日志</summary>
    /// <param name="msg"></param>
    public static void WriteLine(String msg)
    {
        if (XTrace.Debug) XTrace.WriteLine(msg);
    }
    #endregion
}