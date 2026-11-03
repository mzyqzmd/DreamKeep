using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>商店卡位：绑卡时切到对应牌面、关掉涟漪、把词条说明推给同节点的气泡，促销时亮出半价标签。
    /// 悬停只驱动当前牌面放大——半价标签在绑定时重挂到该牌面下随它一起缩放，价格与根节点不缩放；
    /// 左键购买、右键交给地毯打开统一卡牌特写。</summary>
    public sealed class UI_CardShop : UI_ShopItem
    {
        [Tooltip("悬停词条气泡")] [SerializeField] private UI_HoverTip _hoverTip;

        [Tooltip("四张内嵌牌面的持有者")] [SerializeField] private UI_CardFaceSelector _faces;

        [Tooltip("半价标签：促销时亮出，悬停与牌面一起放大")] [SerializeField] private GameObject _salesTag;

        // 词条说明的复用缓冲：每次绑定重填一次，再整表递给气泡发起器
        private readonly List<(string Name, string Description)> _affixTips = new();

        private bool _hoverWired; // 悬停订阅只挂一次：Awake 与 Bind 都可能成为首次触碰（地毯初始失活时 Awake 不跑）

        /// <summary>本格绑定的卡：成交时按它入牌组。</summary>
        public CardModel Card { get; private set; }

        private void Awake() => EnsureRefs();

        #region 绑定：切脸刷面→收词条推气泡→按促销态开关标签→写价

        public void Bind(CardModel card_, int price_, bool onSale_, int gold_)
        {
            EnsureRefs();
            Card = card_;
            UI_CardFace face = _faces.BindStatic(card_, _affixTips);
            _hoverTip.SetContents(_affixTips);
            BindSalesTag(face, onSale_);
            ResetScale();
            ShowPrice(price_, gold_, onSale_);
        }

        // 半价标签跟着牌面走：重挂到当前牌面下，缩放与位移都由牌面带着走（价格留在根节点，不缩放不位移）；
        // 标签自己的 1.05 驱动因此作废——继承牌面的 1.05 再叠一层会变成 1.1025
        private void BindSalesTag(UI_CardFace face_, bool onSale_)
        {
            if (!_salesTag) return;
            Transform tag = _salesTag.transform;
            if (tag.parent != face_.transform) tag.SetParent(face_.transform, true);
            tag.localScale = Vector3.one;
            _salesTag.SetActive(onSale_);
        }

        #endregion

        // 悬停缩放只写一处：当前牌面（半价标签已是它的子节点，跟着一起放大）
        private void PlayHoverScale(bool hovered_)
        {
            UI_HoverScaler face = CurrentFaceScaler();
            if (!face) return;
            if (hovered_) face.PlayHoverScale();
            else face.PlayScale(1f);
        }

        // 换脸或重新绑定时把缩放硬写回 1：上一张牌面可能停在放大态，补间走完还会留下过渡帧
        private void ResetScale()
        {
            UI_HoverScaler face = CurrentFaceScaler();
            if (!face) return;
            face.Kill();
            face.transform.localScale = Vector3.one;
        }

        private UI_HoverScaler CurrentFaceScaler() =>
            _faces && _faces.Current ? _faces.Current.GetComponent<UI_HoverScaler>() : null;

        // 引用自愈：卡位可能在隐藏的地毯里被绑定（Awake 未跑），按类型补取一次；悬停订阅同样在这里兜底
        private void EnsureRefs()
        {
            if (!_hoverTip) _hoverTip = GetComponent<UI_HoverTip>();
            if (!_faces) _faces = GetComponent<UI_CardFaceSelector>();
            if (_hoverWired) return;
            _hoverWired = true;
            Hovered += _ => PlayHoverScale(true);
            Unhovered += _ => PlayHoverScale(false);
        }
    }
}
