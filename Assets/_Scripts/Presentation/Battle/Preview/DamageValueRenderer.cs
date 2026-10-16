using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>伤害数值渲染器：基础值经上行门面的修正查询求最终值，按"高于/低于基础值"着升/降色（相等原色）；
    /// 未推入预览门面时退化为原值纯文本。颜色由持有方配置（卡面按不可打压暗、气泡用原色）。</summary>
    public sealed class DamageValueRenderer : IValueRenderer
    {
        private Color _boostColor;
        private Color _reduceColor;
        private BattleQueryDispatcher _queryDispatcher;
        private Creature _target;

        public DamageValueRenderer(Color boostColor_, Color reduceColor_)
        {
            _boostColor = boostColor_;
            _reduceColor = reduceColor_;
        }

        #region 预览门面与目标：由持有方推入（未推门面即原值输出）

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

        /// <summary>渲染一次伤害数值：修正后的值经 UiValueText 着色；未推门面按原值输出。</summary>
        public string Render(int baseAmount_) =>
            UiValueText.Value(_queryDispatcher == null
                    ? baseAmount_
                    : _queryDispatcher.PreviewCardDamage(_target, baseAmount_),
                baseAmount_, _boostColor, _reduceColor);
    }
}
