using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>说话人外观：一条对话气泡的头像、底色与字色，配在 SO_Ancient 的两个说话人位上，视图逐条应用。</summary>
    [Serializable]
    public class AncientSpeakerView
    {
        [SerializeField] private Sprite _portrait;

        [SerializeField] private Color _bubbleColor = Color.white;

        [SerializeField] private Color _textColor = Color.black;

        /// <summary>头像：可空，空则该条气泡隐藏头像位。</summary>
        public Sprite Portrait => _portrait;

        public Color BubbleColor => _bubbleColor;
        public Color TextColor => _textColor;
    }
}
