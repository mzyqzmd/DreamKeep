using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>卡牌升级补丁基类：只描述"升级时对这张牌做什么"，不含任何执行逻辑；
    /// 与词条数据（AffixData）同款的子类选择家族，烘焙期由 CardModelBaker 解析成运行时补丁。</summary>
    [Serializable]
    public abstract class UpgradeData
    {
    }
}
