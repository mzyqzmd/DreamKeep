using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>敌人单位视图基类：在共享表现（面板、演出、共享事件）之上，加上敌人固有的意图预告与纵深槽位移动。
    /// 动画名：四条公约由 UI_Actor 写死，本单位专属动画由具体敌人类声明。</summary>
    public abstract class UI_Enemy : UI_Actor
    {
        [Header("意图预告")] [SerializeField] private UI_EnemyIntentGroup _intentGroup;
        [Header("槽位移动")] [SerializeField] private float _moveDuration = 0.3f;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        private Enemy _boundEnemy;

        private Tween _moveTween;

        // 销毁：先收掉自己的移动补间，再转调基类注销登记
        protected override void OnDestroy()
        {
            _moveTween?.Kill();
            base.OnDestroy();
        }

        #region 装配入口：由生成侧在 Instantiate 后注入装配

        /// <summary>统一装配：绑敌人 → 转调基类（演出、面板与共享事件）→ 订阅意图与移动 → 展示当前预告意图 → 钉到出生槽位。</summary>
        public void Bind(BattleEventDispatcher eventDispatcher_, Enemy enemy_)
        {
            _boundEnemy = enemy_;
            base.Bind(eventDispatcher_, enemy_.Creature);
            eventDispatcher_.Register<BE_EnemyIntentChanged>(OnIntentChangedAsync);
            eventDispatcher_.Register<BE_EnemyMoved>(OnEnemyMovedAsync);
            _intentGroup.Show(_boundEnemy.PreviewIntent);

            // 出生摆位：不补间，直接钉到当前槽位点
            _moveTween?.Kill();
            transform.position = _actorRoot.GetSlotTransform(_boundEnemy.Slot).position;
        }

        #endregion

        // 意图变动：刷新头顶图标组
        private UniTask OnIntentChangedAsync(BE_EnemyIntentChanged changed_)
        {
            if (changed_.Enemy != _boundEnemy) return UniTask.CompletedTask;
            _intentGroup.Show(changed_.Intent);
            return UniTask.CompletedTask;
        }

        // 移动：滑向目标槽位点，事件链阻塞到到位（逐格推进与打完回位都按序演出）
        private async UniTask OnEnemyMovedAsync(BE_EnemyMoved moved_)
        {
            if (moved_.Enemy != _boundEnemy) return;
            Transform slot = _actorRoot.GetSlotTransform(moved_.ToSlot);
            if (slot == null) return;

            // 杀旧补间再开新的；播完或被杀都放行等待方
            _moveTween?.Kill();
            UniTaskCompletionSource finished = new();
            _moveTween = ((RectTransform)transform).DOMove(slot.position, _moveDuration)
                .SetEase(_moveEase)
                .OnComplete(() => finished.TrySetResult())
                .OnKill(() => finished.TrySetResult());
            await finished.Task;
        }
    }
}