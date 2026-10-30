using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡组陈列卡：网格与特写共用的纯展示卡——牌面由同节点的选择器按卡类型切换，本类只转发绑定，
    /// 关掉牌面自带的 card_ripple 涟漪动效（陈列态不播动效），悬停时词条说明交给同节点的气泡发起器（Pointer 模式自己接指针事件）。
    /// 点击行为由绑定方注入（网格卡点开特写）；回调为 null 即纯展示（特写态再点无意义，命中面只负责挡住背板）。</summary>
    [RequireComponent(typeof(UI_HoverTip))]
    [RequireComponent(typeof(UI_CardFaceSelector))]
    public sealed class UI_CardInDeck : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("悬停信息气泡")] [SerializeField] private UI_HoverTip _hoverTip;

        private UI_CardFaceSelector _faces; // 四张内嵌牌面的持有者：当前是哪张由它回答
        private Action<CardModel> _onClick; // 绑定方注入的点击回调：网格卡开特写用，null 即纯展示

        // 词条说明的内容（复用列表）：Bind 时经牌面的收集方法重填一次，再递给气泡发起器
        private readonly List<(string Name, string Description)> _affixTips = new();

        /// <summary>绑定的卡：当前牌面正处于哪张卡的展示态。</summary>
        public CardModel Card => _faces.Current.Card;

        private void Awake() => _faces = GetComponent<UI_CardFaceSelector>();

        public void OnPointerClick(PointerEventData eventData_) => _onClick?.Invoke(Card);

        #region 绑定：登记点击回调→切脸→刷牌面→关涟漪→收集词条推气泡

        public void Bind(CardModel card_, Action<CardModel> onClick_)
        {
            _onClick = onClick_;
            UI_CardFace face = _faces.Select(card_);
            face.Bind(card_);
            face.SetRippleVisible(false);
            // 词条说明随绑定写一次（牌面门面刚清空，数值按"与升级前基准静态比较"着色；卡组陈列没有预览对账，恒为此口径）
            face.CollectTipLines(_affixTips);
            _hoverTip.SetContents(_affixTips);
        }

        #endregion
    }
}
