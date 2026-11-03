using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>对话行：一句台词＝说话人加文本；文本为 TMP 富文本，可直接带强调标记。</summary>
    [Serializable]
    public class AncientDialogueLine
    {
        [SerializeField] private EAncientSpeaker _speaker;

        [TextArea(1, 4)]
        [SerializeField] private string _text;

        public EAncientSpeaker Speaker => _speaker;
        public string Text => _text;
    }
}
