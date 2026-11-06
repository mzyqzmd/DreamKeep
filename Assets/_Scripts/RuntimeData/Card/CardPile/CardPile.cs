using System.Collections.Generic;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>通用牌堆：抽牌堆、手牌、弃牌堆、消耗堆、打出堆与局内卡组共用，提供进出、洗牌与整体转移。
    /// 列表顺序即卡在堆中的顺序；对外只读，改堆一律经本类，避免外部写出不连续的槽位。</summary>
    public class CardPile
    {
        private readonly List<CardModel> _cards = new();

        public int Count => _cards.Count;
        public IReadOnlyList<CardModel> Cards => _cards;

        #region 把一张卡追加到堆尾
        public void Add(CardModel card_) => _cards.Add(card_);
        #endregion

        #region 取出堆顶的一张卡，堆为空时返回 null
        public CardModel DrawTop()
        {
            if (_cards.Count == 0) return null;
            CardModel top = _cards[0];
            _cards.RemoveAt(0);
            return top;
        }
        #endregion

        #region 按实例移除指定卡，用于打出与定向弃置
        public bool Remove(CardModel card_)
        {
            int index = _cards.IndexOf(card_);
            if (index < 0) return false;
            _cards.RemoveAt(index);
            return true;
        }
        #endregion

        #region 判断指定卡是否在本堆中，用于出牌等命令的合法性校验
        public bool Contains(CardModel card_) => _cards.Contains(card_);
        #endregion

        #region 委托确定性随机源原地洗牌，保证同种子可复现
        public void Shuffle(Rng rng_) => rng_.Shuffle(_cards);
        #endregion

        #region 把本堆全部卡搬入目标堆并清空自身，用于弃牌堆洗回抽牌堆
        public void MoveAllTo(CardPile target_)
        {
            for (int i = 0; i < _cards.Count; i++) target_.Add(_cards[i]);
            _cards.Clear();
        }
        #endregion
    }
}
