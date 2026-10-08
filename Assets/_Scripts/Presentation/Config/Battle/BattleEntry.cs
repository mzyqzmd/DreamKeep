using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段表现层入口：按组合根转发的视图预制体生成玩家与敌人视图并值传递装配，再启动战斗循环，
    /// 是"这场战斗从哪里开始"的唯一落点；视图一律运行时生成，场景里没有预摆的单位实例。</summary>
    public class BattleEntry : IStartable
    {
        private const int PlayerSlot = 0; // 0 号槽位恒为玩家（1~3 号为敌人，1 最靠前）

        private readonly BattleLoop _loop;
        private readonly BattleContext _battleContext;
        private readonly BattleEventDispatcher _dispatcher;
        private readonly UI_ActorRoot _actorRoot;
        private readonly GameObject _playerViewPrefab;
        private readonly List<GameObject> _enemyViewPrefabs;

        public BattleEntry(BattleLoop loop_, BattleContext battleContext_, BattleEventDispatcher dispatcher_,
            UI_ActorRoot actorRoot_, GameObject playerViewPrefab_, List<GameObject> enemyViewPrefabs_)
        {
            _loop = loop_;
            _battleContext = battleContext_;
            _dispatcher = dispatcher_;
            _actorRoot = actorRoot_;
            _playerViewPrefab = playerViewPrefab_;
            _enemyViewPrefabs = enemyViewPrefabs_;
        }

        public void Start()
        {
            SpawnPlayerView();
            SpawnEnemyViews();
            _loop.Start().Forget();
        }

        #region 生成玩家视图：实例化到玩家槽位，枢轴（脚底中点）钉到槽位点后值传递装配

        private void SpawnPlayerView()
        {
            if (!_playerViewPrefab)
            {
                Debug.LogError("玩家配置未接视图预制体，跳过玩家视图生成");
                return;
            }

            Transform slot = _actorRoot.GetSlotTransform(PlayerSlot);
            if (!slot)
            {
                Debug.LogError($"玩家槽位点 {PlayerSlot} 号未接线，跳过玩家视图生成");
                return;
            }

            GameObject instance = Object.Instantiate(_playerViewPrefab, slot);
            if (!instance.TryGetComponent(out UI_Player playerView))
            {
                Debug.LogError($"玩家视图预制体 {_playerViewPrefab.name} 根上缺少 UI_Player 组件");
                Object.Destroy(instance);
                return;
            }

            // 位置真源只有槽位点：prefab 的本地偏移不参与，枢轴即脚底中点
            ((RectTransform)instance.transform).position = slot.position;
            playerView.Bind(_dispatcher, _battleContext.Player);
        }

        #endregion

        #region 生成敌人视图：按配置资产各自的预制体生成（下标即敌人下标），缺预制体或缺槽位点报错并跳过该项

        private void SpawnEnemyViews()
        {
            List<Enemy> enemies = _battleContext.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                GameObject prefab = i < _enemyViewPrefabs.Count ? _enemyViewPrefabs[i] : null;
                if (!prefab)
                {
                    Debug.LogError($"敌人 {enemies[i].Name} 的配置未接视图预制体，跳过该敌人视图");
                    continue;
                }

                Transform slot = _actorRoot.GetSlotTransform(enemies[i].Slot);
                if (!slot)
                {
                    Debug.LogError($"敌人 {enemies[i].Name} 的槽位点 {enemies[i].Slot} 号未接线，跳过该敌人视图");
                    continue;
                }

                GameObject instance = Object.Instantiate(prefab, slot);
                if (!instance.TryGetComponent(out UI_Enemy enemyView))
                {
                    Debug.LogError($"敌人视图预制体 {prefab.name} 根上缺少 UI_Enemy 派生组件");
                    Object.Destroy(instance);
                    continue;
                }

                // 意图与移动是敌人的固有能力，一次装配到位；出生摆位由 Bind 直接钉到槽位点
                enemyView.Bind(_dispatcher, enemies[i]);
            }
        }

        #endregion
    }
}
