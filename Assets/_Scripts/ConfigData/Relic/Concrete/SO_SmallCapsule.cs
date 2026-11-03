using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>小型扭蛋：拾起时从配置的若干遗物池合并取一件随机遗物，无持续效果。</summary>
    [CreateAssetMenu(fileName = "SmallCapsule", menuName = "DreamKeep/Data/Relic/小型扭蛋")]
    public sealed class SO_SmallCapsule : SO_Relic
    {
        [Tooltip("候选遗物池：合并后等权抽取，已持有的跳过")]
        [SerializeField]
        private List<SO_RelicPool> _pools = new();

        public IReadOnlyList<SO_RelicPool> Pools => _pools;
    }
}
