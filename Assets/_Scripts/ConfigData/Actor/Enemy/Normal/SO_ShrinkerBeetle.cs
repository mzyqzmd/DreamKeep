using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>缩小甲虫配置：阶段一施加大量「缩小」，该意图执行一次后自动整表切到阶段二意图。</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "DreamKeep/Data/Actor/Enemy/Normal/ShrinkerBeetle")]
    public class SO_ShrinkerBeetle : SO_Enemy
    {
        [Header("阶段二配置")] [SerializeField] private bool _switchPhaseAfterFirstIntent = true;

        [SerializeReference, SubclassSelector]
        private List<IntentData> _phaseTwoIntents = new();

        public bool SwitchPhaseAfterFirstIntent => _switchPhaseAfterFirstIntent;
        public List<IntentData> PhaseTwoIntents => _phaseTwoIntents;
    }
}
