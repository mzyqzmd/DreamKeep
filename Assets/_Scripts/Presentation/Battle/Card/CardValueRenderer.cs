using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>卡面数值渲染器：把词条的基准值、当前值与所属通道换成该显示的文本——伤害与格挡两条通道经上行门面现算最终值，
    /// 费用的"低于基准"与层数的"高于基准"按各自方向着色（相等原色）；未推入预览门面或未登记的通道退化为当前值纯文本。
    /// 颜色由持有方配置（卡面按不可打压暗、气泡用原色）。</summary>
    public sealed class CardValueRenderer : IStatRenderer
    {
        private Color _boostColor;
        private Color _reduceColor;
        private Color _costLowerColor;
        private BattleQueryDispatcher _queryDispatcher;
        private Creature _target;

        public CardValueRenderer(Color boostColor_, Color reduceColor_, Color costLowerColor_)
        {
            _boostColor = boostColor_;
            _reduceColor = reduceColor_;
            _costLowerColor = costLowerColor_;
        }

        #region 预览门面与配色：由持有方推入（未推门面即原值输出）

        /// <summary>推入预览门面与当前目标（持有方每帧对账时调用；门面为空即退化为原值纯文本）。</summary>
        public void SetContext(BattleQueryDispatcher queryDispatcher_, Creature target_)
        {
            _queryDispatcher = queryDispatcher_;
            _target = target_;
        }

        /// <summary>换升/降/减费三色（卡面按不可打状态压暗时更新）。</summary>
        public void SetColors(Color boostColor_, Color reduceColor_, Color costLowerColor_)
        {
            _boostColor = boostColor_;
            _reduceColor = reduceColor_;
            _costLowerColor = costLowerColor_;
        }

        #endregion

        /// <summary>渲染一次数值：按通道经门面求最终值后着色；未推门面或未登记通道按当前值输出。</summary>
        // TODO 修正来源显示：将来要把"9（力量+2）"这类来源写进卡面/气泡时，在此接住门面返回的贡献者列表并展开成文本（现在只显示最终值）
        public string Render(int baseValue_, int value_, EStatChannel channel_)
        {
            // 费用与层数是静态比较（比较基准与当前值都由调用方给出），不经门面求值，故先于门面守卫处理
            if (channel_ == EStatChannel.Cost) return UiValueText.Lower(value_, baseValue_, _costLowerColor);
            if (channel_ == EStatChannel.PerkLayers) return UiValueText.Higher(value_, baseValue_, _boostColor);
            if (_queryDispatcher == null) return value_.ToString();
            int shown = channel_ switch
            {
                EStatChannel.Damage => _queryDispatcher.PreviewCardDamage(_target, baseValue_),
                EStatChannel.Block => _queryDispatcher.PreviewCardBlock(_target, baseValue_),
                _ => value_,
            };
            return UiValueText.Value(shown, baseValue_, _boostColor, _reduceColor);
        }
    }
}
