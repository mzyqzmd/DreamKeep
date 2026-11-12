using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>多尼斯异鸟视图：首次获得力量时整段播发怒过渡，之后待机、攻击与受击常驻愤怒组动画；死亡沿用公约。</summary>
    public sealed class UI_Byrdonis : UI_Enemy
    {
        private const string AngryLoopAnim = "angry_loop";
        private const string AngryAttackAnim = "attack_angry";
        private const string AngryHitAnim = "hurt_angry";
        private const string GetAngryAnim = "get_angry";

        private bool _angry;

        protected override string IdleLoopAnim => _angry ? AngryLoopAnim : base.IdleLoopAnim;
        protected override string AttackAnim => _angry ? AngryAttackAnim : base.AttackAnim;
        protected override string HitAnim => _angry ? AngryHitAnim : base.HitAnim;

        // 装配钩子：校验愤怒组与发怒过渡动画名
        protected override void OnEnemyBound(BattleEventDispatcher eventDispatcher_) =>
            _animTool.VerifyAnim(AngryLoopAnim, AngryAttackAnim, AngryHitAnim, GetAngryAnim);

        // 自己给自己力量：只在首次整段播发怒过渡，播完回愤怒循环
        protected override async UniTask OnPerkApplied(BE_PerkApplied applied_)
        {
            await base.OnPerkApplied(applied_);
            if (_angry || _boundCreature.IsDead) return;
            if (applied_.Source != _boundCreature || applied_.Target != _boundCreature) return;
            if (!(applied_.Perk is BP_Strength)) return;
            _angry = true;
            await _animTool.PlayOnceAsync(GetAngryAnim, 1f, () => _animTool.PlayLoop(IdleLoopAnim));
        }
    }
}
