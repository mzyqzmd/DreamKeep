using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>镣铐药水：给全体敌人施加负的镣铐影子状态，同步扣除等量真实力量。</summary>
    [CreateAssetMenu(fileName = "ShacklingPotion", menuName = "DreamKeep/Data/Potion/镣铐药水")]
    public sealed class SO_ShacklingPotion : SO_Potion
    {
        [SerializeField] private SO_Shackling _shackling;
        [SerializeField] private int _amount = 7;

        public SO_Shackling Shackling => _shackling;
        public int Amount => _amount;
        public override EPotionTargetType TargetType => EPotionTargetType.AllEnemy;
    }
}
