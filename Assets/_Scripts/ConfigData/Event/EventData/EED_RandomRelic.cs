using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>单件随机遗物：从遗物池过滤已持有后随机一件直接入包（单项目奖励不弹面板）；候选为空则空转。</summary>
    [Serializable]
    [AddTypeMenu("Event/随机遗物")]
    public sealed class EED_RandomRelic : EventEffectData
    {
        [SerializeField] private SO_RelicPool _pool; // 遗物来源池

        public SO_RelicPool Pool => _pool;
    }
}
