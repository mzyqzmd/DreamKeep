using System;
using System.Collections.Generic;
using _DreamKeep.Utility;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>说明气泡发起器：需要"指针悬停弹说明气泡"的对象挂上它即可用——内容可提前在 Inspector 配置（静态），
    /// 也可由宿主在数据变化时注入（动态，单条或整列）；气泡视图与弹层宿主只管"实例取还与唯一列守卫"（UI_PopTextRoot），
    /// 本组件接管"起排点 + 朝向 + 间距 + 逐条累排"的整列定位，只回答"谁被悬停、弹什么、弹在哪"。
    /// 触发分两种模式：默认自己接 UGUI 指针事件（挂上即用）；外部驱动模式不响应指针，由调用方在它判定出的时刻调 Show/Hide
    /// （手牌上的卡牌即这一种——卡的"悬停"由手牌区每帧射线裁决，瞄准中指针离开卡但气泡要留、打出瞬间要立刻收，与指针事件并不一致）。</summary>
    public sealed class UI_HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("触发模式：Pointer = 组件自己接指针事件（悬停即弹、移出即收）；External = 不响应指针，由调用方调 Show()/Hide()")] [SerializeField]
        private ETipTrigger _trigger = ETipTrigger.Pointer;

        [Tooltip("气泡起点：本对象矩形上的九宫格对齐点，气泡从这里起算")] [SerializeField]
        private EUiAnchor _startAnchor = EUiAnchor.TopCenter;

        [Tooltip("气泡以自身何处贴住起点：底沿贴锚点（BottomCenter）整列向上长，顶沿与居中则向下长")] [SerializeField]
        private EUiAnchor _tipAnchor = EUiAnchor.BottomCenter;

        [Tooltip("起点的像素偏移，走本对象局部轴向（随本对象旋转与缩放）：如 (0,1) 即沿本对象\"上\"让开 1 像素")] [SerializeField]
        private Vector2 _tipOffsetPx = new(0f, 1f);

        [Tooltip("气泡挂载父级：空 = 挂弹层宿主自身（不随本对象缩放移动）；拖自己 = 随本对象抬起、缩放与拖动（卡牌即如此）")] [SerializeField]
        private Transform _tipParent;

        [Tooltip("叠列气泡之间的垂直间距（画布像素）：多条内容（如卡牌词条）一条压一条地往反方向排")] [SerializeField]
        private float _stackSpacingPx = 12f;

        [Tooltip("内容是否为静态：勾上即用下面两串，运行期无需注入；不勾则由宿主调 SetContents 注入")] [SerializeField]
        private bool _isStaticContents;

        [ShowIf("_isStaticContents"), SerializeField]
        private string _tipName;

        [ShowIf("_isStaticContents"), SerializeField, TextArea(3, 10)]
        private string _tipDescription;


        private readonly List<(string Name, string Description)> _contents = new();

        private bool _shown;

        // 弹层宿主：全场景唯一（场景对象 Canvas_Pop/PopTextRoot）。预制体拖不到它、池化生成的对象也拿不到容器注入，
        // 故首次用时懒查一次、全局共享；读到假 null（宿主被销毁或换了场景）重查
        private static UI_PopTextRoot _host;

        // 懒查弹层宿主：找不到即场景缺弹层，直接报错
        private static UI_PopTextRoot Host
        {
            get
            {
                if (!_host) _host = FindFirstObjectByType<UI_PopTextRoot>();
                return !_host
                    ? throw new InvalidOperationException("[UI_HoverTip] 场景里找不到 UI_PopTextRoot，说明气泡没有弹层宿主可用")
                    : _host;
            }
        }

        // 静态内容在装配时一次性预填；动态内容由宿主在数据变化时注入（两串都空即当作无内容）
        private void Awake()
        {
            if (!_isStaticContents) return;
            _contents.Add((_tipName, _tipDescription));
        }

        // 宿主被禁用（槽位与图标还池、卡牌回收、面板关激活）时 Exit 不来，兜底收掉悬着的气泡；两种触发模式同样兜底
        private void OnDisable() => Hide();

        #region 对外入口：显隐与内容注入（指针模式由本组件的指针事件调用，外部驱动模式由调用方调用）

        /// <summary>弹出说明气泡：内容为空不弹；重复调用按当前内容重刷整列（起排点 + 朝向 + 间距 + 逐条累排都在此完成）。</summary>
        public void Show()
        {
            if (_contents.Count == 0) return;
            _shown = true;
            Vector3 anchor = UiGeometry.AnchorPoint((RectTransform)transform, _startAnchor) +
                             transform.TransformVector(new Vector3(_tipOffsetPx.x, _tipOffsetPx.y, 0f));
            Vector2 pivot = UiGeometry.Pivot(_tipAnchor);
            Transform mount = _tipParent ? _tipParent : Host.transform;
            IReadOnlyList<UI_DescriptionPopTip> bubbles = Host.AcquireColumn(this, _contents.Count);
            float step = pivot.y > 0f ? -1f : 1f; // 底沿贴锚点（pivot.y 为 0）向上长，顶/居中则向下长
            for (int i = 0; i < bubbles.Count; i++)
                anchor = PlaceBubble(bubbles[i], pivot, mount, anchor, step, _stackSpacingPx, _contents[i]);
        }

        // 摆一张气泡：写内容→定 pivot→挂到挂载点并置顶→按世界锚点落位→强制重排拿当帧高度→把下一条锚点退到"当前高度 + 间距"处
        private static Vector3 PlaceBubble(UI_DescriptionPopTip bubble_, Vector2 pivot_, Transform mount_,
            Vector3 anchor_, float step_, float spacingPx_, (string Name, string Description) content_)
        {
            RectTransform bRect = (RectTransform)bubble_.transform;
            bubble_.SetContent(content_.Name, content_.Description);
            bRect.pivot = pivot_;
            bubble_.transform.SetParent(mount_, false);
            bubble_.transform.SetAsLastSibling();
            bRect.position = anchor_;
            LayoutRebuilder.ForceRebuildLayoutImmediate(bRect);
            return bRect.TransformPoint(new Vector3(0f, step_ * (bRect.rect.height + spacingPx_), 0f));
        }


        /// <summary>收起自己发起的那一列（已被别的发起者顶掉时不做事）。</summary>
        public void Hide()
        {
            if (!_shown) return;
            _shown = false;
            if (_host) _host.ReleaseColumn(this);
        }

        #endregion

        #region 设置tip内容

        /// <summary>注入单条内容：数据每变一次推一次；此刻正显示则按新内容立即重刷。</summary>
        public void SetContents(string name_, string description_)
        {
            _contents.Clear();
            _contents.Add((name_, description_));
            if (_shown) Show();
        }

        /// <summary>注入整列内容（多条按顺序叠列）：同上，正显示则立即重刷。</summary>
        public void SetContents(IReadOnlyList<(string Name, string Description)> contents_)
        {
            _contents.Clear();
            for (int i = 0; i < contents_.Count; i++) _contents.Add(contents_[i]);
            if (_shown) Show();
        }

        #endregion

        #region 指针事件：默认模式下悬停即弹、移出即收（外部驱动模式一律忽略）

        public void OnPointerEnter(PointerEventData eventData_)
        {
            if (_trigger != ETipTrigger.Pointer) return;
            Show();
        }

        public void OnPointerExit(PointerEventData eventData_)
        {
            if (_trigger != ETipTrigger.Pointer) return;
            Hide();
        }

        #endregion
    }
}