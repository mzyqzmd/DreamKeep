using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>牌堆查看卡：牌堆陈列里的纯展示卡——牌面由同节点的选择器按卡类型切换，本类只转发绑定，
    /// 关掉牌面自带的 card_ripple 涟漪动效（陈列态不播动效），悬停时词条说明交给同节点的气泡发起器（Pointer 模式自己接指针事件）。
    /// 无拖拽无点击行为：本卡不接任何指针事件，点击冒泡给查看面板——面板没有"点背景关闭"的入口，无需在此拦截。</summary>
    [RequireComponent(typeof(UI_HoverTip))]
    [RequireComponent(typeof(UI_CardFaceSelector))]
    public sealed class UI_CardDisplay : MonoBehaviour
    {
        [Tooltip("悬停信息气泡")] [SerializeField] private UI_HoverTip _hoverTip;

        private UI_CardFaceSelector _faces; // 四张内嵌牌面的持有者：当前是哪张由它回答

        // 词条说明的内容（复用列表）：Bind 时经牌面的收集方法重填一次，再递给气泡发起器
        private readonly List<(string Name, string Description)> _affixTips = new();

        public CardModel Card => _faces.Current.Card;

        private void Awake() => _faces = GetComponent<UI_CardFaceSelector>();

        #region 绑定：切脸→刷牌面→关涟漪→收集词条推气泡

        public void Bind(CardModel card_)
        {
            // 词条说明随绑定写一次（纯文本——渲染器退化原值；牌堆陈列没有预览对账，恒为此口径）
            _faces.BindStatic(card_, _affixTips);
            _hoverTip.SetContents(_affixTips);
        }

        #endregion

        #region 悬停气泡：内容由 Bind 注入，弹出与收起的时机交给宿主（事件选项条在指针进出时调用）

        /// <summary>弹出词条说明气泡（外部驱动模式：宿主判定的时刻调用）。</summary>
        public void ShowTip() => _hoverTip.Show();

        /// <summary>收起词条说明气泡。</summary>
        public void HideTip() => _hoverTip.Hide();

        #endregion
    }
}
