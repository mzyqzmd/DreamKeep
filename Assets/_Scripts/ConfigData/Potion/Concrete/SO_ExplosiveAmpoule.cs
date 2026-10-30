using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>爆炸安瓿：对全体敌人造成伤害（投掷伤害不吃力量与易伤）。</summary>
    [CreateAssetMenu(fileName = "ExplosiveAmpoule", menuName = "DreamKeep/Data/Potion/爆炸安瓿")]
    public sealed class SO_ExplosiveAmpoule : SO_Potion
    {
        [SerializeField] private int _damage = 10;

        public int Damage => _damage;
        public override EPotionTargetType TargetType => EPotionTargetType.AllEnemy;
    }
}
