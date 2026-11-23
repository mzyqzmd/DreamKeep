using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>闪亮口红：指定回合的玩家回合开始时获得力量与敏捷。</summary>
    [CreateAssetMenu(fileName = "Lipstick", menuName = "DreamKeep/Data/Relic/闪亮口红")]
    public sealed class SO_Lipstick : SO_Relic
    {
        [SerializeField] private int _turn = 3;
        [SerializeField] private SO_Strength _strength;
        [SerializeField] private int _strengthLayers = 1;
        [SerializeField] private SO_Dexterity _dexterity;
        [SerializeField] private int _dexterityLayers = 1;

        public int Turn => _turn;
        public SO_Strength Strength => _strength;
        public int StrengthLayers => _strengthLayers;
        public SO_Dexterity Dexterity => _dexterity;
        public int DexterityLayers => _dexterityLayers;
    }
}
