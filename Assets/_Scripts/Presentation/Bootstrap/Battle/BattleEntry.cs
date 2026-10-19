using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段表现层入口：按组合根转发的视图预制体生成玩家与敌人视图并值传递装配，再启动战斗循环，
    /// 是"这场战斗从哪里开始"的唯一落点；视图一律运行时生成，场景里没有预摆的单位实例。
    /// 运行时生成的预制体进不了容器，故生成后补一次 InjectGameObject——视图上的 [Inject] 字段（如气泡宿主）由此拿到。</summary>
    public class BattleEntry : IStartable
    {
        private const int PlayerSlot = 0; // 0 号槽位恒为玩家（1~3 号为敌人，1 最靠前）

        private readonly BattleLoop _loop;
        private readonly BattleContext _battleContext;
        private readonly BattleEventDispatcher _dispatcher;
        private readonly UI_ActorRoot _actorRoot;
        private readonly GameObject _playerViewPrefab;
        private readonly List<GameObject> _enemyViewPrefabs;
        private readonly IObjectResolver _resolver;

        public BattleEntry(BattleLoop loop_, BattleContext battleContext_, BattleEventDispatcher dispatcher_,
            UI_ActorRoot actorRoot_, [Key("Player")] GameObject playerViewPrefab_, List<GameObject> enemyViewPrefabs_,
            IObjectResolver resolver_)
        {
            _loop = loop_;
            _battleContext = battleContext_;
            _dispatcher = dispatcher_;
            _actorRoot = actorRoot_;
            _playerViewPrefab = playerViewPrefab_;
            _enemyViewPrefabs = enemyViewPrefabs_;
            _resolver = resolver_;
        }

        public void Start()
        {
            SpawnPlayerView();
            SpawnEnemyViews();
            _loop.Start();
        }

        #region 生成玩家视图：实例化到玩家槽位，枢轴（脚底中点）钉到槽位点后值传递装配

        private void SpawnPlayerView()
        {
            GameObject player = Object.Instantiate(_playerViewPrefab, _actorRoot.GetSlotTransform(PlayerSlot));
            _resolver.InjectGameObject(player); // 先补注入再 Bind：Bind 会立刻建初始状态槽位，槽位要用到注入好的气泡宿主
            if (player.TryGetComponent(out UI_Player playerView))
                playerView.Bind(_dispatcher, _battleContext.Player);
        }

        #endregion

        #region 生成敌人视图：按配置资产各自的预制体生成（下标即敌人下标），缺预制体或缺槽位点报错并跳过该项

        private void SpawnEnemyViews()
        {
            for (int i = 0; i < _battleContext.Enemies.Count; i++)
            {
                GameObject enemy =
                    Object.Instantiate(_enemyViewPrefabs[i],
                        _actorRoot.GetSlotTransform(_battleContext.Enemies[i].Slot));
                _resolver.InjectGameObject(enemy); // 先补注入再 Bind：Bind 会立刻展示初始意图，图标要用到注入好的气泡宿主
                // 意图与移动是敌人的固有能力，一次装配到位；出生摆位由 Bind 直接钉到槽位点
                if (enemy.TryGetComponent(out UI_Enemy enemyView))
                    enemyView.Bind(_dispatcher, _battleContext.Enemies[i]);
            }
        }

        #endregion
    }
}