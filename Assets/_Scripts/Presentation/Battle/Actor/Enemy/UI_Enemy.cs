using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>敌人视图基类：战斗单位共享表现（UI_Actor）的敌人侧装配点——绑定域敌人、注册事件、按槽位落位与移动补间、
    /// 头顶意图预告与 Spine 动画（stand 循环 / 受击 hit / 攻击 attack / 死亡 death）；具体敌人继承并挂各自预制体。</summary>
    public abstract class UI_Enemy : UI_Actor
    {
        private const string AnimStand = "stand";
        private const string AnimHit = "hit";
        private const string AnimAttack = "attack";
        private const string AnimDeath = "death";

        [SerializeField] private SkeletonGraphic _skeletonGraphic;
        [SerializeField] private UI_EnemyIntentGroup _intentGroup;

        [Header("移动")] [SerializeField] private float _moveDuration = 0.3f;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        /// <summary>绑定的域敌人：事件按它过滤，槽位与展示意图真值都从它读。</summary>
        protected Enemy _boundEnemy;

        private UI_EnemyArea _area;
        private Tween _moveTween;
        private bool _dead;

        #region 对子类暴露的装配入口：绑定敌人、注册事件并按槽位落位

        /// <summary>显式值传递装配（不走 [Inject]：容器不知道"是第几个敌人"），由生成侧在 Instantiate 后调用。</summary>
        public virtual void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_, Enemy enemy_, UI_EnemyArea area_)
        {
            _boundEnemy = enemy_;
            _boundCreature = enemy_.Creature;
            _area = area_;
            base.Construct(battleContext_, eventDispatcher_);

            eventDispatcher_.Register<BE_EnemyIntentChanged>(OnIntentChangedAsync);
            eventDispatcher_.Register<BE_EnemyMoved>(OnEnemyMovedAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnSelfDamageDealtAsync);
            eventDispatcher_.Register<BE_ActorDied>(OnActorDiedAsync);

            _intentGroup.Show(_boundEnemy.PreviewIntent);
            EnterStandLoop();
            PlaceAtSlot(_boundEnemy.Slot);
        }

        #endregion

        // 受击：hit 播完回标准站姿，致死伤跳过（death 由 BE_ActorDied 接管）
        protected override async UniTask OnDamageDealtAnimAsync(BE_DamageDealt dealt_)
        {
            if (_dead || _boundCreature.IsDead) return;
            await PlayOnceAsync(AnimHit);
            EnterLoop(AnimStand);
        }

        // 意图预告：广播的下一条展示意图刷新头顶图标组
        private UniTask OnIntentChangedAsync(BE_EnemyIntentChanged changed_)
        {
            if (changed_.Enemy != _boundEnemy) return UniTask.CompletedTask;
            _intentGroup.Show(changed_.Intent);
            return UniTask.CompletedTask;
        }

        // 移动：滑向目标槽位点，事件链阻塞至到位（逐格推进与打完回位都按序演出）
        private async UniTask OnEnemyMovedAsync(BE_EnemyMoved moved_)
        {
            if (_dead || moved_.Enemy != _boundEnemy) return;
            Transform slot = _area.GetSlotTransform(moved_.ToSlot);
            if (slot == null) return;
            await PlayMoveAsync(slot.position);
        }

        // 敌人自己的攻击结算：BE_DamageDealt 是结算后事件（先落账后挥拳），播完回站姿
        private async UniTask OnSelfDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (_dead || dealt_.Source != _boundCreature) return;
            await PlayOnceAsync(AnimAttack);
            if (!_boundCreature.IsDead) EnterLoop(AnimStand);
        }

        // 死亡：置停帧标记后播 death，播完停在末帧并收尸隐藏
        private async UniTask OnActorDiedAsync(BE_ActorDied died_)
        {
            if (died_.Creature != _boundCreature || _dead) return;
            _dead = true;
            _moveTween?.Kill();
            await PlayOnceAsync(AnimDeath);
            gameObject.SetActive(false);
        }

        // 出生摆位：不补间直接钉到当前槽位点
        private void PlaceAtSlot(int slot_)
        {
            Transform point = _area.GetSlotTransform(slot_);
            if (point == null) return;
            _moveTween?.Kill();
            ((RectTransform)transform).position = point.position;
        }

        // 位移补间：杀旧重开，播完或被杀都放行等待方
        private async UniTask PlayMoveAsync(Vector3 worldPosition_)
        {
            _moveTween?.Kill();
            UniTaskCompletionSource finished = new();
            _moveTween = ((RectTransform)transform).DOMove(worldPosition_, _moveDuration)
                .SetEase(_moveEase)
                .OnComplete(() => finished.TrySetResult())
                .OnKill(() => finished.TrySetResult());
            await finished.Task;
        }

        private void EnterStandLoop()
        {
            if (!_skeletonGraphic.IsValid) _skeletonGraphic.Initialize(false);
            _skeletonGraphic.raycastTarget = false;
            _skeletonGraphic.AnimationState.SetAnimation(0, AnimStand, true);
        }

        private void EnterLoop(string animationName_)
        {
            if (!_skeletonGraphic.IsValid) _skeletonGraphic.Initialize(false);
            _skeletonGraphic.AnimationState.SetAnimation(0, animationName_, true);
        }

        private async UniTask PlayOnceAsync(string animationName_)
        {
            if (!_skeletonGraphic.IsValid) _skeletonGraphic.Initialize(false);
            TrackEntry entry = _skeletonGraphic.AnimationState.SetAnimation(0, animationName_, false);
            UniTaskCompletionSource finished = new();
            entry.Complete += _ => finished.TrySetResult();
            entry.End += _ => finished.TrySetResult();
            await finished.Task;
        }

        private void OnDestroy() => _moveTween?.Kill();
    }
}
