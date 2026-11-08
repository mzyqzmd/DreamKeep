using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>玩家战斗台词表：按失败类别分组，每组是否开口、说几次由分组自身与次数台账决定。</summary>
    [CreateAssetMenu(fileName = "PlayerSpeechLines", menuName = "DreamKeep/Data/Speech/PlayerSpeechLines")]
    public sealed class SO_PlayerSpeechLines : ScriptableObject
    {
        [Header("出牌被拒")] [Tooltip("能量不足")] [SerializeField]
        private SpeechLineGroup _notEnoughEnergy = new();

        [Tooltip("牌带「不能被打出」词条")] [SerializeField]
        private SpeechLineGroup _unplayable = new();

        [Tooltip("昏眩下本回合已打满 1 张")] [SerializeField]
        private SpeechLineGroup _dizzyLimited = new();

        [Header("抽牌溢出")] [Tooltip("手牌已达上限，抽到的牌直接进弃牌堆")] [SerializeField]
        private SpeechLineGroup _handOverflow = new();

        public SpeechLineGroup NotEnoughEnergy => _notEnoughEnergy;
        public SpeechLineGroup Unplayable => _unplayable;
        public SpeechLineGroup DizzyLimited => _dizzyLimited;
        public SpeechLineGroup HandOverflow => _handOverflow;
    }
}
