using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡面数值渲染器：把词条的基准值（升级前）、当前值与所属通道换成该显示的文本——伤害与格挡经上行门面现算最终值
    ///（多个目标时按"各目标的最终值是否一致"折叠：一致就显示该值，不一致回退只算来源侧修正；未推门面则直接用当前值），
    /// 其余通道一律静态比较基准值与当前值；着色方向按通道分派，相等一律原色：
    /// 伤害/格挡高绿低红、抽牌/层数仅升绿、失血低绿高红、费用仅降绿。颜色由持有方配置（卡面按不可打压暗、气泡用原色）。</summary>
    public sealed class CardValueRenderer : IStatRenderer
    {
        private Color _boostColor;
        private Color _reduceColor;
        private Color _costLowerColor;
        private BattleQueryDispatcher _queryDispatcher;
        private IReadOnlyList<Creature> _targets;

        public CardValueRenderer(Color boostColor_, Color reduceColor_, Color costLowerColor_)
        {
            _boostColor = boostColor_;
            _reduceColor = reduceColor_;
            _costLowerColor = costLowerColor_;
        }

        #region 预览门面与配色：由持有方推入（未推门面即原值输出）

        /// <summary>推入预览门面与当前的目标集合（持有方每帧对账时调用；门面为空时伤害与格挡直接用当前值
        /// 与升级前基准静态比较着色，目标集合为空或为 null 即只算来源侧修正）。</summary>
        public void SetContext(BattleQueryDispatcher queryDispatcher_, IReadOnlyList<Creature> targets_)
        {
            _queryDispatcher = queryDispatcher_;
            _targets = targets_;
        }

        /// <summary>换升/降/减费三色（卡面按不可打状态压暗时更新）。</summary>
        public void SetColors(Color boostColor_, Color reduceColor_, Color costLowerColor_)
        {
            _boostColor = boostColor_;
            _reduceColor = reduceColor_;
            _costLowerColor = costLowerColor_;
        }

        #endregion

        #region 求值：跨目标折叠，供渲染与手牌侧的数值差分共用

        /// <summary>跨目标折叠求值（预览基数=当前值，含升级增量）：各目标的最终值一致就取该值（多目标卡也能给出准确数字），
        /// 不一致回退只含来源侧修正的值——目标之间的差异无法用一个数字表达，回退基准即"不显示目标侧修正"；
        /// 目标集合为空或为 null 同样只算来源侧。按词条各自调用，故伤害与格挡互不影响。</summary>
        public static int Fold(BattleQueryDispatcher queryDispatcher_, IReadOnlyList<Creature> targets_, int value_,
            EStatChannel channel_)
        {
            if (targets_ == null || targets_.Count == 0) return Resolve(queryDispatcher_, channel_, value_, null);

            int shown = Resolve(queryDispatcher_, channel_, value_, targets_[0]);
            for (int i = 1; i < targets_.Count; i++)
                if (Resolve(queryDispatcher_, channel_, value_, targets_[i]) != shown)
                    return Resolve(queryDispatcher_, channel_, value_, null);
            return shown;
        }

        // 单目标求值：与结算共用同一份规则（传 null 即只算来源侧修正）
        private static int Resolve(BattleQueryDispatcher queryDispatcher_, EStatChannel channel_, int baseValue_,
            Creature target_) =>
            channel_ == EStatChannel.Damage
                ? queryDispatcher_.PreviewCardDamage(target_, baseValue_)
                : queryDispatcher_.PreviewCardBlock(target_, baseValue_);

        #endregion

        /// <summary>渲染一次数值：伤害与格挡经门面求最终值（基数=当前值），全部通道按各自方向与基准值（升级前）着色，
        /// 相等保持原色；未推门面时伤害与格挡不做 perk 修正，直接用当前值静态比较。</summary>
        // TODO 修正来源显示：将来要把"9（力量+2）"这类来源写进卡面/气泡时，在此接住门面返回的贡献者列表并展开成文本（现在只显示最终值）
        public string Render(int baseValue_, int value_, EStatChannel channel_)
        {
            // 费用与层数是静态比较（比较基准与当前值都由调用方给出），不经门面求值，故最先处理
            if (channel_ == EStatChannel.Cost) return UiValueText.Lower(value_, baseValue_, _costLowerColor);
            if (channel_ == EStatChannel.PerkLayers) return UiValueText.Higher(value_, baseValue_, _boostColor);

            // 伤害与格挡的最终值含战斗内 perk 修正，经门面现算；基数取当前值（含升级增量），其余通道不经门面
            int shown = value_;
            if (_queryDispatcher != null && channel_ is EStatChannel.Damage or EStatChannel.Block)
                shown = Fold(_queryDispatcher, _targets, value_, channel_);

            return channel_ switch
            {
                EStatChannel.Damage or EStatChannel.Block =>
                    UiValueText.Value(shown, baseValue_, _boostColor, _reduceColor),
                EStatChannel.LostHealth => UiValueText.Inverse(shown, baseValue_, _boostColor, _reduceColor),
                _ => UiValueText.Higher(shown, baseValue_, _boostColor),
            };
        }
    }
}
