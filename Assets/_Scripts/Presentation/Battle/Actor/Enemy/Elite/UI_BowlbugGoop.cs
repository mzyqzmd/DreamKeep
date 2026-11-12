using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>盛碗虫（蜜）视图：自己获得增益时播 buff，其余沿用公约动画。</summary>
    public sealed class UI_BowlbugGoop : UI_Enemy
    {
        private const string BuffAnim = "buff";

        // 装配钩子：校验增益动画名
        protected override void OnEnemyBound(BattleEventDispatcher eventDispatcher_) => _animTool.VerifyAnim(BuffAnim);

        // 施加状态：槽位刷新沿用基类；自己获得增益时播 buff（事件链等它放行）
        protected override async UniTask OnPerkApplied(BE_PerkApplied applied_)
        {
            await base.OnPerkApplied(applied_);
            if (_boundCreature.IsDead || applied_.Source != _boundCreature || applied_.Target != _boundCreature ||
                applied_.Perk.Polarity != EPerkPolarity.Buff)
                return;
            await _animTool.PlayOnceAsync(BuffAnim, onFinished_: () => _animTool.PlayLoop(IdleLoopAnim));
        }
    }
}