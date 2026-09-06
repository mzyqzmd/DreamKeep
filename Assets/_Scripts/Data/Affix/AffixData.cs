using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>词条数据基类：只描述"做什么、做给谁、做多少"，不含任何执行逻辑。</summary>
    [Serializable]
    public abstract class AffixData
    {
        [SerializeField] protected EAffixTarget _target;

        public abstract string Name { get; }           // 词条名称
        public abstract string Description { get; }    // 词条描述
        public virtual bool ShowDescription => true;   // 是否显示描述
        public EAffixTarget Target => _target;         // 词条作用目标
        public abstract EAffixKind Kind { get; }       // 词条类型
    }
}
