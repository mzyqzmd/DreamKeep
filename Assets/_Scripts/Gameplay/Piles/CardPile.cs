using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>通用牌堆：抽牌堆、手牌、弃牌堆共用，提供进出、洗牌与整体转移。以连续槽位索引为键存卡，索引即卡在堆中的顺序。</summary>
    public class CardPile
    {
        private readonly Dictionary<int, InBattleCard> _cards = new();

        public int Count => _cards.Count;
        public Dictionary<int, InBattleCard> Cards => _cards;

        #region 把一张卡追加到堆尾
        public void Add(InBattleCard card_) => _cards[_cards.Count] = card_;
        #endregion

        #region 取出堆顶的一张卡，堆为空时返回 null
        public InBattleCard DrawTop()
        {
            if (_cards.Count == 0) return null;
            return RemoveAt(0);
        }
        #endregion

        #region 按实例移除指定卡，用于打出与定向弃置
        public bool Remove(InBattleCard card_)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != card_) continue;
                RemoveAt(i);
                return true;
            }
            return false;
        }
        #endregion

        #region 按卡实例 Id 查找本堆中的卡，找不到返回 null
        public InBattleCard Find(int instanceId_)
        {
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].InstanceId == instanceId_)
                    return _cards[i];
            return null;
        }
        #endregion

        #region 用确定性随机源做原地 Fisher-Yates 洗牌，保证同种子可复现
        public void Shuffle(Rng rng_)
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = rng_.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }
        #endregion

        #region 把本堆全部卡搬入目标堆并清空自身，用于弃牌堆洗回抽牌堆
        public void MoveAllTo(CardPile target_)
        {
            for (int i = 0; i < _cards.Count; i++)
                target_.Add(_cards[i]);
            _cards.Clear();
        }
        #endregion

        /// <summary>移除指定槽位并把后续卡整体前移，保持槽位索引连续。</summary>
        private InBattleCard RemoveAt(int slot_)
        {
            InBattleCard card = _cards[slot_];
            for (int i = slot_; i < _cards.Count - 1; i++)
                _cards[i] = _cards[i + 1];
            _cards.Remove(_cards.Count - 1);
            return card;
        }
    }
}
