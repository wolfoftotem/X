﻿using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Model;

/// <summary>通用插件接口</summary>
/// <remarks>
/// 为了方便构建一个简单通用的插件系统，先规定如下：
/// 1，负责加载插件的宿主，在加载插件后会进行插件实例化，此时可在插件构造函数中做一些事情，但不应该开始业务处理，因为宿主的准备工作可能尚未完成
/// 2，宿主一切准备就绪后，会顺序调用插件的Init方法，并将宿主标识传入，插件通过标识区分是否自己的目标宿主。插件的Init应尽快完成。
/// 3，如果插件实现了<see cref="IDisposable"/>接口，宿主最后会清理资源。
/// </remarks>
public interface IPlugin
{
    /// <summary>初始化</summary>
    /// <param name="identity">插件宿主标识</param>
    /// <param name="provider">服务提供者</param>
    /// <returns>返回初始化是否成功。如果当前宿主不是所期待的宿主，这里返回false</returns>
    Boolean Init(String identity, IServiceProvider provider);
}

/// <summary>插件特性。用于判断某个插件实现类是否支持某个宿主</summary>
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    /// <summary>插件宿主标识</summary>
    public String Identity { get; set; }

    /// <summary>实例化</summary>
    /// <param name="identity"></param>
    public PluginAttribute(String identity) => Identity = identity;
}

/// <summary>插件管理器</summary>
public class PluginManager : DisposeBase, IServiceProvider
{
    #region 属性
    /// <summary>宿主标识，用于供插件区分不同宿主</summary>
    public String Identity { get; set; }

    /// <summary>宿主服务提供者</summary>
    public IServiceProvider Provider { get; set; }

    /// <summary>插件集合</summary>
    public IPlugin[] Plugins { get; set; }

    /// <summary>日志提供者</summary>
    public ILog Log { get; set; } = XTrace.Log;
    #endregion

    #region 构造
    /// <summary>实例化一个插件管理器</summary>
    public PluginManager() { }

    /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
    /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
    /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // 倒序销毁
            var ps = Plugins;
            if (ps != null)
            {
                for (var i = ps.Length - 1; i >= 0; i--)
                {
                    ps[i].TryDispose();
                }
                Plugins = null;
            }
        }
    }
    #endregion

    #region 方法
    /// <summary>加载插件。此时是加载所有插件，无法识别哪些是需要的</summary>
    public void Load()
    {
        var list = new List<IPlugin>();
        // 此时是加载所有插件，无法识别哪些是需要的
        foreach (var item in LoadPlugins())
        {
            if (item != null)
            {
                try
                {
                    // 插件类注册到容器中，方便后续获取
                    var container = Provider?.GetService<IObjectContainer>();
                    container?.TryAddSingleton(item, item);

                    var obj = Provider?.GetService(item) ?? item.CreateInstance();
                    if (obj is IPlugin plugin) list.Add(plugin);
                }
                catch (Exception ex)
                {
                    Log?.Debug(null, ex);
                }
            }
        }
        Plugins = list.ToArray();
    }

    IEnumerable<Type> LoadPlugins()
    {
        // 此时是加载所有插件，无法识别哪些是需要的
        foreach (var item in AssemblyX.FindAllPlugins(typeof(IPlugin), true))
        {
            if (item != null)
            {
                // 如果有插件特性，并且所有特性都不支持当前宿主，则跳过
                var atts = item.GetCustomAttributes<PluginAttribute>(true);
                if (atts != null && atts.Any(a => a.Identity != Identity)) continue;

                yield return item;
            }
        }
    }

    /// <summary>开始初始化。初始化之后，不属于当前宿主的插件将会被过滤掉</summary>
    public void Init()
    {
        var ps = Plugins;
        if (ps == null || ps.Length <= 0) return;

        var list = new List<IPlugin>();
        foreach (var item in ps)
        {
            try
            {
                if (item.Init(Identity, this)) list.Add(item);
            }
            catch (Exception ex)
            {
                Log?.Debug(null, ex);
            }
        }

        Plugins = list.ToArray();
    }
    #endregion

    #region IServiceProvider 成员
    Object IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(PluginManager)) return this;

        return Provider?.GetService(serviceType);
    }
    #endregion
}