using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>缚魂药水：对全体敌人同时施加虚弱与易伤，两个状态各自配定义资产与层数。</summary>
    [CreateAssetMenu(fileName = "PotionOfBinding", menuName = "DreamKeep/Data/Potion/缚魂药水")]
    public sealed class SO_PotionOfBinding : SO_Potion
    {
        [SerializeField] private SO_Weak _weakPerk;
        [SerializeField] private int _weakLayers = 1;
        [SerializeField] private SO_Vulnerable _vulnerablePerk;
        [SerializeField] private int _vulnerableLayers = 1;

        public SO_Weak WeakPerk => _weakPerk;
        public int WeakLayers => _weakLayers;
        public SO_Vulnerable VulnerablePerk => _vulnerablePerk;
        public int VulnerableLayers => _vulnerableLayers;
        public override EPotionTargetType TargetType => EPotionTargetType.AllEnemy;
    }
}
