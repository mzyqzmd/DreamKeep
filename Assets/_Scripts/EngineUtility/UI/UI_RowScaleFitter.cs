using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.EngineUtility
{
    /// <summary>整行等比适配：激活子项超出组宽或组高时，取两个方向里更紧的比例整体缩小
    /// （子项与间距同比缩，卡面不变形），使整行既不超过组宽也不超过组高；两向都装得下时不放大。
    /// 缩放写子项 localScale，排布依赖 HorizontalLayoutGroup 的 Use Child Scale——脚本自行开启宽高两个开关；
    /// 写完必须重建布局，否则子项只是原地缩小、占位不变。编辑模式同样生效。</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public sealed class UI_RowScaleFitter : MonoBehaviour
    {
        //基准间距：首次启用时取布局组当前值，之后按缩放比例写回
        private float _baseSpacing = -1f;

        private HorizontalLayoutGroup _layout;
        private RectTransform _rect;
        private int _appliedCount = -1;
        private float _appliedScale = 1f;
        private float _appliedSpacing;

        private void OnEnable()
        {
            _layout = GetComponent<HorizontalLayoutGroup>();
            _rect = (RectTransform)transform;
            _layout.childScaleWidth = true;
            _layout.childScaleHeight = true;
            if (_baseSpacing <= 0f) _baseSpacing = _layout.spacing;
            _appliedSpacing = _layout.spacing;
            _appliedCount = -1;
        }

        private void OnDisable()
        {
            _appliedCount = -1;
            if (_layout == null || Mathf.Approximately(_appliedScale, 1f)) return;
            WriteChildren(1f);
            _layout.spacing = _baseSpacing;
            _appliedScale = 1f;
            _appliedSpacing = _baseSpacing;
            if (_rect != null) LayoutRebuilder.MarkLayoutForRebuild(_rect);
        }

        private void Update() => Fit();

        private void Fit()
        {
            if (_layout == null) return;

            // 当前间距与上次写入值不同 = 作者在 Inspector 改了间距，改认新基准，免得把缩放结果当基准累积缩水
            if (!Mathf.Approximately(_layout.spacing, _appliedSpacing)) _baseSpacing = _layout.spacing;

            int count = 0;
            float rowWidth = 0f;
            float rowHeight = 0f;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) is not RectTransform child || !child.gameObject.activeInHierarchy) continue;
                count++;
                rowWidth += child.sizeDelta.x;
                rowHeight = Mathf.Max(rowHeight, child.sizeDelta.y);
            }

            if (count == 0)
            {
                if (!Mathf.Approximately(_layout.spacing, _baseSpacing)) _layout.spacing = _baseSpacing;
                _appliedCount = 0;
                _appliedScale = 1f;
                _appliedSpacing = _baseSpacing;
                return;
            }

            rowWidth += _baseSpacing * (count - 1);
            float availableWidth = _rect.rect.width - _layout.padding.horizontal;
            float availableHeight = _rect.rect.height - _layout.padding.vertical;
            float scaleWidth = rowWidth <= 0f || availableWidth <= 0f ? 1f : availableWidth / rowWidth;
            float scaleHeight = rowHeight <= 0f || availableHeight <= 0f ? 1f : availableHeight / rowHeight;
            float scale = Mathf.Clamp01(Mathf.Min(scaleWidth, scaleHeight));

            if (count == _appliedCount && Mathf.Approximately(scale, _appliedScale)) return;
            _appliedCount = count;
            _appliedScale = scale;
            _appliedSpacing = _baseSpacing * scale;

            _layout.spacing = _appliedSpacing;
            WriteChildren(scale);
            LayoutRebuilder.MarkLayoutForRebuild(_rect);
        }

        // 同通道的悬停缩放要把基准同步成适配比例，否则离场会回弹到 1.0
        private void WriteChildren(float scale_)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) is not RectTransform child || !child.gameObject.activeInHierarchy) continue;
                child.localScale = Vector3.one * scale_;
                if (child.TryGetComponent(out UI_HoverScaler hover)) hover.SetBaseScale(scale_);
            }
        }
    }
}