using System.Collections.Generic;
using _DreamKeep.Core;
using _DreamKeep.Data;

namespace _DreamKeep.Domain
{
    /// <summary>通用牌堆：抽牌堆、手牌、弃牌堆共用，提供进出、洗牌与整体转移。</summary>
    public class CardPile
    {
        private readonly List<SO_Card> _cards = new();

        public int Count => _cards.Count;
        public IReadOnlyList<SO_Card> Cards => _cards;

        /// <summary>把一张卡放入堆顶。</summary>
        public void Add(SO_Card card_) => _cards.Add(card_);

        /// <summary>取出堆顶的一张卡，堆为空时返回 null。</summary>
        public SO_Card DrawTop()
        {
            if (_cards.Count == 0) return null;
            SO_Card card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }

        /// <summary>用确定性随机源做原地 Fisher-Yates 洗牌，保证同种子可复现。</summary>
        public void Shuffle(Rng rng_)
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = rng_.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        /// <summary>把本堆全部卡搬入目标堆并清空自身，用于弃牌堆洗回抽牌堆。</summary>
        public void MoveAllTo(CardPile target_)
        {
            foreach (SO_Card card in _cards)
                target_.Add(card);
            _cards.Clear();
        }
    }
}
