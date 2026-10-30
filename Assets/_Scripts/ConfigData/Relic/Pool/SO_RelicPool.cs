using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>遗物奖励池：遗物奖励从这里抽，跳过玩家已持有的那几件。
    /// 可按稀有度分多份池，由遭遇奖励配置的池列表合并使用。</summary>
    [CreateAssetMenu(fileName = "RelicPool", menuName = "DreamKeep/Data/Pool/RelicPool")]
    public sealed class SO_RelicPool : ScriptableObject
    {
        [SerializeField] private List<SO_Relic> _relics = new();

        public IReadOnlyList<SO_Relic> Relics => _relics;
    }
}
