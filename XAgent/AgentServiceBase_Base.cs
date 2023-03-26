using System.ServiceProcess;
using NewLife.Log;

namespace XAgent;

/// <summary>服务程序基类</summary>
public abstract class AgentServiceBase : ServiceBase, IAgentService
{
    #region 属性
    /// <summary>显示名</summary>
    public virtual String DisplayName => ServiceName;

    /// <summary>描述</summary>
    public virtual String Description => ServiceName + "服务";

    ///// <summary>线程数</summary>
    //public virtual Int32 ThreadCount => 1;

    ///// <summary>线程名</summary>
    //public virtual String[] ThreadNames => null;
    #endregion

    #region 构造
    /// <summary>初始化</summary>
    public AgentServiceBase()
    {
        // 指定默认服务名
        if (String.IsNullOrEmpty(ServiceName)) ServiceName = GetType().Name;
    }
    #endregion

    #region 静态属性
    /// <summary></summary>
    internal protected static AgentServiceBase _Instance;
    /// <summary>服务实例。每个应用程序域只有一个服务实例</summary>
    public static AgentServiceBase Instance => _Instance;
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