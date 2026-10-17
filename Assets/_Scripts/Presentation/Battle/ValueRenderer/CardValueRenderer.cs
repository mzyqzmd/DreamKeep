using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>卡面数值渲染器：把词条的基础值与所属通道换成该显示的文本——伤害与格挡两条通道经上行门面现算最终值，
    /// 按"高于/低于基础值"着升/降色（相等原色）；未推入预览门面或未登记的通道退化为原值纯文本。
    /// 颜色由持有方配置（卡面按不可打压暗、气泡用原色）。</summary>
    public sealed class CardValueRenderer : IStatRenderer
    {
        private Color _boostColor;
        private Color _reduceColor;
        private BattleQueryDispatcher _queryDispatcher;
        private Creature _target;

        public CardValueRenderer(Color boostColor_, Color reduceColor_)
        {
            _boostColor = boostColor_;
            _reduceColor = reduceColor_;
        }

        #region 预览门面与配色：由持有方推入（未推门面即原值输出）

        /// <summary>推入预览门面与当前目标（持有方每帧对账时调用；门面为空即退化为原值纯文本）。</summary>
        public void SetContext(BattleQueryDispatcher queryDispatcher_, Creature target_)
        {
            _queryDispatcher = queryDispatcher_;
            _target = target_;
        }

        /// <summary>换升/降两色（卡面按不可打状态压暗时更新）。</summary>
        public void SetColors(Color boostColor_, Color reduceColor_)
        {
            _boostColor = boostColor_;
            _reduceColor = reduceColor_;
        }

        #endregion

        /// <summary>渲染一次数值：按通道经门面求最终值后着色；未推门面或未登记通道按原值输出。</summary>
        // TODO 修正来源显示：将来要把"9（力量+2）"这类来源写进卡面/气泡时，在此接住门面返回的贡献者列表并展开成文本（现在只显示最终值）
        public string Render(int baseAmount_, EStatChannel channel_)
        {
            if (_queryDispatcher == null) return baseAmount_.ToString();
            int shown = channel_ switch
            {
                EStatChannel.Damage => _queryDispatcher.PreviewCardDamage(_target, baseAmount_),
                EStatChannel.Block => _queryDispatcher.PreviewCardBlock(_target, baseAmount_),
                _ => baseAmount_,
            };
            return UiValueText.Value(shown, baseAmount_, _boostColor, _reduceColor);
        }
    }
}
