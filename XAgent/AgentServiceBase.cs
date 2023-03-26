using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;

namespace XAgent;

/// <summary>服务程序基类</summary>
public abstract class AgentServiceBase : ServiceBase, IAgentService
{
    #region 属性
    /// <summary>显示名</summary>
    public virtual String DisplayName => ServiceName;

    /// <summary>描述</summary>
    public virtual String Description => ServiceName + "服务";
    #endregion

    #region 构造
    /// <summary>初始化</summary>
    public AgentServiceBase()
    {
        // 指定默认服务名
        if (String.IsNullOrEmpty(ServiceName)) ServiceName = GetType().Name;
    }
    #endregion

    #region 主函数
    /// <summary>服务主函数</summary>
    /// <param name="args"></param>
    public void Main(String[] args)
    {
        args ??= Environment.GetCommandLineArgs();

        Init();

        var cmd = args?.FirstOrDefault(e => !e.IsNullOrEmpty() && e.Length > 1 && e[0] == '-');
        if (!cmd.IsNullOrEmpty())
        {
            try
            {
                ProcessCommand(cmd, args);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
        else
        {
            if (!DisplayName.IsNullOrEmpty()) Console.Title = DisplayName;

            // 输出状态，菜单循环
            ShowStatus();
            ProcessMenu();
        }

        // 释放文本文件日志对象，确保日志队列内容写入磁盘
        if (XTrace.Log is CompositeLog compositeLog)
        {
            var log = compositeLog.Get<TextFileLog>();
            log.TryDispose();
        }
    }

    /// <summary>
    /// 初始化服务
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    protected virtual void Init()
    {
        Log = XTrace.Log;

        // 初始化配置
        var set = Setting.Current;
        if (set.ServiceName.IsNullOrEmpty()) set.ServiceName = ServiceName;
        if (set.DisplayName.IsNullOrEmpty()) set.DisplayName = DisplayName;
        if (set.Description.IsNullOrEmpty()) set.Description = Description;

        // 从程序集构造配置
        var asm = AssemblyX.Entry;
        if (set.ServiceName.IsNullOrEmpty()) set.ServiceName = asm.Name;
        if (set.DisplayName.IsNullOrEmpty()) set.DisplayName = asm.Title;
        if (set.Description.IsNullOrEmpty()) set.Description = asm.Description;

        // 用配置覆盖
        ServiceName = set.ServiceName;

        set.Save();
    }

    /// <summary>显示状态</summary>
    protected virtual void ShowStatus()
    {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        var name = ServiceName;
        if (name != DisplayName)
            Console.WriteLine("服务：{0}({1})", DisplayName, name);
        else
            Console.WriteLine("服务：{0}", name);
        Console.WriteLine("描述：{0}", Description);
        Console.Write("状态：");

        if (!this.IsInstalled())
            Console.WriteLine("未安装");
        else
        {
            if (!this.IsRunning())
                Console.WriteLine("未启动");
            else
                Console.WriteLine("运行中");
        }

        var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
        Console.WriteLine();
        Console.WriteLine("{0}\t版本：{1}\t发布：{2:yyyy-MM-dd HH:mm:ss}", asm.Name, asm.FileVersion, asm.Compile);

        var asm2 = AssemblyX.Create(Assembly.GetEntryAssembly());
        if (asm2 != asm)
            Console.WriteLine("{0}\t版本：{1}\t发布：{2:yyyy-MM-dd HH:mm:ss}", asm2.Name, asm2.FileVersion, asm2.Compile);

        Console.ForegroundColor = color;
    }

    /// <summary>显示菜单</summary>
    protected virtual void ShowMenu()
    {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine();
        Console.WriteLine("1 显示状态");

        var run = false;
        if (this.IsInstalled())
        {
            if (this.IsRunning())
            {
                run = true;
                Console.WriteLine("3 停止服务 -stop");
                Console.WriteLine("4 重启服务 -restart");
            }
            else
            {
                Console.WriteLine("2 卸载服务 -u");
                Console.WriteLine("3 启动服务 -start");
            }
        }
        else
        {
            Console.WriteLine("2 安装服务 -i");
        }

        if (!run)
        {
            Console.WriteLine("5 模拟运行 -run");
        }

        var dogs = Setting.Current.WatchDog.Split(",");
        if (dogs != null && dogs.Length > 0)
        {
            Console.WriteLine("7 看门狗保护服务 {0}", String.Join(",", dogs));
        }

        if (_Menus.Count > 0)
        {
            OnShowMenu(_Menus);
        }

        Console.WriteLine("0 退出");

        Console.ForegroundColor = color;
    }

    /// <summary>
    /// 显示自定义菜单
    /// </summary>
    /// <param name="menus"></param>
    protected virtual void OnShowMenu(IList<Menu> menus)
    {
        foreach (var item in menus)
        {
            Console.WriteLine("{0} {1}", item.Key, item.Name);
        }
    }

    private readonly List<Menu> _Menus = new();
    /// <summary>添加菜单</summary>
    /// <param name="key"></param>
    /// <param name="name"></param>
    /// <param name="callbak"></param>
    public void AddMenu(Char key, String name, Action callbak)
    {
        _Menus.RemoveAll(e => e.Key == key);
        _Menus.Add(new Menu(key, name, callbak));
    }

    /// <summary>菜单项</summary>
    public class Menu
    {
        /// <summary>按键</summary>
        public Char Key { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>回调方法</summary>
        public Action Callback { get; set; }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        public Menu(Char key, String name, Action callback)
        {
            Key = key;
            Name = name;
            Callback = callback;
        }
    }

    /// <summary>处理菜单</summary>
    protected virtual void ProcessMenu()
    {
        var service = this;
        var name = ServiceName;
        while (true)
        {
            //输出菜单
            ShowMenu();
            Console.Write("请选择操作（-x是命令行参数）：");

            //读取命令
            var key = Console.ReadKey();
            if (key.KeyChar == '0') break;
            Console.WriteLine();
            Console.WriteLine();

            try
            {
                switch (key.KeyChar)
                {
                    case '1':
                        //输出状态
                        ShowStatus();

                        break;
                    case '2':
                        if (service.IsInstalled())
                            service.Install(false);
                        else
                            service.Install(true);
                        break;
                    case '3':
                        if (service.IsRunning())
                            service.ControlService(false);
                        else
                            service.ControlService(true);
                        // 稍微等一下状态刷新
                        Thread.Sleep(500);
                        break;
                    case '4':
                        if (service.IsRunning())
                            service.Restart();
                        // 稍微等一下状态刷新
                        Thread.Sleep(500);
                        break;
                    case '5':
                        #region 模拟运行
                        try
                        {
                            Console.WriteLine("正在模拟运行……");
                            StartWork("模拟运行开始");

                            Console.WriteLine("任意键结束模拟运行！");
                            Console.ReadKey(true);

                            StopWork("模拟运行停止");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                        #endregion
                        break;
                    case '7':
                        CheckWatchDog();
                        break;
                    default:
                        // 自定义菜单
                        var menu = _Menus.FirstOrDefault(e => e.Key == key.KeyChar);
                        menu?.Callback();
                        break;
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
    }

    /// <summary>处理命令</summary>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    protected virtual void ProcessCommand(String cmd, String[] args)
    {
        var name = ServiceName;
        WriteLog("ProcessCommand cmd={0} args={1}", cmd, args.Join(" "));

        var service = this;
        cmd = cmd.ToLower();
        switch (cmd)
        {
            case "-s":
                try
                {
                    ServiceBase.Run(new ServiceBase[] { service });
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
                break;
            case "-i":
                service.Install(true);
                break;
            case "-u":
                service.Install(false);
                break;
            case "-start":
                service.ControlService(true);
                break;
            case "-stop":
                service.ControlService(false);
                break;
            case "-restart":
                service.Restart();
                break;
            case "-install":
                // 可能服务已存在，安装时报错，但不要影响服务启动
                try
                {
                    service.Install(true);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
                // 稍微等待
                for (var i = 0; i < 50; i++)
                {
                    service.Install(true);

                    if (service.IsInstalled()) break;

                    Thread.Sleep(100);
                }
                service.ControlService(true);
                break;
            case "-uninstall":
                try
                {
                    service.ControlService(false);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
                service.Install(false);
                break;
            case "-reinstall":
                try
                {
                    service.ControlService(false);
                    service.Install(false);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }

                // 稍微等待
                for (var i = 0; i < 50; i++)
                {
                    service.Install(true);

                    if (service.IsInstalled()) break;

                    Thread.Sleep(100);
                }
                service.ControlService(true);
                break;
            case "-run":
                if ("-delay".EqualIgnoreCase(args)) Thread.Sleep(5_000);
                StartWork("直接运行");
                Console.ReadKey(true);
                break;
            default:
                // 快速调用自定义菜单
                if (cmd.Length == 2 && cmd[0] == '-')
                {
                    var menu = _Menus.FirstOrDefault(e => e.Key == cmd[1]);
                    menu?.Callback();
                }
                break;
        }

        WriteLog("ProcessFinished cmd={0}", cmd);
    }
    #endregion

    #region 服务控制
    /// <summary>服务启动事件</summary>
    /// <param name="args"></param>
    protected override void OnStart(String[] args) => StartWork("服务运行");

    /// <summary>服务停止事件</summary>
    protected override void OnStop() => StopWork("服务停止");

    /// <summary>开始循环工作</summary>
    public virtual void StartWork(String reason)
    {
        WriteLog("服务启动 {0}", reason);

        // 启动服务管理
        _timer = new TimerX(DoCheck, null, 10_000, 10_000) { Async = true };
    }

    /// <summary>停止循环工作</summary>
    public virtual void StopWork(String reason)
    {
        WriteLog("服务停止 {0}", reason);

        // 停止服务管理线程
        _timer.TryDispose();
        _timer = null;
    }
    #endregion

    #region 服务维护线程
    private TimerX _timer;

    /// <summary>服务管理线程封装</summary>
    /// <param name="data"></param>
    protected virtual void DoCheck(Object data)
    {
        //如果某一项检查需要重启服务，则返回true，这里跳出循环，等待服务重启
        if (CheckMemory()) return;
        if (CheckThread()) return;
        if (CheckHandle()) return;
        if (CheckAutoRestart()) return;

        // 检查看门狗
        CheckWatchDog();
    }

    /// <summary>检查内存是否超标</summary>
    /// <returns>是否超标重启</returns>
    protected virtual Boolean CheckMemory()
    {
        var max = Setting.Current.MaxMemory;
        if (max <= 0) return false;

        var p = Process.GetCurrentProcess();
        var cur = p.WorkingSet64 + p.PrivateMemorySize64;
        cur = cur / 1024 / 1024;
        if (cur <= max) return false;

        WriteLog("当前进程占用内存 {0:n0}M，超过阀值 {1:n0}M，准备重新启动！", cur, max);

        this.Restart();

        return true;
    }

    /// <summary>检查服务进程的总线程数是否超标</summary>
    /// <returns></returns>
    protected virtual Boolean CheckThread()
    {
        var max = Setting.Current.MaxThread;
        if (max <= 0) return false;

        var p = Process.GetCurrentProcess();
        if (p.Threads.Count <= max) return false;

        WriteLog("当前进程总线程 {0:n0}个，超过阀值 {1:n0}个，准备重新启动！", p.Threads.Count, max);

        this.Restart();

        return true;
    }

    /// <summary>检查服务进程的句柄数是否超标</summary>
    /// <returns></returns>
    protected virtual Boolean CheckHandle()
    {
        var max = Setting.Current.MaxHandle;
        if (max <= 0) return false;

        var p = Process.GetCurrentProcess();
        if (p.HandleCount < max) return false;

        WriteLog("当前进程句柄 {0:n0}个，超过阀值 {1:n0}个，准备重新启动！", p.HandleCount, max);

        this.Restart();

        return true;
    }

    /// <summary>服务开始时间</summary>
    private readonly DateTime Start = DateTime.Now;

    /// <summary>检查自动重启</summary>
    /// <returns></returns>
    protected virtual Boolean CheckAutoRestart()
    {
        var auto = Setting.Current.AutoRestart;
        if (auto <= 0) return false;

        var ts = DateTime.Now - Start;
        if (ts.TotalMinutes <= auto) return false;

        WriteLog("服务已运行 {0:n0}分钟，达到预设重启时间（{1:n0}分钟），准备重启！", ts.TotalMinutes, auto);

        this.Restart();

        return true;
    }
    #endregion

    #region 看门狗
    /// <summary>检查看门狗。</summary>
    /// <remarks>
    /// XAgent看门狗功能由管理线程完成，每分钟一次。
    /// 检查指定的任务是否已经停止，如果已经停止，则启动它。
    /// </remarks>
    protected virtual void CheckWatchDog()
    {
        var ss = Setting.Current.WatchDog.Split(",");
        if (ss == null || ss.Length < 1) return;

        foreach (var item in ss)
        {
            // 注意：IsServiceRunning返回三种状态，null表示未知
            if (ServiceHelper.IsServiceRunning(item) == false)
            {
                WriteLog("发现服务{0}被关闭，准备启动！", item);

                ServiceHelper.RunCmd("net start " + item, false, true);
            }
        }
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; }

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}