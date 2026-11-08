using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>整局结果面板（Scene_Run 的 Canvas_System 常驻，System 层盖过一切 UI）：整局终点共用——
    /// 通关 / 战败，全屏遮罩拦下全部输入，弹出后玩家唯一可点的是返回主菜单；
    /// 确认经 RequestReturnToMainMenu 上报常驻主菜单编排者收尾（黑屏清理并卸载 Scene_Run）。文案在 Inspector 按终点配置，
    /// 战败额外显示原因行，通关与无原因终点隐藏。</summary>
    public sealed class UI_ResultPanel : UI_Panel
    {
        [Tooltip("Banner 标题")] [SerializeField]
        private TextMeshProUGUI _titleText;

        [Tooltip("结果描述")] [SerializeField] private TextMeshProUGUI _descriptionText;

        [Tooltip("原因行：战败显示、通关隐藏")] [SerializeField]
        private TextMeshProUGUI _reasonText;

        [Tooltip("返回主菜单按钮：确认整局终点后收尾")] [SerializeField]
        private Button _returnButton;

        [Tooltip("通关标题")] [SerializeField, TextArea(3, 10)]
        private string _winTitle;

        [Tooltip("通关描述")] [SerializeField, TextArea(3, 10)]
        private string _winDescription;

        [Tooltip("战败标题")] [SerializeField, TextArea(3, 10)]
        private string _defeatedTitle;

        [Tooltip("战败描述")] [SerializeField, TextArea(3, 10)]
        private string _defeatedDescription;

        [Tooltip("战败原因默认文案：Show 传入 reason_ 可覆盖")] [SerializeField, TextArea(3, 10)]
        private string _defeatedReason;

        private SegmentSceneManager _segmentManager; // 常驻编排者：确认后直接收尾，不经段入口转发
        private AudioRoot _audio;

        [Inject]
        public void Construct(SegmentSceneManager segmentManager_, AudioRoot audioRoot_)
        {
            _segmentManager = segmentManager_;
            _audio = audioRoot_;
        }

        private void Awake()
        {
            if (_returnButton) _returnButton.onClick.AddListener(OnReturnClicked);
        }

        private void OnDestroy()
        {
            if (_returnButton) _returnButton.onClick.RemoveListener(OnReturnClicked);
        }

        #region 对外：按终点弹出

        /// <summary>按终点种类填充文案后弹出：reason_ 传入时覆盖战败原因默认文案。</summary>
        public void Show(EResultKind kind_, string reason_ = null)
        {
            bool win = kind_ == EResultKind.RunVictory;
            _titleText.text = win ? _winTitle : _defeatedTitle;
            _descriptionText.text = win ? _winDescription : _defeatedDescription;
            if (_reasonText)
            {
                string reason = reason_ ?? _defeatedReason;
                bool showReason = !win && !string.IsNullOrEmpty(reason);
                _reasonText.gameObject.SetActive(showReason);
                if (showReason) _reasonText.text = reason;
            }

            if (_audio)
            {
                _audio.StopBgm();
                _audio.Play(win ? _audio.Cues.Victory : _audio.Cues.Defeat);
            }

            base.Show();
        }

        #endregion

        private void OnReturnClicked() => _segmentManager.RequestReturnToMainMenu();
    }
}