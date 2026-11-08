using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>意图配置基类：只描述"做什么、做多少"，不携带任何显示文案或台词；
    /// 显示说明由运行时意图提供，敌人台词由独立台词目录按执行事实查表。</summary>
    [Serializable]
    public abstract class IntentData
    {
    }
}
