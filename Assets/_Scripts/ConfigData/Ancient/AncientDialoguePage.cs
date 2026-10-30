using System;
using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>对话页：一次拜访要演的完整台词，页内按行序逐句演绎。</summary>
    [Serializable]
    public class AncientDialoguePage
    {
        [SerializeField] private List<AncientDialogueLine> _lines = new();

        public IReadOnlyList<AncientDialogueLine> Lines => _lines;
    }
}
