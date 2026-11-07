using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>玩家战斗台词表：按失败类别分组，每组多条随机取一句；空组视为不说话。</summary>
    [CreateAssetMenu(fileName = "PlayerSpeechLines", menuName = "DreamKeep/Data/Speech/PlayerSpeechLines")]
    public sealed class SO_PlayerSpeechLines : ScriptableObject
    {
        [Header("出牌被拒")] [Tooltip("能量不足")] [SerializeField]
        private List<string> _notEnoughEnergy = new();

        [Tooltip("牌带「不能被打出」词条")] [SerializeField]
        private List<string> _unplayable = new();

        [Tooltip("昏眩下本回合已打满 1 张")] [SerializeField]
        private List<string> _dizzyLimited = new();

        [Header("抽牌溢出")] [Tooltip("手牌已达上限，抽到的牌直接进弃牌堆")] [SerializeField]
        private List<string> _handOverflow = new();

        public string PickNotEnoughEnergy() => SpeechLinePicker.Pick(_notEnoughEnergy);
        public string PickUnplayable() => SpeechLinePicker.Pick(_unplayable);
        public string PickDizzyLimited() => SpeechLinePicker.Pick(_dizzyLimited);
        public string PickHandOverflow() => SpeechLinePicker.Pick(_handOverflow);
    }
}
