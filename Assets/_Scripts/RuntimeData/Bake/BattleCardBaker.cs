using System.Collections.Generic;

namespace _DreamKeep.RuntimeData
{
    /// <summary>战斗卡烘焙器：每场战斗开局把局外卡深拷贝成战斗卡，两层之间不共享任何词条实例。</summary>
    public static class BattleCardBaker
    {
        #region 深拷贝一张局外卡：费用取当前值，落点与词条逐条复制
        public static InBattleCard Bake(InGameCard card_)
        {
            // 进战斗时局外卡上不存在任何修正器，Cost.Value 与基值一致
            InBattleCard baked = new(card_.CardId, card_.Cost.Value, card_.Target);
            for (int i = 0; i < card_.Affixes.Count; i++)
                baked.AddAffix(card_.Affixes[i].Clone());
            return baked;
        }
        #endregion

        #region 批量深拷贝整副牌组
        public static List<InBattleCard> BakeAll(List<InGameCard> cards_)
        {
            List<InBattleCard> baked = new(cards_.Count);
            for (int i = 0; i < cards_.Count; i++)
                baked.Add(Bake(cards_[i]));
            return baked;
        }
        #endregion
    }
}
