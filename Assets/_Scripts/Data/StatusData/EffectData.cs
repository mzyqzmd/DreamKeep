using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    [Serializable]
    public abstract class EffectData
    {
        public abstract string Name { get; }           // 效果名称
        public abstract string Description { get; }    // 效果描述
    }
}
