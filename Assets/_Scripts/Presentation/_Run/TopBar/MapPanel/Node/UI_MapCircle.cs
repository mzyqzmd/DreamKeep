using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>当前位置标记：PlayAt 摆到节点并从第 0 帧播一次（单次状态播完自然停在末帧），
    /// ShowAt 只摆位并直接定到末帧（重开地图不重播），Hide 收回。
    /// 纯视觉不接指针事件，帧 Image 关 raycast 防挡节点点击。</summary>
    public sealed class UI_MapCircle : MonoBehaviour
    {
        private const string PlayState = "MapCircleOnce";

        [Tooltip("帧图（只用来关射线，画面由动画机驱动）")] [SerializeField]
        private UnityEngine.UI.Image _frame;

        [Tooltip("动画机：播单次状态后停在末帧")] [SerializeField]
        private Animator _animator;

        [Tooltip("圈相对节点外扩比例：略大于节点才能把整只节点包住")] [SerializeField]
        private float _sizePadding = 1.05f;

        private int _stateHash;
        private Vector2 _baseSize; // 预制体原始尺寸（180×176），必须早于任何缩放记录

        /// <summary>本次播放时长（秒）：PlayAt 之后读动画状态长度，空引用安全返回 0。</summary>
        public float PlayDuration => _animator != null ? _animator.GetCurrentAnimatorStateInfo(0).length : 0f;

        /// <summary>迁到节点位置、按节点尺寸等比缩放，并从头播一次；重触发即重播。</summary>
        public void PlayAt(RectTransform parent_, Vector2 anchoredPos_, Vector2 targetSize_)
        {
            PlaceAt(parent_, anchoredPos_, targetSize_);
            PlayFrame(0f);
        }

        /// <summary>迁到节点位置、按节点尺寸缩放并直接停在末帧：重开地图用，不产生播放过程。</summary>
        public void ShowAt(RectTransform parent_, Vector2 anchoredPos_, Vector2 targetSize_)
        {
            PlaceAt(parent_, anchoredPos_, targetSize_);
            PlayFrame(1f);
        }

        public void Hide() => gameObject.SetActive(false);

        private void PlaceAt(RectTransform parent_, Vector2 anchoredPos_, Vector2 targetSize_)
        {
            EnsureRefs();
            RectTransform rect = (RectTransform)transform;
            transform.SetParent(parent_, false);
            // 与节点/连线同一坐标系（父级顶边中点）：预制体根锚点是居中，直接用会整体偏移半层高
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPos_;
            // 按长边等比缩放：Boss 这类宽高比不是 1 的节点也能整只圈住；每次重写，实例复用不会残留旧缩放
            float baseMax = Mathf.Max(_baseSize.x, _baseSize.y);
            float targetMax = Mathf.Max(targetSize_.x, targetSize_.y);
            rect.localScale = Vector3.one * (baseMax <= 0f ? 1f : targetMax * _sizePadding / baseMax);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        // 预制体 KeepAnimatorStateOnDisable=0：失活不保留姿态，显示时必须显式定帧
        private void PlayFrame(float normalized_)
        {
            if (_animator == null) return;
            if (_stateHash == 0) _stateHash = Animator.StringToHash(PlayState);
            _animator.Play(_stateHash, 0, normalized_);
            _animator.Update(0f);
        }

        // 面板 inactive 下实例化的圆环不跑 Awake，引用一律懒取
        private void EnsureRefs()
        {
            if (_frame == null) _frame = GetComponent<UnityEngine.UI.Image>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_frame != null) _frame.raycastTarget = false;
            if (_baseSize.x <= 0f) _baseSize = ((RectTransform)transform).rect.size; // 首次进来还没被缩放，取到的就是预制体尺寸
        }
    }
}
