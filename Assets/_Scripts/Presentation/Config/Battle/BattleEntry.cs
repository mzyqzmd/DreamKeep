using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段表现层入口：先按配置生成敌人视图并值传递装配，再启动战斗循环，是"这场战斗从哪里开始"的唯一落点。</summary>
    public class BattleEntry : IStartable
    {
        private readonly BattleLoop _loop;
        private readonly BattleContext _battleContext;
        private readonly BattleEventDispatcher _dispatcher;
        private readonly UI_EnemyArea _enemyArea;
        private readonly List<GameObject> _enemyViewPrefabs;

        public BattleEntry(BattleLoop loop_, BattleContext battleContext_, BattleEventDispatcher dispatcher_,
            UI_EnemyArea enemyArea_, List<GameObject> enemyViewPrefabs_)
        {
            _loop = loop_;
            _battleContext = battleContext_;
            _dispatcher = dispatcher_;
            _enemyArea = enemyArea_;
            _enemyViewPrefabs = enemyViewPrefabs_;
        }

        public void Start()
        {
            SpawnEnemyViews();
            _loop.Start().Forget();
        }

        #region 生成敌人视图：预制体列表与敌人配置平行索引，错位报错并跳过多余项

        private void SpawnEnemyViews()
        {
            List<Enemy> enemies = _battleContext.Enemies;
            if (_enemyViewPrefabs.Count != enemies.Count)
                Debug.LogError($"敌人视图预制体数 {_enemyViewPrefabs.Count} 与敌人配置数 {enemies.Count} 不一致，多余的将被跳过");

            int count = Mathf.Min(_enemyViewPrefabs.Count, enemies.Count);
            for (int i = 0; i < count; i++)
            {
                GameObject instance =
                    Object.Instantiate(_enemyViewPrefabs[i], _enemyArea.GetSlotTransform(2 - i).transform);
                if (!instance.TryGetComponent(out UI_Enemy enemyView))
                {
                    Debug.LogError($"敌人视图预制体 {_enemyViewPrefabs[i].name} 根上缺少 UI_Enemy 子类组件");
                    Object.Destroy(instance);
                    continue;
                }

                enemyView.Construct(_battleContext, _dispatcher, enemies[i], _enemyArea);
            }
        }

        #endregion
    }
}