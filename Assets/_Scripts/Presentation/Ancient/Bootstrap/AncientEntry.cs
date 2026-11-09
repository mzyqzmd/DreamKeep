using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>先古之民段表现入口：启动域流程，把异常报告统一写 Console、把强制选牌请求接上段内弹板并交回命令门面，
    /// 再把段完成事实转成整局编排者的收段动作。视图只订阅事实与提交意图，不再一路 await 面板。</summary>
    public sealed class AncientEntry : IStartable
    {
        private readonly AncientLoop _loop;
        private readonly AncientEventDispatcher _events;
        private readonly AncientCommandDispatcher _commands;
        private readonly RunState _run;
        private readonly UI_CardUpgradePanel _upgradePanel;
        private readonly UI_CardRemovePanel _removePanel;
        private readonly SegmentSceneManager _segmentManager;

        public AncientEntry(AncientLoop loop_, AncientEventDispatcher events_, AncientCommandDispatcher commands_,
            RunState run_, UI_CardUpgradePanel upgradePanel_, UI_CardRemovePanel removePanel_,
            SegmentSceneManager segmentManager_)
        {
            _loop = loop_;
            _events = events_;
            _commands = commands_;
            _run = run_;
            _upgradePanel = upgradePanel_;
            _removePanel = removePanel_;
            _segmentManager = segmentManager_;
        }

        public void Start()
        {
            _events.Register<AE_ErrorReported>(reported_ => Debug.LogError(reported_.Message));
            // 强制选牌：交回取消由域流程重弹（cancelable=false 的时机由 helper 内部保证）
            _events.Register<AE_UpgradeCardsRequested>(requested_ =>
                CardSelectFlow.OpenAndSubmitAsync(_upgradePanel, _run.Player, requested_.Count,
                    cards_ => _commands.SubmitCardSelection(cards_), cancelable_: false).Forget());
            _events.Register<AE_RemoveCardsRequested>(requested_ =>
                CardSelectFlow.OpenAndSubmitAsync(_removePanel, _run.Player, requested_.Count,
                    cards_ => _commands.SubmitCardSelection(cards_), cancelable_: false).Forget());
            _events.Register<AE_AncientCompleted>(completed_ => _segmentManager.CompleteSegment());
            _loop.Start();
        }

    }
}
