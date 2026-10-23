using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡牌陈列排序工具：牌堆查看器等纯展示场合的显示顺序，只影响陈列、不写回域内任何堆。
    /// 按类型（诅咒→能力→技能→攻击）与按费用（低→高）各一条入口，均可用 descending 反转方向；
    /// 主键相同者一律保持域内相对顺序（洗牌后的堆序不被搅乱），倒序只翻转主键、不反转同值组；
    /// 未登记类型与无费用卡（费用为负）恒排最后，不参与方向翻转。</summary>
    public static class CardSorter
    {
        // 未登记类型的兜底权重：数量上排在所有已知类型之后
        private const int UnknownTypeWeight = 4;

        #region 排序入口

        /// <summary>按类型陈列顺序原位排序：正序诅咒→能力→技能→攻击，descending 反转为攻击→技能→能力→诅咒。</summary>
        public static void SortByType(List<CardModel> cards_, bool descending_ = false) =>
            Sort(cards_, (a_, b_) => CompareType(a_, b_, descending_));

        /// <summary>按当前费用原位排序：正序低费在前、高费在后，descending 反转；无费用卡（费用为负）两个方向都排最后。</summary>
        public static void SortByCost(List<CardModel> cards_, bool descending_ = false) =>
            Sort(cards_, (a_, b_) => CompareCost(a_, b_, descending_));

        #endregion

        // 类型陈列权重：越小越靠前——诅咒、能力、技能、攻击；未知类型兜底排最后
        private static int TypeWeight(ECardType type_) => type_ switch
        {
            ECardType.Curse => 0,
            ECardType.Ability => 1,
            ECardType.Skill => 2,
            ECardType.Attack => 3,
            _ => UnknownTypeWeight,
        };

        // 稳定排序：List.Sort 不稳定，故装饰上原下标做次级键再排序写回，主键相同者保持输入相对顺序
        private static void Sort(List<CardModel> cards_, Comparison<CardModel> comparison_)
        {
            if (cards_.Count < 2) return;
            (CardModel Card, int Slot)[] decorated = new (CardModel, int)[cards_.Count];
            for (int i = 0; i < cards_.Count; i++) decorated[i] = (cards_[i], i);
            Array.Sort(decorated, (a_, b_) =>
            {
                int byKey = comparison_(a_.Card, b_.Card);
                return byKey != 0 ? byKey : a_.Slot.CompareTo(b_.Slot);
            });
            for (int i = 0; i < decorated.Length; i++) cards_[i] = decorated[i].Card;
        }

        // 类型比较：未登记类型恒排最后（两个方向都不翻转），其余按陈列权重，descending 反转
        private static int CompareType(CardModel a_, CardModel b_, bool descending_)
        {
            int weightA = TypeWeight(a_.Type);
            int weightB = TypeWeight(b_.Type);
            bool unknownA = weightA == UnknownTypeWeight;
            bool unknownB = weightB == UnknownTypeWeight;
            if (unknownA || unknownB)
            {
                if (unknownA == unknownB) return 0;
                return unknownA ? 1 : -1;
            }

            return descending_ ? weightB.CompareTo(weightA) : weightA.CompareTo(weightB);
        }

        // 费用比较：无费用（费用为负）恒排最后且组内保原序，其余按当前费用（含修正），descending 反转
        private static int CompareCost(CardModel a_, CardModel b_, bool descending_)
        {
            int costA = a_.Cost.Value;
            int costB = b_.Cost.Value;
            bool freeA = costA < 0;
            bool freeB = costB < 0;
            if (freeA || freeB)
            {
                if (freeA == freeB) return 0;
                return freeA ? 1 : -1;
            }

            return descending_ ? costB.CompareTo(costA) : costA.CompareTo(costB);
        }
    }
}