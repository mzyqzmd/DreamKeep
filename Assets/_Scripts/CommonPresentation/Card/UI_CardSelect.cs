using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>选牌卡：三选一陈列里的纯展示卡，牌面由同节点的选择器按卡类型切换，悬停放大与词条气泡交给同节点的组件。
    /// 本类只做绑定与"我被点了"的播报——选中后牌怎么进牌库、面板关不关，全归面板与调用方。</summary>
    [RequireComponent(typeof(UI_HoverTip))]
    [RequireComponent(typeof(UI_CardFaceSelector))]
    public sealed class UI_CardSelect : MonoBehaviour, IPointerClickHandler
    {
        // 词条说明的内容（复用列表）：Bind 时经牌面的收集方法重填一次，再递给气泡发起器
        private readonly List<(string Name, string Description)> _affixTips = new();

        private UI_CardFaceSelector _faces; // 四张内嵌牌面的持有者：当前是哪张由它回答
        private UI_HoverTip _hoverTip;

        public CardModel Card => _faces.Current.Card;

        /// <summary>点击播报：只交出自己的视图，选中什么由面板对外回报。</summary>
        public event Action<UI_CardSelect> Clicked;

        private void Awake()
        {
            _faces = GetComponent<UI_CardFaceSelector>();
            _hoverTip = GetComponent<UI_HoverTip>();
        }

        #region 绑定：切脸→刷牌面→关涟漪→收集词条推气泡

        public void Bind(CardModel card_)
        {
            // 词条说明随绑定写一次（纯文本——渲染器退化原值；选牌没有预览对账，恒为此口径）
            _faces.BindStatic(card_, _affixTips);
            _hoverTip.SetContents(_affixTips);
        }

        #endregion

        public void OnPointerClick(PointerEventData _) => Clicked?.Invoke(this);
    }
}
