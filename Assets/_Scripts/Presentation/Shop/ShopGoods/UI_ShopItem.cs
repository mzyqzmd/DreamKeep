using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>商店商品视图基类：统一「价格文本与三态着色 + 被拒抖动 + 左右键与悬停上报 + 售出留位」。
    /// 具体商品只负责自己的内容（卡切脸、遗物/药水换图标）与绑定时把售价交给它。
    /// 售出后根节点保持激活占住布局槽位，只收起全部子内容并关掉命中射线——整行位置因此不会重排。</summary>
    public abstract class UI_ShopItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("价格")]
        [Tooltip("价格文本：显示当前售价")] [SerializeField]
        private TextMeshProUGUI _priceText;

        [Tooltip("原价且买得起")] [SerializeField]
        private Color _affordableColor = Color.white;

        [Tooltip("买不起（不分是否打折）")] [SerializeField]
        private Color _unaffordableColor = new(1f, 0.42f, 0.42f);

        [Tooltip("打折且买得起")] [SerializeField]
        private Color _saleColor = new(0.35f, 0.9f, 0.35f);

        [Header("被拒反馈")]
        [Tooltip("抖动幅度（画布像素）")] [SerializeField]
        private float _shakeAmplitudePx = 12f;

        [Tooltip("抖动时长（秒）")] [SerializeField] private float _shakeDuration = 0.3f;

        private Tween _shakeTween;

        /// <summary>左键购买：地毯据此把这一格交给段入口结算。</summary>
        public event Action<UI_ShopItem> BuyRequested;

        /// <summary>右键查看详情：目前只有卡位响应。</summary>
        public event Action<UI_ShopItem> DetailRequested;

        /// <summary>指针进入本格：地毯据此让商人手移过来。</summary>
        public event Action<UI_ShopItem> Hovered;

        /// <summary>指针离开本格：商人手归位。</summary>
        public event Action<UI_ShopItem> Unhovered;

        /// <summary>本格当前售价：成交结算与气泡文案都以它为准。</summary>
        public int Price { get; private set; }

        /// <summary>本格是否已售出（或本就缺货）：售出后不再响应任何指针事件。</summary>
        public bool Sold { get; private set; }

        // 失活先停抖：在抖的先跳回基准再清（Settle），半途位移不会带进下次；格子收起后也不再留着补间写位置
        private void OnDisable() => UiTween.Settle(ref _shakeTween);

        public void OnPointerClick(PointerEventData eventData_)
        {
            if (Sold) return;
            if (eventData_.button == PointerEventData.InputButton.Right) DetailRequested?.Invoke(this);
            else BuyRequested?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (Sold) return;
            Hovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (Sold) return;
            Unhovered?.Invoke(this);
        }

        #region 对外：报价、被拒与售出

        /// <summary>写售价并按当前金币与是否打折决定颜色：买不起恒红，否则打折绿、原价白。
        /// 每次成交后地毯会给整行重染一次。</summary>
        public void ShowPrice(int price_, int gold_, bool onSale_ = false)
        {
            Price = price_;
            if (Sold || !_priceText) return;
            _priceText.text = price_.ToString();
            _priceText.color = gold_ < price_ ? _unaffordableColor : onSale_ ? _saleColor : _affordableColor;
        }

        /// <summary>被拒反馈：左右抖一下（金币不足、药水槽满），不改变任何数据。</summary>
        public void PlayRefuse()
        {
            if (Sold) return;
            UiTween.PlayShake(ref _shakeTween, (RectTransform)transform, _shakeAmplitudePx, _shakeDuration);
        }

        /// <summary>成交或售罄：根节点留在布局里占位，只收起全部子内容并关掉命中射线（可重复调用）。</summary>
        public void MarkSold()
        {
            if (Sold) return;
            Sold = true;
            _shakeTween?.Kill();

            // 内容一律是根节点的直接子物体（四张脸 / 半价标签 / 价格；图标 / 价格），整体收起即可
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
            if (TryGetComponent(out Image hit)) hit.raycastTarget = false;
        }

        #endregion
    }
}
