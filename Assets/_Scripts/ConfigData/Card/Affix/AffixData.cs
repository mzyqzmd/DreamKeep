using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>词条数据基类：只描述"做什么、做给谁、做多少"，不含任何执行逻辑。</summary>
    [Serializable]
    public abstract class AffixData
    {
        public abstract string Name { get; }           // 词条名称
        public virtual bool ShowDescriptionCardView => true;   // 是否在卡面上显示描述
    }
}
