using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>通用单位 Spine 动画器：玩家与敌人共用同一个类，差异全在动画名配置（留空即禁用该能力）；
    /// 攻击演出由伤害事件（Source==自己）驱动、出牌手势只属技能/能力卡、受击与获得格挡由 UI_Actor 点播；
    /// 死亡播完停可配时长后自我销毁（停留段不阻塞事件链）。</summary>
    public class UI_ActorAnimator : MonoBehaviour
    {
        [Header("动画名（留空即禁用该能力）")]
        [SerializeField] private string _standAnim;
        [SerializeField] private string _hitAnim;
        [SerializeField] private string _attackAnim;
        [SerializeField] private string _deathAnim;
        [SerializeField] private string _skillAnim;
        [SerializeField] private string _blockAnim;
        [SerializeField] private string _parryAnim;
        [SerializeField] private string _parryStandAnim;

        [Header("演出参数")]
        [Tooltip("格挡过渡动画只播前段的比例（动画资源过长时裁剪）")]
        [SerializeField] private float _parryClipEnd01 = 1f;
        [Tooltip("死亡动画播完后的停留时长，到时自我销毁")]
        [SerializeField] private float _deathHoldDuration = 2f;

        [SerializeField] private SkeletonGraphic _skeletonGraphic;

        /// <summary>绑定的域侧生物：事件按它过滤，回位与格挡站姿切换读它的格挡真值。</summary>
        private Creature _boundCreature;

        /// <summary>死亡停帧标记：death 播完后忽略一切动画请求。</summary>
        private bool _dead;

        /// <summary>是否处于格挡站姿：parry 过渡播过即视为已进入，避免重复获得格挡时重播过渡。</summary>
        private bool _inParryStance;

        #region 对外暴露的装配入口：绑定生物与事件，进标准站姿循环

        /// <summary>由 UI_Actor 在统一装配时转发调用：绑定生物、订阅演出事件并初始化骨骼。</summary>
        public void Bind(Creature creature_, BattleEventDispatcher eventDispatcher_)
        {
            _boundCreature = creature_;
            eventDispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            eventDispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            eventDispatcher_.Register<BE_ActorDied>(OnActorDiedAsync);

            if (!_skeletonGraphic.IsValid) _skeletonGraphic.Initialize(false);
            _skeletonGraphic.raycastTarget = false;
            if (!string.IsNullOrEmpty(_standAnim)) EnterLoop(_standAnim);
        }

        #endregion

        #region 受击与获得格挡的点播契约：UI_Actor 在刷 HUD 数字前 await，演完才落账

        public async UniTask PlayHitAsync(BE_DamageDealt dealt_)
        {
            if (_dead || _boundCreature.IsDead) return; // 致死伤不播 hit，death 由 BE_ActorDied 接管
            if (string.IsNullOrEmpty(_hitAnim)) return;
            if (dealt_.Amount <= dealt_.Absorbed) return; // 未破防：无真伤不播，保持格挡站姿
            await PlayOnceAsync(_hitAnim);
            await ReturnToStanceAsync(); // 破防时格挡必归零，正常回标准站姿
        }

        public async UniTask PlayBlockGainAsync(BE_BlockGained gained_)
        {
            if (_dead) return;
            if (!string.IsNullOrEmpty(_parryStandAnim))
            {
                await EnterParryStanceAsync();
                return;
            }
            if (string.IsNullOrEmpty(_blockAnim)) return;
            await PlayOnceAsync(_blockAnim);
            await ReturnToStanceAsync();
        }

        #endregion

        // 出牌手势：只属技能/能力卡（攻击卡静默——挥拳由伤害事件驱动；敌人未配技能动画名则整条静默）
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (_dead || string.IsNullOrEmpty(_skillAnim)) return;
            switch (played_.Card.Type)
            {
                case ECardType.Skill:
                case ECardType.Ability:
                    await PlayOnceAsync(_skillAnim);
                    await ReturnToStanceAsync();
                    break;
            }
        }

        // 自己造成伤害即攻击演出：多段伤害逐段挥拳，演完按格挡真值回位
        private async UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (_dead || dealt_.Source != _boundCreature) return;
            if (string.IsNullOrEmpty(_attackAnim)) return;
            await PlayOnceAsync(_attackAnim);
            await ReturnToStanceAsync();
        }

        // 回合开始清格挡：硬切回标准站姿（无过渡动画），未处于格挡站姿则无事发生
        private UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target != _boundCreature || _dead) return UniTask.CompletedTask;
            ExitParryStance();
            return UniTask.CompletedTask;
        }

        // 死亡：置停帧标记后播 death，播完停可配时长再自我销毁（停留段不阻塞事件链）
        private async UniTask OnActorDiedAsync(BE_ActorDied died_)
        {
            if (died_.Creature != _boundCreature || _dead) return;
            _dead = true;
            if (!string.IsNullOrEmpty(_deathAnim)) await PlayOnceAsync(_deathAnim);
            HoldThenDestroyAsync().Forget();
        }

        // 死亡停留：到时销毁整个单位视图；期间被外部销毁则安静收场
        private async UniTaskVoid HoldThenDestroyAsync()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(_deathHoldDuration));
            if (this == null) return;
            Destroy(gameObject);
        }

        // 回位统一入口：有格挡且配了格挡站姿则进站姿，否则硬切标准站姿
        private UniTask ReturnToStanceAsync()
        {
            if (_dead || _boundCreature.IsDead) return UniTask.CompletedTask;
            if (_boundCreature.Block > 0 && !string.IsNullOrEmpty(_parryStandAnim))
                return EnterParryStanceAsync();
            ExitParryStance();
            return UniTask.CompletedTask;
        }

        // 进格挡站姿：先播一次 parry 过渡（可裁剪），已在站姿则仅保持循环
        private async UniTask EnterParryStanceAsync()
        {
            if (_dead) return;
            if (_inParryStance)
            {
                EnterLoop(_parryStandAnim);
                return;
            }
            _inParryStance = true; // 进入即置位，避免过渡播放期间被重入再播一遍
            if (!string.IsNullOrEmpty(_parryAnim))
            {
                await PlayOnceAsync(_parryAnim, _parryClipEnd01);
                if (_dead) return; // 过渡期间若已死亡，交由 death 演出去，勿覆盖
            }
            EnterLoop(_parryStandAnim);
        }

        // 退出格挡态：清标志并硬切回标准站姿，未处于站姿则不动
        private void ExitParryStance()
        {
            if (!_inParryStance) return;
            _inParryStance = false;
            if (!string.IsNullOrEmpty(_standAnim)) EnterLoop(_standAnim);
        }

        private void EnterLoop(string animationName_)
        {
            if (!_skeletonGraphic.IsValid) _skeletonGraphic.Initialize(false);
            _skeletonGraphic.AnimationState.SetAnimation(0, animationName_, true);
        }

        private async UniTask PlayOnceAsync(string animationName_, float clipEnd01 = 1f)
        {
            if (!_skeletonGraphic.IsValid) _skeletonGraphic.Initialize(false);
            TrackEntry entry = _skeletonGraphic.AnimationState.SetAnimation(0, animationName_, false);
            if (clipEnd01 < 1f) // 仅播放动画前 clipEnd01 比例，到时即完成并停在裁剪帧
                entry.TrackEnd = entry.Animation.Duration * Mathf.Clamp01(clipEnd01);
            UniTaskCompletionSource finished = new();
            entry.Complete += _ => finished.TrySetResult();
            entry.End += _ => finished.TrySetResult();
            await finished.Task;
        }
    }
}
