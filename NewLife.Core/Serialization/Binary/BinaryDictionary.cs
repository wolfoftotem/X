﻿using System.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>字典数据编码</summary>
public class BinaryDictionary : BinaryHandlerBase
{
    /// <summary>初始化</summary>
    public BinaryDictionary() => Priority = 30;

    /// <summary>写入一个对象</summary>
    /// <param name="value">目标对象</param>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public override Boolean Write(Object value, Type type)
    {
        if (value is not IDictionary dic) return false;

        // 先写入长度
        if (dic.Count == 0)
        {
            Host.WriteSize(0);
            return true;
        }

        Host.WriteSize(dic.Count);

        // 循环写入数据
        foreach (DictionaryEntry item in dic)
        {
            Host.Write(item.Key);
            Host.Write(item.Value);
        }

        return true;
    }

    /// <summary>尝试读取指定类型对象</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override Boolean TryRead(Type type, ref Object value)
    {
        if (!type.As<IDictionary>() && !type.As(typeof(IDictionary<,>))) return false;

        // 子元素类型
        var gs = type.GetGenericArguments();
        if (gs.Length != 2) throw new NotSupportedException($"字典类型仅支持 {typeof(Dictionary<,>).FullName}");

        var keyType = gs[0];
        var valType = gs[1];

        // 先读取长度
        var count = Host.ReadSize();
        if (count == 0) return true;

        // 创建字典
        if (value == null && type != null)
        {
            value = type.CreateInstance();
        }

        var dic = value as IDictionary;

        for (var i = 0; i < count; i++)
        {
            Object key = null;
            Object val = null;
            if (!Host.TryRead(keyType, ref key)) return false;
            if (!Host.TryRead(valType, ref val)) return false;

            dic[key] = val;
        }

        return true;
    }
}