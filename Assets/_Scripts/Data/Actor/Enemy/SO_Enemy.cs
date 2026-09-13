using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>敌人配置：生命与名称来自单位配置基类，意图序列按顺序循环执行。</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "DreamKeep/Data/Actor/Enemy")]
    public class SO_Enemy : SO_Actor
    {
        [SerializeReference] private List<IntentData> _enemyIntents = new();

        public List<IntentData> EnemyIntents => _enemyIntents;
    }
}
