using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using VContainer;

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
        private BattleQueryDispatcher _queryDispatcher;

        private Tween _moveTween;

        // 意图数值的逐帧对账：攻击意图的伤害随修正实时刷新（等值短路在图标内，未变零写入）；未绑定或已死亡早退
        private void Update()
        {
            if (_boundCreature == null || _boundCreature.IsDead) return;
            RefreshIntentPreviews();
        }

        // 销毁：先收掉自己的移动补间，再转调基类注销登记
        protected override void OnDestroy()
        {
            _moveTween?.Kill();
            base.OnDestroy();
        }

        #region 装配入口：生成侧注入上行门面、Instantiate 后值传递装配

        /// <summary>注入查询门面：意图数值预览的查询出口（生成侧 InjectGameObject 先于 Bind 调用）。</summary>
        [Inject]
        public void Construct(BattleQueryDispatcher queryDispatcher_) => _queryDispatcher = queryDispatcher_;

        /// <summary>统一装配：绑敌人 → 转调基类（演出、面板与共享事件）→ 订阅意图与移动 → 展示当前预告意图 → 钉到出生槽位。</summary>
        public void Bind(BattleEventDispatcher eventDispatcher_, Enemy enemy_)
        {
            _boundEnemy = enemy_;
            base.Bind(eventDispatcher_, enemy_.Creature);
            eventDispatcher_.Register<BE_EnemyIntentChanged>(OnIntentChangedAsync);
            eventDispatcher_.Register<BE_EnemyMoved>(OnEnemyMovedAsync);
            _intentGroup.Show(_boundEnemy.PreviewIntent);
            RefreshIntentPreviews(); // 新图标立即带修正值，不闪基础值

            // 出生摆位：不补间，直接钉到当前槽位点
            _moveTween?.Kill();
            transform.position = _actorRoot.GetSlotTransform(_boundEnemy.Slot).position;
        }

        #endregion

        #region 敌人重写演绎

        protected override UniTask OnActorDiedAsync(BE_ActorDied died_)
        {
            if (died_.Creature != _boundCreature) return UniTask.CompletedTask;
            _intentGroup.Show(null); // 死亡即刻收起头顶意图，再交给基类播死亡演出
            return base.OnActorDiedAsync(died_);
        }

        #endregion

        #region 敌人独有演绎

        // 意图变动：刷新头顶图标组（新图标立即按当前修正值重写一遍）
        private UniTask OnIntentChangedAsync(BE_EnemyIntentChanged changed_)
        {
            if (changed_.Enemy != _boundEnemy) return UniTask.CompletedTask;
            _intentGroup.Show(changed_.Intent);
            RefreshIntentPreviews();
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
            _moveTween = transform.DOMove(slot.position, _moveDuration).SetEase(_moveEase);
            await _moveTween.WaitAsync();
        }

        #endregion

        // 意图预览对账：来源=敌人自己（力量/虚弱吃敌人侧，目标为玩家的易伤吃玩家侧，均由门面内定）
        private void RefreshIntentPreviews() =>
            _intentGroup.RefreshPreviews(_queryDispatcher, _boundCreature);
    }
}
