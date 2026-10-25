using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>虚弱药水：对一名敌人施加若干层虚弱；层数随条目配，状态定义资产不携带层数。</summary>
    [CreateAssetMenu(fileName = "WeakPotion", menuName = "DreamKeep/Data/Potion/虚弱药水")]
    public sealed class SO_WeakPotion : SO_Potion
    {
        [SerializeField] private SO_Weak _perk;
        [SerializeField] private int _layers = 3;

        public SO_Weak Perk => _perk;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.AnyEnemy;
    }
}
