using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>回合横幅公告：回合开始时显示"第 X 回合"与"玩家回合/敌人回合"，纯视图、不订事件不认识容器——
    /// 懒生成与事件订阅归 UI_PopTextRoot。时间线为 _turnText 从放大收拢到目标尺寸入场、停留、整层渐隐后自收（SetActive(false)），
    /// 播放与抽牌等回合演出同屏并行（fire-and-forget，不由域侧 await）；重复触发即杀旧时间线重播。</summary>
    public class UI_TurnCountPopTip : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _turnText;
        [SerializeField] private TextMeshProUGUI _turnCountText;

        [Tooltip("渐隐通道（整层透明度）：预制体没挂时 Awake 自取或自补，无需 Editor 接线")] [SerializeField]
        private CanvasGroup _canvasGroup;

        [Header("动效")] [SerializeField] private float _showScaleFrom = 1.6f; // 入场缩放起点（相对目标尺寸的倍率）：从放大收拢
        [SerializeField] private float _showScaleDuration = 0.2f;
        [SerializeField] private float _holdDuration = 0.7f; // 入场收拢后的停留时长
        [SerializeField] private float _fadeOutDuration = 0.3f;

        private Sequence _timeline;
        private Vector3 _turnTextTargetScale;

        private void Awake()
        {
            _turnTextTargetScale = _turnText.transform.localScale;
            if (!_canvasGroup && !TryGetComponent(out _canvasGroup)) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        #region 对外展示：写文案并重播整条时间线

        /// <summary>展示回合横幅：写文案、复位透明度后重播"收拢入场 → 停留 → 渐隐"；播放期间再触发则杀旧重播。</summary>
        public void Show(bool isPlayerTurn_, int turnCount_)
        {
            _turnText.text = isPlayerTurn_ ? "玩家回合" : "敌人回合";
            _turnCountText.text = $"第 {turnCount_} 回合";
            gameObject.SetActive(true);
            PlayTimeline();
        }

        #endregion

        // 时间线：收拢入场 → 停留 → 整层渐隐 → 自收；透明度复位放在每次重播开头，收起时不必还原
        private void PlayTimeline()
        {
            _timeline?.Kill();
            _canvasGroup.alpha = 1f;
            _timeline = DOTween.Sequence()
                .Join(_turnText.transform.DOScale(_turnTextTargetScale, _showScaleDuration)
                    .From(_turnTextTargetScale * _showScaleFrom).SetEase(Ease.OutCubic))
                .AppendInterval(_holdDuration)
                .Append(DOTween.To(() => _canvasGroup.alpha, value_ => _canvasGroup.alpha = value_, 0f, _fadeOutDuration))
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void OnDestroy() => _timeline?.Kill();
    }
}
