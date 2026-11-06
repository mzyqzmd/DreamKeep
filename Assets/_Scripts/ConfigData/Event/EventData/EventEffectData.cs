using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>事件效果抽象基类：只描述"做什么"（数据住子类字段），执行由 GameRun/Event 的域循环按具体类型分派——
    /// 配置层不认识运行时，与 AD_*/UD_* 词条家族同一套写法。</summary>
    [Serializable]
    public abstract class EventEffectData { }
}
