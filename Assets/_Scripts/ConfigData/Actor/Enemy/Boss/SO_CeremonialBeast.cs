using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData.Boss
{
    /// <summary>仪式兽配置：双阶段 Boss，阶段一沿用基类意图字段，横冲直撞被打断时整表切到阶段二意图。</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "DreamKeep/Data/Actor/Enemy/Boss/CeremonialBeast")]
    public class SO_CeremonialBeast : SO_Enemy
    {
        [Header("阶段二配置")] [SerializeReference, SubclassSelector]
        private List<IntentData> _phaseTwoIntents = new();

        [SerializeReference, SubclassSelector] private MixedIntentData _phaseTwoFrontSlotIntent;

        public List<IntentData> PhaseTwoIntents => _phaseTwoIntents;
        public MixedIntentData PhaseTwoFrontSlotIntent => _phaseTwoFrontSlotIntent;
    }
}
