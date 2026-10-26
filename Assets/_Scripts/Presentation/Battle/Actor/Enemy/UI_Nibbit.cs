using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>小啃兽视图：自己给自己叠加力量时完整播一次示威动作（hiss），其余表现全走基类。</summary>
    public sealed class UI_Nibbit : UI_Enemy
    {
        // 尼比特专属动画：hiss 是它获得力量时的示威动作
        private const string HissAnim = "hiss";

        // 开局校验专属动画名：Bind 不对子类开放，而战斗开始事件必然在单位 Bind 之后
        protected override void OnBattleStarted(BE_BattleStarted started_)
        {
            base.OnBattleStarted(started_);
            _animTool.VerifyAnim(HissAnim);
        }

        // 只认自己给自己力量：玩家施加的力量与其它状态都不播；hiss 整段播完才放行事件链
        protected override async UniTask OnPerkApplied(BE_PerkApplied applied_)
        {
            await base.OnPerkApplied(applied_);
            if (applied_.Source != _boundCreature || applied_.Target != _boundCreature) return;
            if (!(applied_.Perk is BP_Strength)) return;
            await _animTool.PlayOnceAsync(HissAnim, playRatio_: 1f, onFinished_: () => _animTool.PlayLoop(IdleLoopAnim));
        }
    }
}
