using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace _DreamKeep.ConfigData
{
    /// <summary>敌人配置：生命与名称来自单位配置基类，意图序列按顺序循环执行；最前槽位替换意图可选。</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "DreamKeep/Data/Actor/Enemy")]
    public class SO_Enemy : ScriptableObject
    {
        [Header("基础配置")]
        [SerializeField] private string _name;
        [SerializeField] private int _maxHP;
        [SerializeField] private GameObject _viewPrefab;
        [Header("敌人配置")]
        [SerializeReference, SubclassSelector] private List<IntentData> _enemyIntents = new();
        [SerializeReference, SubclassSelector] private MixedIntentData _frontSlotIntent;

        public string Name => _name;
        public int MaxHp => _maxHP;
        public GameObject ViewPrefab => _viewPrefab;
        public List<IntentData> EnemyIntents => _enemyIntents;
        public MixedIntentData FrontSlotIntent => _frontSlotIntent;
    }
}