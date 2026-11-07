using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>仪式兽视图：双阶段演出——阶段一攻击 plow→plow_end 两段连播算一次攻击、阶段二用公约 attack；
    /// 给自己施加正面增益播 shrill；横冲直撞触发的击晕播 stun→stun_loop，醒来播 wake_up（位移撞晕不发事件、不播）。</summary>
    public sealed class UI_CeremonialBeast : UI_Enemy
    {
        private const string PlowAnim = "plow";
        private const string PlowEndAnim = "plow_end";
        private const string ShrillAnim = "shrill";
        private const string StunAnim = "stun";
        private const string StunLoopAnim = "stun_loop";
        private const string WakeUpAnim = "wake_up";

        #region 装配钩子：校验专属动画名并订阅击晕演出事件

        protected override void OnEnemyBound(BattleEventDispatcher eventDispatcher_)
        {
            _animTool.VerifyAnim(PlowAnim, PlowEndAnim, ShrillAnim, StunAnim, StunLoopAnim, WakeUpAnim);
            eventDispatcher_.Register<BE_EnemyStunned>(OnStunnedAsync);
            eventDispatcher_.Register<BE_EnemyStunEnded>(OnStunEndedAsync);
        }

        #endregion

        #region 演出重写

        // 我打人：阶段一 plow→plow_end 两段连播算一次攻击；阶段二沿用公约 attack
        protected override async UniTask OnDealtDamageAsync(BE_DamageDealt dealt_)
        {
            if (_boundCreature.IsDead || dealt_.Source != _boundCreature || _boundEnemy.IsPhaseTwo)
            {
                await base.OnDealtDamageAsync(dealt_);
                return;
            }

            // plow 播完接 plow_end，两段都自然播完才回待机；期间被受击接棒则整体作废（票号保证）
            await _animTool.PlayOnceAsync(PlowAnim, onFinished_: () =>
                _animTool.PlayOnceAsync(PlowEndAnim, onFinished_: () => _animTool.PlayLoop(IdleLoopAnim)).Forget());
        }

        // 我挨打：击晕期间被打，hurt 播完回击晕循环而不是待机；其余沿用基类
        protected override async UniTask OnDamagedAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != _boundCreature || !_boundEnemy.IsStunned)
            {
                await base.OnDamagedAsync(dealt_);
                return;
            }

            UniTask wait = _animTool.PlayOnceAsync(HitAnim, onFinished_: () => _animTool.PlayLoop(StunLoopAnim));
            if (_blockView != null) _blockView.SetBlock(dealt_.Target.Block);
            if (_healthBar != null) _healthBar.SetHealth(dealt_.Target.CurrentHp);
            await wait;
        }

        // 施加状态：槽位刷新沿用基类；给自己施加正面增益时播 shrill（事件链等它放行）
        protected override async UniTask OnPerkApplied(BE_PerkApplied applied_)
        {
            await base.OnPerkApplied(applied_);
            if (_boundCreature.IsDead || applied_.Target != _boundCreature || applied_.Perk.Polarity != EPerkPolarity.Buff)
                return;
            await _animTool.PlayOnceAsync(ShrillAnim, onFinished_: () => _animTool.PlayLoop(IdleLoopAnim));
        }

        #endregion

        #region 击晕链：横冲直撞触发时播 stun 后进击晕待机循环，醒来播 wake_up 回待机

        private async UniTask OnStunnedAsync(BE_EnemyStunned stunned_)
        {
            if (stunned_.Enemy != _boundEnemy || _boundCreature.IsDead) return;
            await _animTool.PlayOnceAsync(StunAnim, onFinished_: () => _animTool.PlayLoop(StunLoopAnim));
        }

        private async UniTask OnStunEndedAsync(BE_EnemyStunEnded ended_)
        {
            if (ended_.Enemy != _boundEnemy || _boundCreature.IsDead) return;
            await _animTool.PlayOnceAsync(WakeUpAnim, playRatio_: 1f, onFinished_: () => _animTool.PlayLoop(IdleLoopAnim));
        }

        #endregion
    }
}
