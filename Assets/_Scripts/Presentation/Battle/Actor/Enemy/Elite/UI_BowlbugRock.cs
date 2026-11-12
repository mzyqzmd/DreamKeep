using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>盛碗虫（石）视图：攻击播头槌，本次攻击被完全格挡（失衡将触发）改播失衡后缀头槌；
    /// 进入击晕态播 stun 后常驻击晕循环、醒来直接回待机，击晕期间挨打受击后回击晕循环。</summary>
    public sealed class UI_BowlbugRock : UI_Enemy
    {
        private const string HeadbuttAnim = "headbutt";
        private const string HeadbuttStunnedAnim = "headbutt_stunned";
        private const string StunAnim = "stun";
        private const string StunnedLoopAnim = "stunned_loop";

        protected override string AttackAnim => HeadbuttAnim;

        // 装配钩子：校验头槌与击晕链动画名，并订阅击晕演出事件
        protected override void OnEnemyBound(BattleEventDispatcher eventDispatcher_)
        {
            _animTool.VerifyAnim(HeadbuttAnim, HeadbuttStunnedAnim, StunAnim, StunnedLoopAnim);
            eventDispatcher_.Register<BE_EnemyStunned>(OnStunnedAsync);
            eventDispatcher_.Register<BE_EnemyStunEnded>(OnStunEndedAsync);
        }

        // 我打人：本次伤害被完全格挡（失衡即将触发）改播失衡后缀头槌，其余播普通头槌
        protected override async UniTask OnDealtDamageAsync(BE_DamageDealt dealt_)
        {
            if (_boundCreature.IsDead || dealt_.Source != _boundCreature)
            {
                await base.OnDealtDamageAsync(dealt_);
                return;
            }

            string attackAnim = dealt_.Amount > 0 && dealt_.Absorbed == dealt_.Amount
                ? HeadbuttStunnedAnim
                : HeadbuttAnim;
            await _animTool.PlayOnceAsync(attackAnim, onFinished_: () => _animTool.PlayLoop(IdleLoopAnim));
        }

        // 我挨打：击晕期间被打，hurt 播完回击晕循环而不是待机；其余沿用基类
        protected override async UniTask OnDamagedAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != _boundCreature || !_boundEnemy.IsStunned)
            {
                await base.OnDamagedAsync(dealt_);
                return;
            }

            UniTask wait = _animTool.PlayOnceAsync(HitAnim, onFinished_: () => _animTool.PlayLoop(StunnedLoopAnim));
            if (_blockView) _blockView.SetBlock(dealt_.Target.Block);
            if (_healthBar) _healthBar.SetHealth(dealt_.Target.CurrentHp);
            await wait;
        }

        // 击晕链：进入击晕态先播 stun 再进击晕循环
        private async UniTask OnStunnedAsync(BE_EnemyStunned stunned_)
        {
            if (stunned_.Enemy != _boundEnemy || _boundCreature.IsDead) return;
            await _animTool.PlayOnceAsync(StunAnim, onFinished_: () => _animTool.PlayLoop(StunnedLoopAnim));
        }

        // 醒来直接回待机（骨骼没有 wake_up 过渡）
        private void OnStunEndedAsync(BE_EnemyStunEnded ended_)
        {
            if (ended_.Enemy != _boundEnemy || _boundCreature.IsDead) return;
            _animTool.PlayLoop(IdleLoopAnim);
        }
    }
}