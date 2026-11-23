using System;
using Cysharp.Threading.Tasks;
using _DreamKeep.CommonPresentation;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>换段过渡：持有换段闸门与黑屏包装，按"收面板 → 关会话 → 卸旧段 → 装新段"的固定次序驱动换段。
    /// 不认识段种类映射与 BGM，那些由表现协调方负责；本类只保证装卸次序与失败日志口径。</summary>
    public sealed class SegmentTransitionCoordinator
    {
        private readonly SegmentSceneLoader _loader;
        private readonly Func<UI_FadeScreen> _fadeScreen;
        private readonly Func<PopPanelFactory> _popFactory;
        private readonly Action _closeMap;
        private readonly Action _showTopBar;
        private readonly Action _closeBattleSession;
        private readonly string _logPrefix;

        public SegmentTransitionCoordinator(SegmentSceneLoader loader_, Func<UI_FadeScreen> fadeScreen_,
            Func<PopPanelFactory> popFactory_, Action closeMap_, Action showTopBar_,
            Action closeBattleSession_, string logPrefix_)
        {
            _loader = loader_;
            _fadeScreen = fadeScreen_;
            _popFactory = popFactory_;
            _closeMap = closeMap_;
            _showTopBar = showTopBar_;
            _closeBattleSession = closeBattleSession_;
            _logPrefix = logPrefix_;
        }

        public bool Switching { get; set; }

        /// <summary>过渡分发：常驻幕布未注入时退化为直跑（不黑屏不挡功能）。</summary>
        public UniTask CoverAsync(Func<UniTask> work_)
        {
            UI_FadeScreen fade = _fadeScreen();
            return fade ? fade.CoverAsync(work_) : work_();
        }

        /// <summary>黑屏内的换段链：先卸旧段再装新段——新段构建时会按类型扫场景注册，两段同类组件共存会互相污染。</summary>
        public async UniTask SwitchSegmentAsync(string name_)
        {
            try
            {
                // 进新段前收起上一场的奖励面板与地图：面板不随「继续」关闭，留着会挡住下一场
                // （只关已建出的实例：为关一次而懒建，会在常驻画布凭空多出面板克隆体）
                PopPanelFactory popFactory = _popFactory();
                if (popFactory) popFactory.TryCloseRewardPanel();
                _closeMap(); // 只收起地图面板本身，不整栏失活——整栏失活后再激活会让地图跟着复活
                _closeBattleSession();
                await _loader.UnloadCurrentSegmentAsync();
                if (!await _loader.LoadSegmentAsync(name_))
                    Debug.LogError($"[{_logPrefix}] 段场景加载失败（不在构建列表或句柄失效），本局作废");
                else _showTopBar(); // 进入段场景后整局顶栏才显示
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"[{_logPrefix}] 段装配异常，本局作废：{e.Message}");
            }
        }
    }
}
