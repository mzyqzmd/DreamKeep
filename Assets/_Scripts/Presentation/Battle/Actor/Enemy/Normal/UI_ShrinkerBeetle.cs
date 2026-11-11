using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>缩小甲虫视图：自己给玩家施加负面状态时完整播一次施法动作（cast），其余表现全走基类。</summary>
    public sealed class UI_ShrinkerBeetle : UI_Enemy
    {
        // shrinker_beetle 专属动画：cast 是它给玩家挂减益时的施法手势
        private const string CastAnim = "cast";

        // 开局校验专属动画名：Bind 不对子类开放，而战斗开始事件必然在单位 Bind 之后
        protected override void OnBattleStarted(BE_BattleStarted started_)
        {
            base.OnBattleStarted(started_);
            _animTool.VerifyAnim(CastAnim);
        }

        // 只认"自己施加给玩家"的减益：自我强化、增益与其它人的施加都不播；cast 整段播完才放行事件链
        protected override async UniTask OnPerkApplied(BE_PerkApplied applied_)
        {
            await base.OnPerkApplied(applied_);
            if (applied_.Source != _boundCreature || applied_.Target == _boundCreature) return;
            if (applied_.Perk.Polarity != EPerkPolarity.Debuff) return;
            await _animTool.PlayOnceAsync(CastAnim, playRatio_: 1f, onFinished_: () => _animTool.PlayLoop(IdleLoopAnim));
        }
    }
}
