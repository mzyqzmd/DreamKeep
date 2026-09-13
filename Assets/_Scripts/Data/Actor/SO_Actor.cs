using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>战斗单位配置基类：名称与生命上限，玩家与敌人共用。</summary>
    public abstract class SO_Actor : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private int _maxHP;

        public string Name => _name;
        public int MaxHp => _maxHP;
    }
}
