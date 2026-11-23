using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>火焰药水：对一名敌人造成固定伤害，不吃力量修正（投掷物不随攻击力变化）。</summary>
    [CreateAssetMenu(fileName = "FirePotion", menuName = "DreamKeep/Data/Potion/火焰药水")]
    public sealed class SO_FirePotion : SO_Potion
    {
        [SerializeField] private int _amount = 20;

        public int Amount => _amount;
        public override EPotionTargetType TargetType => EPotionTargetType.AnyEnemy;
    }
}
