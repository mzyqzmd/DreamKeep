using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>易伤药水：对单体敌人施加易伤。</summary>
    [CreateAssetMenu(fileName = "VulnerablePotion", menuName = "DreamKeep/Data/Potion/易伤药水")]
    public sealed class SO_VulnerablePotion : SO_Potion
    {
        [SerializeField] private SO_Vulnerable _vulnerable;
        [SerializeField] private int _layers = 3;

        public SO_Vulnerable Vulnerable => _vulnerable;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.AnyEnemy;
    }
}
