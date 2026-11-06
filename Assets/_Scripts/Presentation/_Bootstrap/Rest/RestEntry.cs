using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;
using _DreamKeep.Presentation.Root;

namespace _DreamKeep.Presentation
{
    /// <summary>休息段表现入口：启动域流程，把弹板与特效请求接上段内视图并把结果交回命令门面，
    /// 再把段完成事实转成整局编排者的收段动作。视图只订阅事实与提交意图，不再一路 await 面板。</summary>
    public sealed class RestEntry : IStartable
    {
        private readonly RestLoop _loop;
        private readonly RestEventDispatcher _events;
        private readonly RestCommandDispatcher _commands;
        private readonly RunState _run;
        private readonly UI_RestVFXRoot _vfxRoot;
        private readonly UI_CardUpgradePanel _panel;
        private readonly PopPanelFactory _popPanelFactory;
        private readonly UI_RestProceedButton _proceedButton;
        private readonly SegmentSceneManager _segmentManager;
        private readonly VfxFactory _vfxFactory;
        private UI_CardRemovePanel _removePanel; // 工厂单例跨段复用，可能随停泊画布销毁重建，Start 时领取

        public RestEntry(RestLoop loop_, RestEventDispatcher events_, RestCommandDispatcher commands_, RunState run_,
            UI_RestVFXRoot vfxRoot_, UI_CardUpgradePanel panel_, PopPanelFactory popPanelFactory_,
            UI_RestProceedButton proceedButton_, SegmentSceneManager segmentManager_, VfxFactory vfxFactory_)
        {
            _loop = loop_;
            _events = events_;
            _commands = commands_;
            _run = run_;
            _vfxRoot = vfxRoot_;
            _panel = panel_;
            _popPanelFactory = popPanelFactory_;
            _proceedButton = proceedButton_;
            _segmentManager = segmentManager_;
            _vfxFactory = vfxFactory_;
        }

        public void Start()
        {
            _vfxRoot.Initialize(_vfxFactory); // 工厂在父容器、跨场景不可序列化，运行时交底
            // 删牌面板由工厂懒建单例：显式停泊到本段弹层画布容器（同锻造面板的父级），随段卸载销毁、下次进段重建
            _removePanel = _popPanelFactory.AcquireCardRemovePanel((RectTransform)_panel.transform.parent);

            _events.Register<REE_RestSmokeRequested>(smoke_ => _vfxRoot.PlayRestSmokeAsync());
            _events.Register<REE_UpgradeCardsRequested>(requested_ =>
                OpenCardPanelAsync(_panel, requested_.Count).Forget());
            _events.Register<REE_RemoveCardsRequested>(requested_ =>
                OpenCardPanelAsync(_removePanel, requested_.Count).Forget());
            _events.Register<REE_ProceedOffered>(OnProceedOffered);
            _events.Register<REE_RestCompleted>(completed_ => _segmentManager.CompleteSegment());
            _loop.Start();
        }

        // 弹板自管选牌、升级/移除与演出；关闭键交还 null = 取消，由命令门面原样交回域循环
        private async UniTask OpenCardPanelAsync(UI_CardSelectPanel panel_, int count_)
        {
            panel_.Show(_run.Player, count_);
            _commands.SubmitCardSelection(await panel_.WaitResultAsync());
        }

        private void OnProceedOffered(REE_ProceedOffered offered_)
        {
            _proceedButton.gameObject.SetActive(true);
            _proceedButton.Clicked += OnProceedButtonClicked;
        }

        private void OnProceedButtonClicked() => _commands.Proceed();
    }
}
