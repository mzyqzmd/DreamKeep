using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    [Serializable]
    public abstract class PerkData
    {
        [SerializeField] private Sprite _icon;
        public abstract string Name { get; }           // 效果名称
        public Sprite Icon => _icon;
    }
}
