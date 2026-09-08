using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>卡牌数据库：以字符串 Id 为键集中管理全部卡牌，卡牌自身不持有 Id。</summary>
    [CreateAssetMenu(fileName = "NewCardDatabase", menuName = "DreamKeep/Data/Card Database")]
    public class SO_CardDatabase : SerializedScriptableObject
    {
        [OdinSerialize] private readonly Dictionary<string, SO_Card> _cards = new();

        public bool TryGetCard(string cardId_, out SO_Card card_) =>
            _cards.TryGetValue(cardId_, out card_);
    }
}
