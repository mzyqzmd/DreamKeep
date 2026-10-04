using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>敌人单位视图基类：在共享单位表现（面板与共享事件）之上，天然具备意图预告与纵深槽位移动——
    /// 类型即保证，不再靠"有没有挂组件"去探测；每种敌人各派生一个具体类，差异化行为在子类里覆写。</summary>
    public abstract class UI_Enemy : UI_Actor
    {
        [Header("意图预告")] [SerializeField] private UI_EnemyIntentGroup _intentGroup;

        [Header("槽位移动")] [SerializeField] private float _moveDuration = 0.3f;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        private Enemy _boundEnemy;
        private UI_ActorRoot _root;
        private Tween _moveTween;

        #region 对外暴露的装配入口：由生成侧在 Instantiate 后注入装配

        /// <summary>统一装配：绑敌人 → 转调基类（面板与共享事件）→ 订阅意图与移动 → 展示当前预告意图 → 钉到出生槽位。</summary>
        public void Bind(BattleEventDispatcher eventDispatcher_, Enemy enemy_, UI_ActorRoot root_, ActorViewRegistry registry_)
        {
            _boundEnemy = enemy_;
            _root = root_;
            base.Bind(eventDispatcher_, enemy_.Creature, registry_);
            eventDispatcher_.Register<BE_EnemyIntentChanged>(OnIntentChangedAsync);
            eventDispatcher_.Register<BE_EnemyMoved>(OnEnemyMovedAsync);
            _intentGroup.Show(_boundEnemy.PreviewIntent);
            PlaceAtSlot(_boundEnemy.Slot);
        }

        #endregion

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
            if (moved_.Enemy != _boundEnemy || this == null) return;
            Transform slot = _root.GetSlotTransform(moved_.ToSlot);
            if (slot == null) return;
            await PlayMoveAsync(slot.position);
        }

        // 出生摆位：不补间直接钉到当前槽位点
        private void PlaceAtSlot(int slot_)
        {
            Transform point = _root.GetSlotTransform(slot_);
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

        // 销毁：先收掉自己的移动补间，再转调基类注销视图登记
        protected override void OnDestroy()
        {
            _moveTween?.Kill();
            base.OnDestroy();
        }
    }
}
