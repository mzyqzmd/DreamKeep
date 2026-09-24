using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>敌人配置：生命与名称来自单位配置基类，意图序列按顺序循环执行。</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "DreamKeep/Data/Actor/Enemy")]
    public class SO_Enemy : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private int _maxHP;
        [SerializeReference, SubclassSelector] private List<IntentData> _enemyIntents = new();

        public string Name => _name;
        public int MaxHp => _maxHP;
        public List<IntentData> EnemyIntents => _enemyIntents;
    }
}