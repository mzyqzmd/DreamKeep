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

#if UNITY_EDITOR
        // 资产卫生：抽取候选按引用去重，同引用重复项不会像旧合并池那样提升掉率，在这里显式提醒配置者
        private void OnValidate()
        {
            HashSet<SO_Relic> seen = new();
            for (int i = 0; i < _relics.Count; i++)
                if (_relics[i] != null && !seen.Add(_relics[i]))
                    Debug.LogWarning($"[{name}] 遗物 {_relics[i].name} 在第 {i} 项重复：候选按引用去重，重复项不会提升掉率", this);
        }
#endif
    }
}
