using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;

namespace _DreamKeep.GameRun
{
    /// <summary>战斗段收尾流程：等战斗循环跑完（一次性完成信号）后裁决去留——
    /// 胜利当帧标记节点通过（不等奖励面板的「继续」），有配置且掷出奖励才弹面板等待领取，
    /// 战败 / 无配置 / 掷空 / 表现弹板失败一律按退出收尾。等待点由命令门面放行，玩法事实经段分发器播出；
    /// 纯 C#，配置缺口经注入的 onWarning_ 上报，不直接依赖引擎日志出口。
    /// 本 Loop 随段 scope 生灭，段被切走时等待链悬空待 GC，不残留跨段信号。</summary>
    public sealed class BattleSegmentLoop
    {
        private readonly RunState _run;
        private readonly BattleLoop _battleLoop;
        private readonly SO_EncounterReward _reward;
        private readonly BattleSegmentEventDispatcher _events;
        private readonly Action<string> _onWarning;
        private UniTaskCompletionSource<bool> _rewardDone = new();

        public BattleSegmentLoop(RunState run_, BattleLoop battleLoop_, SO_EncounterReward reward_,
            BattleSegmentEventDispatcher events_, Action<string> onWarning_)
        {
            _run = run_;
            _battleLoop = battleLoop_;
            _reward = reward_;
            _events = events_;
            _onWarning = onWarning_;
        }

        public void Start() => RunAsync().Forget();

        /// <summary>奖励面板「继续」已点：领完放行，段按完成收尾。</summary>
        public void AcknowledgeReward() => _rewardDone.TrySetResult(true);

        /// <summary>表现弹奖励面板失败：降级放行，段按退出收尾。</summary>
        public void SkipReward() => _rewardDone.TrySetResult(false);

        // 等待点构造期建好：命令早于 await 到达也不会丢；循环型等待点在 await 之后重置
        private async UniTask RunAsync()
        {
            await _battleLoop.WaitFinishedAsync();

            // 战败不通过（节点保持原状）：整局终点事实，表现侧弹结果面板，确认后回主菜单重开
            if (!_battleLoop.IsVictory)
            {
                await _events.PlayAsync(new BAE_RunDefeated());
                return;
            }

            _run.CompleteMapNode(); // 胜利即通过：地图推进不等奖励面板的「继续」

            // Boss 恒为最后一行：Boss 节点胜利即整局通关，跳过奖励链直接弹整局结果面板（确认后回主菜单重开）
            if (_run.Map.CurrentNode.PointType == EMapPointType.Boss)
            {
                await _events.PlayAsync(new BAE_RunCleared());
                return;
            }

            // 掷奖励消耗整局随机流，必须在域侧掷；掷骰异常与掷空（无配置或候选全跳过）都按退出收尾
            List<RunReward> rewards;
            try
            {
                rewards = EncounterRewardRoller.Roll(_reward, _run, _onWarning);
            }
            catch (Exception e)
            {
                _onWarning?.Invoke($"[{nameof(BattleSegmentLoop)}] 奖励掷骰异常，按退出收尾：{e}");
                await _events.PlayAsync(new BAE_BattleExited());
                return;
            }

            if (rewards.Count == 0)
            {
                await _events.PlayAsync(new BAE_BattleExited());
                return;
            }

            await _events.PlayAsync(new BAE_RewardOffered(rewards));
            bool claimed = await _rewardDone.Task;
            _rewardDone = new UniTaskCompletionSource<bool>();
            await _events.PlayAsync(claimed ? new BAE_RewardClaimed() : new BAE_BattleExited());
        }
    }
}
