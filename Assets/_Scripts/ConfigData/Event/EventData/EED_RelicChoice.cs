using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>遗物 N 选一（E08 二段式）：从遗物池过滤已持有抽 N 件，把选项组刷新为「选择这个」子选项（副标题=遗物名:遗物效果），
    /// 点选一件直接入包；候选不足按实数、为空则空转。</summary>
    [Serializable]
    [AddTypeMenu("Event/遗物N选一")]
    public sealed class EED_RelicChoice : EventEffectData
    {
        [SerializeField] private SO_RelicPool _pool; // 遗物来源池
        [SerializeField] private int _count; // 候选件数（2 选 1 / 3 选 1）
        [SerializeField] private string _pickTitle = "选择这个"; // 子选项标题

        public SO_RelicPool Pool => _pool;
        public int Count => _count;
        public string PickTitle => _pickTitle;
    }
}
