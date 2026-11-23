using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>超巨化药水：给自己施加超巨化状态，下一张攻击牌伤害翻倍。</summary>
    [CreateAssetMenu(fileName = "GigantificationPotion", menuName = "DreamKeep/Data/Potion/超巨化药水")]
    public sealed class SO_GigantificationPotion : SO_Potion
    {
        [SerializeField] private SO_Gigantification _gigantification;

        public SO_Gigantification Gigantification => _gigantification;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
