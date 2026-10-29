using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>一份奖励条目：只描述"给什么"，不含领取动作——领取由持有状态的 RunState / Player 自己做。
    /// 卡牌条目携带三张候选（canonical 原型），选中一张后由领取方克隆成局内卡再进牌组。
    /// 四个静态工厂与 Kind 一一对应，读哪几个字段由 Kind 决定。</summary>
    public readonly struct RunReward
    {
        public ERewardKind Kind { get; }

        /// <summary>金币条目：给多少。</summary>
        public int Gold { get; }

        /// <summary>卡牌条目：候选卡（canonical 原型，恒为三张，池不足时按实际数量）。</summary>
        public IReadOnlyList<CardModel> Cards { get; }

        /// <summary>药水条目：给哪一瓶。</summary>
        public SO_Potion Potion { get; }

        /// <summary>遗物条目：给哪一件。</summary>
        public SO_Relic Relic { get; }

        private RunReward(ERewardKind kind_, int gold_, IReadOnlyList<CardModel> cards_, SO_Potion potion_,
            SO_Relic relic_)
        {
            Kind = kind_;
            Gold = gold_;
            Cards = cards_;
            Potion = potion_;
            Relic = relic_;
        }

        public static RunReward GoldOf(int amount_) => new(ERewardKind.Gold, amount_, null, null, null);

        public static RunReward CardsOf(IReadOnlyList<CardModel> options_) =>
            new(ERewardKind.Card, 0, options_, null, null);

        public static RunReward PotionOf(SO_Potion potion_) => new(ERewardKind.Potion, 0, null, potion_, null);

        public static RunReward RelicOf(SO_Relic relic_) => new(ERewardKind.Relic, 0, null, null, relic_);
    }
}
