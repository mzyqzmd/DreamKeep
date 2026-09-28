using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>敌人配置：生命与名称来自单位配置基类，意图序列按顺序循环执行；0 号槽位替换意图可选。</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "DreamKeep/Data/Actor/Enemy")]
    public class SO_Enemy : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private int _maxHP;
        [SerializeReference, SubclassSelector] private List<IntentData> _enemyIntents = new();

        /// <summary>站 0 号槽位时强制替换当前意图的混合意图（攻击 + 前进），null 表示该敌人无此机制。</summary>
        [SerializeReference, SubclassSelector] private MixedIntentData _slotZeroIntent;

        public string Name => _name;
        public int MaxHp => _maxHP;
        public List<IntentData> EnemyIntents => _enemyIntents;
        public MixedIntentData SlotZeroIntent => _slotZeroIntent;
    }
}