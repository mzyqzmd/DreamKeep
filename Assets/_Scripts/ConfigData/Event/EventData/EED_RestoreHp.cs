using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>恢复生命：回复量钳到当前生命上限；满血时选项仍可选（白选，作保底出口）。</summary>
    [Serializable]
    [AddTypeMenu("Event/恢复生命")]
    public sealed class EED_RestoreHp : EventEffectData
    {
        [SerializeField] private int _amount; // 恢复的生命值

        public int Amount => _amount;
    }
}
