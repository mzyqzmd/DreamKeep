using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>玩家视图：战斗单位共享表现（UI_Actor）的玩家侧装配点——绑定玩家生物数据、注册域事件；
    /// 同时承载玩家 Spine 动画：全部动画经事件链阻塞式顺序演绎（播完才放行下一段），演完才刷 HUD 数字。</summary>
    public class UI_Player : UI_Actor
    {
        private const string AnimStand = "stand";
        private const string AnimParryStand = "parry_stand";
        private const string AnimParry = "parry";
        private const string AnimHurt = "hurt";
        private const string AnimDeath = "death";
        private const string AnimAttack = "attack_pillow";
        private const string AnimSkill = "skill";

        [SerializeField] private SkeletonGraphic _skeletonGraphic;

        /// <summary>死亡停帧标记：death 播完停在末帧不回位，此后一切动画请求一律忽略。</summary>
        private bool _dead;

        /// <summary>是否处于格挡站姿：parry 过渡播过即视为已进入，避免重复获得格挡/出牌回位时重播 parry 过渡。</summary>
        private bool _inParryStance;

        public override void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _boundCreature = battleContext_.Player.Creature;
            base.Construct(battleContext_, eventDispatcher_);
            // 补订阅基类不消费的三事件；BE_CardPlayed 在 HandPlayFlow（卡牌离场演出）之后分派，天然"卡先飞走再挥拳"
            eventDispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            eventDispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            eventDispatcher_.Register<BE_ActorDied>(OnActorDiedAsync);
        }

        #region 动画钩子覆写：基类在刷 HUD 数字前 await，演完才落账

        protected override UniTask OnBlockGainedAnimAsync(BE_BlockGained gained_) =>
            EnterParryStanceAsync();

        protected override async UniTask OnDamageDealtAnimAsync(BE_DamageDealt dealt_)
        {
            if (_dead || _boundCreature.IsDead) return; // 致死伤不播 hurt，death 由 BE_ActorDied 接管
            if (dealt_.Amount <= dealt_.Absorbed) return; // 伤害被格挡全额吸收：未破防，保持格挡站姿不播
            await PlayOnceAsync(AnimHurt);
            await ReturnToStanceAsync(); // 破防时 Block 必归零，正常回标准站姿
        }

        #endregion

        // 出牌：按卡型分流动作，播完按当下格挡真值回位；诅咒等类型不播
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (_dead) return;
            switch (played_.Card.Type)
            {
                case ECardType.Attack:
                    await PlayOnceAsync(AnimAttack);
                    break;
                case ECardType.Skill:
                case ECardType.Ability:
                    await PlayOnceAsync(AnimSkill);
                    break;
                default:
                    return;
            }

            await ReturnToStanceAsync();
        }

        // 回合开始清格挡：硬切回标准站姿（无过渡动画），瞬时完成不阻塞事件链
        private UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target != _boundCreature || _dead) return UniTask.CompletedTask;
            ExitParryStance();
            return UniTask.CompletedTask;
        }

        // 死亡：置停帧标记后播 death，播完停在末帧
        private async UniTask OnActorDiedAsync(BE_ActorDied died_)
        {
            if (died_.Creature != _boundCreature || _dead) return;
            _dead = true;
            await PlayOnceAsync(AnimDeath);
        }

        // 回位统一入口：进格挡站姿必须先播一次 parry 过渡，标准站姿直接硬切
        private UniTask ReturnToStanceAsync()
        {
            if (_boundCreature.Block > 0)
            {
                return EnterParryStanceAsync();
            }
            else
            {
                if (_dead) return UniTask.CompletedTask;
                ExitParryStance();
                return UniTask.CompletedTask;
            }
        }

        private async UniTask EnterParryStanceAsync()
        {
            if (_dead) return;
            if (_inParryStance)        // 已在格挡站姿：跳过 parry 过渡，仅保持循环
            {
                EnterLoop(AnimParryStand);
                return;
            }
            _inParryStance = true;     // 进入即置位，避免过渡播放期间被重入再播一遍
            // NOTE:这个是动画资源太长了，只需要播前一部分，后一部分裁剪掉
            await PlayOnceAsync(AnimParry, 0.5f);   
            if (_dead) return;         // 过渡期间若已死亡，交由 death 演出去，勿覆盖
            EnterLoop(AnimParryStand);
        }

        // 退出格挡态：清理标志并硬切回标准站姿
        private void ExitParryStance()
        {
            _inParryStance = false;
            EnterLoop(AnimStand);
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
            if (clipEnd01 < 1f)        // 仅播放动画前 clipEnd01 比例，到时即完成并停在裁剪帧
                entry.TrackEnd = entry.Animation.Duration * Mathf.Clamp01(clipEnd01);
            UniTaskCompletionSource finished = new();
            entry.Complete += _ => finished.TrySetResult();
            entry.End += _ => finished.TrySetResult();
            await finished.Task;
        }
    }
}