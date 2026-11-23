using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>甲虫汁：对一名敌人施加若干层「缩小」；层数随条目配，状态定义资产不携带层数。</summary>
    [CreateAssetMenu(fileName = "BeetleJuice", menuName = "DreamKeep/Data/Potion/甲虫汁")]
    public sealed class SO_BeetleJuicePotion : SO_Potion
    {
        [SerializeField] private SO_Shrink _perk;
        [SerializeField] private int _layers = 4;

        public SO_Shrink Perk => _perk;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.AnyEnemy;
    }
}
