using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>敌人配置：视图预制体声明敌人身份（根上挂各自的具体视图类），意图序列按顺序循环执行，最前槽位替换意图可选，初始状态随配置自带。</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "DreamKeep/Data/Actor/Enemy/Enemy")]
    public class SO_Enemy : ScriptableObject
    {
        [Header("基础配置")] [SerializeField] private string _name;
        [SerializeField] private int _maxHP;
        [SerializeField] private GameObject _viewPrefab;
        [Tooltip("Spine 皮肤名：同种强度变体各自填 v1/v2/v3，留空则用默认皮肤")] [SerializeField]
        private string _skinName = "default";

        [Header("敌人配置")] [SerializeReference, SubclassSelector]
        private List<IntentData> _enemyIntents = new();

        [SerializeReference, SubclassSelector] private MixedIntentData _frontSlotIntent;

        [Header("初始状态")] [SerializeField] private List<PerkEntry> _startPerks = new();

        [Header("台词")] [Tooltip("留空则该敌人全程不说话；同种强度变体可共用一份")] [SerializeField]
        private SO_EnemySpeechLines _speechLines;

        public string Name => _name;
        public int MaxHp => _maxHP;
        public GameObject ViewPrefab => _viewPrefab;
        public string SkinName => _skinName;
        public List<IntentData> EnemyIntents => _enemyIntents;
        public MixedIntentData FrontSlotIntent => _frontSlotIntent;
        public List<PerkEntry> StartPerks => _startPerks;
        public SO_EnemySpeechLines SpeechLines => _speechLines;
    }
}