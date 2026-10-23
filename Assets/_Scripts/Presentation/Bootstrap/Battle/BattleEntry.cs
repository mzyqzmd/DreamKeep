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
    /// 运行时生成的预制体进不了容器，故生成后补一次 InjectGameObject——视图上的 [Inject] 字段（如气泡宿主）由此拿到。
    /// 预制体、槽位点与视图组件三类漏接线一律报错并跳过该项：漏接线是装配期错误，不该以崩溃或"静默生成到场景根"的形式出现。</summary>
    public class BattleEntry : IStartable
    {
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
            if (!_playerViewPrefab)
            {
                Debug.LogError($"[{nameof(BattleEntry)}] 玩家视图预制体未接线（SO_Player.ViewPrefab），本次不生成玩家视图");
                return;
            }

            Transform slot = _actorRoot.GetSlotTransform(BattleSlots.Player);
            if (!slot)
            {
                Debug.LogError($"[{nameof(BattleEntry)}] 战场单位区缺少玩家槽位点，玩家视图无处摆位，本次不生成");
                return;
            }

            GameObject player = Object.Instantiate(_playerViewPrefab, slot);
            _resolver.InjectGameObject(player); // 先补注入再 Bind：Bind 会立刻建初始状态槽位，槽位要用到注入好的气泡宿主
            if (!player.TryGetComponent(out UI_Player playerView))
            {
                Debug.LogError(
                    $"[{nameof(BattleEntry)}] 玩家预制体 {_playerViewPrefab.name} 根节点缺少 {nameof(UI_Player)}，已销毁实例并跳过");
                Object.Destroy(player);
                return;
            }

            playerView.Bind(_dispatcher, _battleContext.Player);
        }

        #endregion

        #region 生成敌人视图：按配置资产各自的预制体生成（下标即敌人下标），缺预制体或缺槽位点报错并跳过该项

        private void SpawnEnemyViews()
        {
            for (int i = 0; i < _battleContext.Enemies.Count; i++)
            {
                Enemy enemy = _battleContext.Enemies[i];
                GameObject prefab = i < _enemyViewPrefabs.Count ? _enemyViewPrefabs[i] : null;
                if (!prefab)
                {
                    Debug.LogError($"[{nameof(BattleEntry)}] 敌人 {enemy.Name} 未配置视图预制体（SO_Enemy.ViewPrefab），本次跳过");
                    continue;
                }

                Transform slot = _actorRoot.GetSlotTransform(enemy.Slot);
                if (!slot)
                {
                    Debug.LogError($"[{nameof(BattleEntry)}] 敌人 {enemy.Name} 所在槽位点未接线，本次跳过");
                    continue;
                }

                GameObject instance = Object.Instantiate(prefab, slot);
                _resolver.InjectGameObject(instance); // 先补注入再 Bind：Bind 会立刻展示初始意图，图标要用到注入好的气泡宿主
                // 意图与移动是敌人的固有能力，一次装配到位；出生摆位由 Bind 直接钉到槽位点
                if (!instance.TryGetComponent(out UI_Enemy enemyView))
                {
                    Debug.LogError(
                        $"[{nameof(BattleEntry)}] 敌人预制体 {prefab.name} 根节点缺少 {nameof(UI_Enemy)}，已销毁实例并跳过");
                    Object.Destroy(instance);
                    continue;
                }

                enemyView.Bind(_dispatcher, enemy);
            }
        }

        #endregion
    }
}
