using _DreamKeep.EngineUtility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>整局段推进：选点推进、阶段位、写盘时机与待领清单清理都在这里，表现层只负责场景装卸与演出。
    /// 阶段真源住 RunState.Phase，本类只负责推进与落盘；读档据阶段位决定是否跳过战斗循环直接重领奖励。
    /// 住 RuntimeData 是因为存档接口只对这条边开放，GameRun 不再扩大对引擎桥接层的引用。</summary>
    public sealed class RunProgressService : ISaveable
    {
        private const string KeyPhase = "phase";

        public string SaveKey => GetType().FullName;

        private readonly RunState _run;

        public RunProgressService(RunState run_) => _run = run_;

        #region 选点推进：推进节点与层数，并把选点检查点落盘

        public ESegmentKind EnterNode(MapNode node_)
        {
            _run.EnterMapNode(node_);
            _run.SetPhase(ESavePhase.InBattle);
            SaveService.Save(); // 选点即落盘：路线锁定，强退后也不能退回上一节点重选
            return RunFlow.SegmentKindOf(node_.ResolvedType);
        }

        #endregion

        #region 阶段位与段挂起：进段挂起、收段解挂，统一走域侧

        /// <summary>进段：挂起整图可走态，内容未完成时没有一个节点可点。</summary>
        public void EnterSegment() => _run.SetSegmentPending(true);

        /// <summary>离段：解除挂起，地图恢复可走。</summary>
        public void LeaveSegment() => _run.SetSegmentPending(false);

        /// <summary>段完成回地图：解锁地图并回 OnMap 阶段。段收益在下一次选点时才落盘。</summary>
        public void MarkOnMap()
        {
            _run.SetSegmentPending(false);
            _run.SetPhase(ESavePhase.OnMap);
        }

        /// <summary>领奖检查点：掷奖已完成、清单已写入 RunState，这里统一写阶段位并落盘。</summary>
        public void CheckpointAwaitingReward()
        {
            _run.SetPhase(ESavePhase.AwaitingReward);
            SaveService.Save();
        }

        public void ClearPendingRewards() => _run.ClearPendingRewards();

        #endregion

        #region 存档：阶段位随盘，读档据此决定是否跳过战斗循环直接重领奖励

        public void Capture(ISaveWriter writer_) => writer_.Write(KeyPhase, (int)_run.Phase);

        public void Restore(ISaveReader reader_)
        {
            ESavePhase phase = (ESavePhase)reader_.Read(KeyPhase, (int)ESavePhase.OnMap);
            // 旧档降级：领奖阶段却没有奖励清单，按战斗中重打本场，避免读档后无面板可恢复而卡在空段
            if (phase == ESavePhase.AwaitingReward && !_run.HasPendingRewards)
                phase = ESavePhase.InBattle;
            _run.SetPhase(phase);
            SaveService.MarkRestoreIntent((int)RunFlow.SegmentKindOf(_run.Map.CurrentNode.ResolvedType));
        }

        #endregion

        #region 整局结束：删档由域侧统一执行

        public void DeleteSave() => SaveService.Delete();

        #endregion
    }
}
