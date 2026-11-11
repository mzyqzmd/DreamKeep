using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameBattle;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段表现层入口：按组合根转发的视图预制体生成玩家与敌人视图并值传递装配，再启动战斗与段收尾两条循环，
    /// 是"这场战斗从哪里开始"的唯一落点；视图一律运行时生成，场景里没有预摆的单位实例。
    /// 收尾不在此裁决：订阅段收尾事实——奖励面板弹出、完成留段、退出卸段——转成整局编排者的动作与命令意图。
    /// 运行时生成的预制体进不了容器，故生成后补一次 InjectGameObject——视图上的 [Inject] 字段（如气泡宿主）由此拿到。
    /// 预制体、槽位点与视图组件三类漏接线一律报错并跳过该项：漏接线是装配期错误，不该以崩溃或"静默生成到场景根"的形式出现。</summary>
    public class BattleEntry : IStartable
    {
        private readonly BattleLoop _loop;
        private readonly BattleContext _battleContext;
        private readonly BattleEventDispatcher _dispatcher;
        private readonly BattleSegmentLoop _segmentLoop;
        private readonly BattleSegmentEventDispatcher _segmentEvents;
        private readonly BattleSegmentCommandDispatcher _segmentCommands;
        private readonly SegmentSceneManager _segmentManager;
        private readonly PopPanelFactory _popFactory;
        private readonly UI_ResultPanel _resultPanel;
        private readonly UI_ActorRoot _actorRoot;
        private readonly BattleStage _stage;
        private readonly RunState _run;
        private readonly RunCommandDispatcher _commands;
        private readonly RunProgressService _progress;
        private readonly GameObject _playerViewPrefab;
        private readonly List<GameObject> _enemyViewPrefabs;
        private readonly IObjectResolver _resolver;

        public BattleEntry(BattleLoop loop_, BattleContext battleContext_, BattleEventDispatcher dispatcher_,
            BattleSegmentLoop segmentLoop_, BattleSegmentEventDispatcher segmentEvents_,
            BattleSegmentCommandDispatcher segmentCommands_, SegmentSceneManager segmentManager_,
            PopPanelFactory popFactory_, UI_ResultPanel resultPanel_, UI_ActorRoot actorRoot_,
            BattleStage stage_, RunState run_, RunCommandDispatcher commands_, RunProgressService progress_,
            [Key("Player")] GameObject playerViewPrefab_,
            List<GameObject> enemyViewPrefabs_, IObjectResolver resolver_)
        {
            _loop = loop_;
            _battleContext = battleContext_;
            _dispatcher = dispatcher_;
            _segmentLoop = segmentLoop_;
            _segmentEvents = segmentEvents_;
            _segmentCommands = segmentCommands_;
            _segmentManager = segmentManager_;
            _popFactory = popFactory_;
            _resultPanel = resultPanel_;
            _actorRoot = actorRoot_;
            _stage = stage_;
            _run = run_;
            _commands = commands_;
            _progress = progress_;
            _playerViewPrefab = playerViewPrefab_;
            _enemyViewPrefabs = enemyViewPrefabs_;
            _resolver = resolver_;
        }

        public void Start()
        {
            _segmentEvents.Register<BAE_RewardOffered>(OnRewardOffered);
            _segmentEvents.Register<BAE_RewardClaimed>(claimed_ => _segmentManager.CompleteSegment());
            // 整局终点弹结果面板：面板自身拦下全部输入，确认后由面板直调编排者回主菜单
            _segmentEvents.Register<BAE_RunCleared>(cleared_ => ShowResult(EResultKind.RunVictory));
            _segmentEvents.Register<BAE_RunDefeated>(defeated_ => ShowResult(EResultKind.RunDefeated));
            // 无奖励胜利等开发期缺口维持立即卸段，不弹面板
            _segmentEvents.Register<BAE_BattleExited>(exited_ => _segmentManager.FinishSegment());

            // 领奖中读档：本段只作背景，不生成单位、不跑战斗循环，直接复原同一批奖励
            if (_run.HasPendingRewards)
            {
                _stage.HideHud();
                ShowRestoreRewards();
                return;
            }

            SpawnPlayerView();
            SpawnEnemyViews();
            _loop.Start();
            _segmentLoop.Start();
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
                    Debug.LogError($"[BattleEntry] 敌人 {enemy.Creature.ActorName} 未配置视图预制体（SO_Enemy.ViewPrefab），本次跳过");
                    continue;
                }

                Transform slot = _actorRoot.GetSlotTransform(enemy.Slot);
                if (!slot)
                {
                    Debug.LogError($"[BattleEntry] 敌人 {enemy.Creature.ActorName} 所在槽位点未接线，本次跳过");
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

        // 弹板自管领取与演出，「继续」关板并交回领完意图；弹板失败降级为跳过奖励（异常照报，不静默吞）
        private void OnRewardOffered(BAE_RewardOffered offered_)
        {
            _stage.HideHud(); // 战斗完成：交互层收起，画面上只留奖励面板
            _progress.SetPhase(ESavePhase.AwaitingReward); // 领奖中读档据此复原，不重打战斗
            SaveService.Save(); // 唯一检查点：战斗后 RunState + 待领奖励清单落盘，领奖期间不再写盘
            try
            {
                UI_RewardPanel panel = _popFactory.AcquireRewardPanel();
                _commands.BeginRewardClaim(offered_.Rewards);
                panel.Show("战斗奖励", offered_.Rewards, () =>
                {
                    _commands.EndRewardClaim();
                    panel.Close();
                    _segmentCommands.AcknowledgeReward();
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _segmentCommands.SkipReward();
            }
        }

        // 领奖中读档的复原：清单来自存档，不重掷、不再走段 Loop 的等待点，领完直接完成本段
        private void ShowRestoreRewards()
        {
            UI_RewardPanel panel = _popFactory.AcquireRewardPanel();
            _commands.BeginRewardClaim(_run.PendingRewards);
            panel.Show("战斗奖励", _run.PendingRewards, () =>
            {
                _commands.EndRewardClaim();
                panel.Close();
                _segmentManager.CompleteSegment();
            });
        }

        // 战斗结束弹整局结果面板：先收起战斗交互层，免得面板背后露出空手牌与回合按钮
        private void ShowResult(EResultKind kind_)
        {
            _stage.HideHud();
            _resultPanel.Show(kind_);
        }
    }
}
