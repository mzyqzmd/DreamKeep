using _DreamKeep.EngineUtility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>整局段推进：选点推进、阶段位、写盘时机与待领清单清理都在这里，表现层只负责场景装卸与演出。
    /// 实现 ISaveable，阶段位随盘；读档据阶段位决定是否跳过战斗循环直接重领奖励。
    /// 住 RuntimeData 是因为存档接口只对这条边开放，GameRun 不再扩大对引擎桥接层的引用。</summary>
    public sealed class RunProgressService : ISaveable
    {
        private const string KeyPhase = "phase";

        private readonly RunState _run;
        private ESavePhase _phase = ESavePhase.OnMap;

        public RunProgressService(RunState run_) => _run = run_;

        #region 选点推进：推进节点与层数，并把选点检查点落盘

        public ESegmentKind EnterNode(MapNode node_)
        {
            _run.EnterMapNode(node_);
            _phase = ESavePhase.InBattle;
            SaveService.Save(); // 选点即落盘：路线锁定，强退后也不能退回上一节点重选
            return RunFlow.SegmentKindOf(node_.ResolvedType);
        }

        #endregion

        #region 阶段位与待领清单：进段、领奖、回地图三处更新

        public void SetPhase(ESavePhase phase_) => _phase = phase_;

        public void MarkOnMap()
        {
            _run.ClearPendingRewards(); // 领奖链结束：清内存待领清单，下一场不再误判为恢复
            _phase = ESavePhase.OnMap;
        }

        public void ClearPendingRewards() => _run.ClearPendingRewards();

        #endregion

        #region 存档：阶段位随盘，读档据此决定是否跳过战斗循环直接重领奖励

        public void Capture(ISaveWriter writer_) => writer_.Write(KeyPhase, (int)_phase);

        public void Restore(ISaveReader reader_)
        {
            _phase = (ESavePhase)reader_.Read(KeyPhase, (int)ESavePhase.OnMap);
            // 旧档降级：领奖阶段却没有奖励清单，按战斗中重打本场，避免读档后无面板可恢复而卡在空段
            if (_phase == ESavePhase.AwaitingReward && !_run.HasPendingRewards)
                _phase = ESavePhase.InBattle;
            SaveService.MarkRestoreIntent((int)RunFlow.SegmentKindOf(_run.Map.CurrentNode.ResolvedType));
        }

        #endregion

        #region 整局结束：删档由域侧统一执行

        public void DeleteSave() => SaveService.Delete();

        #endregion
    }
}
