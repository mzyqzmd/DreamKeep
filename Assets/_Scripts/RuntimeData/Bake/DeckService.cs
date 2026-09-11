using System.Collections.Generic;
using _DreamKeep.Data;

namespace _DreamKeep.RuntimeData
{
    /// <summary>卡组服务：持有本局卡组原型，是烘焙能力的唯一宿主，原型只在局外被改动。</summary>
    public class DeckService
    {
        private readonly Dictionary<int, CardModel> _deck = new();
        private int _nextCardId = 1;

        /// <summary>本局卡组原型，键为本服务分配的卡组编号，不保证任何排列顺序。</summary>
        public IReadOnlyDictionary<int, CardModel> Deck => _deck;

        #region 按卡 Id 列表烘焙起始卡组，配置缺失的 Id 直接跳过

        public void BakeStarter(IReadOnlyList<string> cardIds_)
        {
            for (int i = 0; i < cardIds_.Count; i++)
                if (CardBaker.TryBake(cardIds_[i], out var card))
                    _deck.Add(_nextCardId++, card);
        }

        #endregion

        #region 往卡组追加一张卡：按配置 Id 现烘焙卡组原型，配置缺失时返回 null

        public CardModel AddCard(string cardId_)
        {
            if (!CardBaker.TryBake(cardId_, out CardModel card)) return null;
            _deck.Add(_nextCardId++, card);
            return card;
        }

        #endregion

        #region 从卡组永久移除一张卡：按卡组编号定位，重复移除安全

        public bool RemoveCard(int cardId_) => _deck.Remove(cardId_);

        #endregion
    }
}