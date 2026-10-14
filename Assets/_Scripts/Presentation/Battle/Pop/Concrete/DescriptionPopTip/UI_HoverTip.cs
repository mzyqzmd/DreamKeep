using System;
using System.Collections.Generic;
using _DreamKeep.Utility;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>说明气泡发起器：需要"指针悬停弹说明气泡"的对象挂上它即可用——内容可提前在 Inspector 配置（静态），
    /// 也可由宿主在数据变化时注入（动态，单条或整列）；气泡视图与弹层宿主仍归 UI_PopTextRoot 统一管（实例取还、叠列、定位），
    /// 本组件只回答"谁被悬停、弹什么、弹在哪"。
    /// 触发分两种模式：默认自己接 UGUI 指针事件（挂上即用）；外部驱动模式不响应指针，由调用方在它判定出的时刻调 Show/Hide
    /// （手牌上的卡牌即这一种——卡的"悬停"由手牌区每帧射线裁决，瞄准中指针离开卡但气泡要留、打出瞬间要立刻收，与指针事件并不一致）。</summary>
    public sealed class UI_HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("触发模式：Pointer = 组件自己接指针事件（悬停即弹、移出即收）；External = 不响应指针，由调用方调 Show()/Hide()")] [SerializeField]
        private ETipTrigger _trigger = ETipTrigger.Pointer;

        [Tooltip("内容是否为静态：勾上即用下面两串，运行期无需注入；不勾则由宿主调 SetContents 注入")] [SerializeField]
        private bool _isStaticContents;

        [ShowIf("_isStaticContents"), SerializeField]
        private string _tipName;

        [ShowIf("_isStaticContents"), SerializeField]
        private string _tipDescription;

        [Tooltip("气泡起点：本对象矩形上的九宫格对齐点，气泡从这里起算")] [SerializeField]
        private EUiAnchor _startAnchor = EUiAnchor.TopCenter;

        [Tooltip("起点的像素偏移，走本对象局部轴向（随本对象旋转与缩放）：如 (0,1) 即沿本对象\"上\"让开 1 像素")] [SerializeField]
        private Vector2 _tipOffsetPx = new(0f, 1f);

        [Tooltip("气泡以自身何处贴住起点：底沿贴锚点（BottomCenter）整列向上长，顶沿与居中则向下长")] [SerializeField]
        private EUiAnchor _tipAnchor = EUiAnchor.BottomCenter;

        [Tooltip("气泡挂载父级：空 = 挂弹层宿主自身（不随本对象缩放移动）；拖自己 = 随本对象抬起、缩放与拖动（卡牌即如此）")] [SerializeField]
        private Transform _tipParent;

        // 弹层宿主：全场景唯一（场景对象 Canvas_Pop/PopTextRoot）。预制体拖不到它、池化生成的对象也拿不到容器注入，
        // 故首次用时懒查一次、全局共享；读到假 null（宿主被销毁或换了场景）重查
        private static UI_PopTextRoot _host;

        private readonly List<(string Name, string Description)> _contents = new();
        private bool _shown;

        // 静态内容在装配时一次性预填；动态内容由宿主在数据变化时注入（两串都空即当作无内容）
        private void Awake()
        {
            if (!_isStaticContents) return;
            if (string.IsNullOrEmpty(_tipName) && string.IsNullOrEmpty(_tipDescription)) return;
            _contents.Add((_tipName, _tipDescription));
        }

        // 宿主被禁用（槽位与图标还池、卡牌回收、面板关激活）时 Exit 不来，兜底收掉悬着的气泡；两种触发模式同样兜底
        private void OnDisable() => Hide();

        #region 对外入口：显隐与内容注入（指针模式由本组件的指针事件调用，外部驱动模式由调用方调用）

        /// <summary>弹出说明气泡：内容为空不弹；重复调用按当前内容重刷整列。</summary>
        public void Show()
        {
            if (_contents.Count == 0) return;
            _shown = true;
            RectTransform rect = (RectTransform)transform;
            Vector3 anchor = UiGeometry.AnchorPoint(rect, _startAnchor) +
                             rect.TransformVector(new Vector3(_tipOffsetPx.x, _tipOffsetPx.y, 0f));
            Host.ShowTips(this, _contents, anchor, _tipAnchor, _tipParent);
        }

        /// <summary>收起自己发起的那一列（已被别的发起者顶掉时不做事）。</summary>
        public void Hide()
        {
            if (!_shown) return;
            _shown = false;
            if (_host) _host.HideTips(this);
        }

        /// <summary>注入单条内容：数据每变一次推一次；此刻正显示则按新内容立即重刷。</summary>
        public void SetContents(string name_, string description_)
        {
            _contents.Clear();
            _contents.Add((name_, description_));
            RefreshIfShown();
        }

        /// <summary>注入整列内容（多条按顺序叠列）：同上，正显示则立即重刷。</summary>
        public void SetContents(IReadOnlyList<(string Name, string Description)> contents_)
        {
            _contents.Clear();
            for (int i = 0; i < contents_.Count; i++) _contents.Add(contents_[i]);
            RefreshIfShown();
        }

        #endregion

        #region 指针事件：默认模式下悬停即弹、移出即收（外部驱动模式一律忽略）

        public void OnPointerEnter(PointerEventData eventData_) => ShowPointerDriven();

        public void OnPointerExit(PointerEventData eventData_) => HidePointerDriven();

        #endregion

        // 懒查弹层宿主：找不到即场景缺弹层，直接报错（对齐工程"找不到就 throw"的装配口径）
        private static UI_PopTextRoot Host
        {
            get
            {
                if (!_host) _host = FindFirstObjectByType<UI_PopTextRoot>();
                if (!_host) throw new InvalidOperationException("[UI_HoverTip] 场景里找不到 UI_PopTextRoot，说明气泡没有弹层宿主可用");
                return _host;
            }
        }

        private void ShowPointerDriven()
        {
            if (_trigger != ETipTrigger.Pointer) return;
            Show();
        }

        private void HidePointerDriven()
        {
            if (_trigger != ETipTrigger.Pointer) return;
            Hide();
        }

        // 正显示时按当前内容重刷（内容变了而气泡还开着：如状态层数刷新、卡牌重绑）
        private void RefreshIfShown()
        {
            if (_shown) Show();
        }
    }
}
