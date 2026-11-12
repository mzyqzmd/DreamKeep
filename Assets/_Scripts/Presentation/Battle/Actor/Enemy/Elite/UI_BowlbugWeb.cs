using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>盛碗虫（丝）视图：给玩家施加负面时播吐丝，其余沿用公约动画。</summary>
    public sealed class UI_BowlbugWeb : UI_Enemy
    {
        private const string SpitAnim = "spit";

        // 装配钩子：校验吐丝动画名
        protected override void OnEnemyBound(BattleEventDispatcher eventDispatcher_) => _animTool.VerifyAnim(SpitAnim);

        // 施加状态：槽位刷新沿用基类；自己给玩家施加负面时播 spit（事件链等它放行）
        protected override async UniTask OnPerkApplied(BE_PerkApplied applied_)
        {
            await base.OnPerkApplied(applied_);
            if (_boundCreature.IsDead || applied_.Source != _boundCreature || applied_.Target == _boundCreature ||
                applied_.Perk.Polarity != EPerkPolarity.Debuff)
                return;
            await _animTool.PlayOnceAsync(SpitAnim, onFinished_: () => _animTool.PlayLoop(IdleLoopAnim));
        }
    }
}