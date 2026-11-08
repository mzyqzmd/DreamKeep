using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using _DreamKeep.ConfigData;
using UnityEngine.UI;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>地图编排器：Build 一次性生成节点视图与点串连线，Refresh 按域状态重绑（每次打开，不重建实例）。
    /// 布局以 Node 层自身矩形为唯一坐标系：先古贴纸面底、Boss 贴纸面顶且都取正中，中间行在两端之间均分，
    /// 同层再加确定性错落；节点、连线、圆环三处共用同一份层内坐标缓存，选点经 NodeClicked 上报。</summary>
    public class UI_MapView : MonoBehaviour
    {
        [Header("生成件")] [SerializeField] private UI_StaticMapNode _staticNodePrefab;
        [SerializeField] private UI_BossMapNode _bossNodePrefab;
        [SerializeField] private UI_MapLine _linePrefab;
        [SerializeField] private UI_MapCircle _circlePrefab;

        [Header("层级（两层必须同尺寸）")] [SerializeField]
        private RectTransform _lineLayer;

        [SerializeField] private RectTransform _nodeLayer;

        [Header("布局")] [Tooltip("普通节点左右留白（留够描边与错落不越出纸面实心区）")] [SerializeField]
        private Vector2 _padding = new(190f, 190f);

        [Tooltip("静态节点缩放：图标分辨率 278 压到约 200")] [SerializeField]
        private float _nodeScale = 0.72f;

        [Tooltip("先古之民缩放：普通节点的两倍（约 400×400）")] [SerializeField]
        private float _ancientScale = 1.44f;

        [Tooltip("Boss 缩放：1991×1699 压到约 537×459")] [SerializeField]
        private float _bossScale = 0.27f;

        [Tooltip("特殊节点（先古/Boss）边缘距纸面实心边的留白")] [SerializeField]
        private float _specialMargin = 200f;

        [Tooltip("先古之民与第一排之间的额外留白（其余行距等比压缩）")] [SerializeField]
        private float _ancientGap = 150f;

        [Tooltip("Boss 与最后一排之间的额外留白（其余行距等比压缩）")] [SerializeField]
        private float _bossGap = 250f;

        [Tooltip("同层错落幅度（像素，仅该层有 2 个以上节点时生效）")] [SerializeField]
        private Vector2 _staggerPx = new(20f, 16f);

        /// <summary>可走节点被点击：编排层据域节点切段。</summary>
        public event Action<MapNode> NodeClicked;

        /// <summary>地图全量重建（Build 尾声，空图路径同样广播）：限本局的内容（如玩家墨迹）据此清空。</summary>
        public event Action MapRebuilt;

        private MapGraph _graph;
        private Coroutine _selectRoutine;
        private bool _laidOut;
        private ScrollRect _scroll;
        private MapNode _scrolledFor; // 上次已把滚动对准的节点：同一节点重复开图不打断玩家的手动滚动
        private readonly Dictionary<MapNode, UI_MapNode> _views = new Dictionary<MapNode, UI_MapNode>();
        private readonly Dictionary<MapNode, Vector2> _positions = new Dictionary<MapNode, Vector2>(); // 层坐标（左上原点、y 向下）
        private readonly List<UI_MapLine> _lines = new List<UI_MapLine>();
        private UI_MapCircle _circle;
        private Rect _mapRect;
        private int[] _rowCounts;

        // 失活会中断协程：引用必须同步清掉，否则重开地图后 OnNodeClicked 会被残值永远拦截
        private void OnDisable() => _selectRoutine = null;

        [Inject]
        public void Construct(RunState run_) => Build(run_.Map);

        /// <summary>全量建图：网格节点 + Boss；位置与连线等首次打开地图（面板已激活）时再摆。</summary>
        public void Build(MapGraph graph_)
        {
            _graph = graph_;
            ClearAll();
            MapRebuilt?.Invoke(); // 换局重建即换一局：墨迹等"限本局"内容据此清空
            if (_graph == null) return;

            _rowCounts = new int[_graph.Rows];
            for (int col = 0; col < _graph.Width; col++)
            for (int row = 0; row < _graph.Rows; row++)
                if (_graph.GetNode(col, row) != null)
                    _rowCounts[row]++;

            for (int col = 0; col < _graph.Width; col++)
            for (int row = 0; row < _graph.Rows; row++)
                CreateView(_graph.GetNode(col, row));
            CreateView(_graph.BossNode);
        }

        /// <summary>按域状态重绑全部节点（含 Unknown 揭示换装）、重摆圆环，并把滚动对准当前位置；不重建实例。</summary>
        public void Refresh()
        {
            if (_graph == null) return;
            if (!_laidOut) LayoutAll(); // 首次打开：面板已激活，先把 Content 撑开再取层矩形
            foreach (KeyValuePair<MapNode, UI_MapNode> pair in _views)
                pair.Value.Bind(pair.Key, pair.Key.State);
            RefreshCircle();
            ScrollToCurrentNode();
        }

        // 装配期面板未激活时 Content 高度还没被 ContentSizeFitter 撑开（读到的是 1340 这类残值），
        // 所以布局推迟到首次打开、Canvas 强制刷新之后再算，否则整张图会被压进顶部三分之一
        private void LayoutAll()
        {
            _laidOut = true;
            Canvas.ForceUpdateCanvases();
            _mapRect = _nodeLayer.rect;
            LayoutPositions(); // 必须晚于全部 Bind：先古/Boss 的半高来自 Bind 后写入的图标尺寸
            CreateLines();
        }

        private void ClearAll()
        {
            if (_selectRoutine != null)
            {
                StopCoroutine(_selectRoutine);
                _selectRoutine = null;
            }

            _laidOut = false;
            _scrolledFor = null;
            foreach (UI_MapNode view in _views.Values) Destroy(view.gameObject);
            _views.Clear();
            _positions.Clear();
            foreach (UI_MapLine line in _lines) Destroy(line.gameObject);
            _lines.Clear();
            HideCircle();
            if (_circle != null) Destroy(_circle.gameObject);
            _circle = null;
        }

        private void CreateView(MapNode node_)
        {
            if (node_ == null) return;
            UI_MapNode prefab = node_.PointType == EMapPointType.Boss ? _bossNodePrefab : _staticNodePrefab;
            UI_MapNode view = Instantiate(prefab, _nodeLayer, false);
            RectTransform rect = (RectTransform)view.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            // 缩放必须先于 Bind：UI_HoverScaler 的基准缩放是在首次激活时取自当前 localScale
            rect.localScale = Vector3.one * ScaleOf(node_);
            view.Clicked += OnNodeClicked;
            view.Bind(node_, node_.State);
            _views[node_] = view;
        }

        private float ScaleOf(MapNode node_) => node_.PointType switch
        {
            EMapPointType.Boss => _bossScale,
            EMapPointType.Ancient => _ancientScale,
            _ => _nodeScale,
        };

        // 点节点：先在目标节点播一次圆环，播完才上报切段；等待期间重复点击作废（协程随面板失活自动中断）
        private void OnNodeClicked(MapNode node_)
        {
            if (_selectRoutine != null) return;
            if (!_positions.TryGetValue(node_, out Vector2 pos)) return;
            if (_circle == null) _circle = Instantiate(_circlePrefab, _nodeLayer, false);
            _circle.PlayAt(_nodeLayer, ToAnchored(pos), ViewSizeOf(_views[node_]));
            _selectRoutine = StartCoroutine(RaiseAfterCircle(node_));
        }

        private IEnumerator RaiseAfterCircle(MapNode node_)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, _circle.PlayDuration));
            _selectRoutine = null;
            NodeClicked?.Invoke(node_);
        }

        // 父子边各画一条点串：线中点摆两节点中点、+y 轴对齐"子→父"视觉方向、长度即两点距
        private void CreateLines()
        {
            foreach (KeyValuePair<MapNode, UI_MapNode> pair in _views)
            foreach (MapNode child in pair.Key.Children)
            {
                if (!_positions.TryGetValue(child, out Vector2 to) ||
                    !_positions.TryGetValue(pair.Key, out Vector2 from)) continue;
                UI_MapLine line = Instantiate(_linePrefab, _lineLayer, false);
                RectTransform rect = (RectTransform)line.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = ToAnchored((from + to) * 0.5f);
                rect.localRotation =
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(from.y - to.y, to.x - from.x) * Mathf.Rad2Deg - 90f);
                line.SetLength(Vector2.Distance(from, to));
                _lines.Add(line);
            }
        }

        #region 布局：特殊节点居中贴纸面、中间行反算行距、同层确定性错落

        // 层内坐标（左上角为原点、y 向下）：先古底边距纸面底 _specialMargin、Boss 顶边距纸面顶 _specialMargin，
        // 两者横向恒取层正中；其余行在两端中心之间均分（先古到第一排多留 _specialGap，行距等比压缩）
        private void LayoutPositions()
        {
            UI_MapNode bossView = null;
            UI_MapNode ancientView = null;
            foreach (KeyValuePair<MapNode, UI_MapNode> pair in _views)
            {
                if (pair.Key.PointType == EMapPointType.Boss) bossView = pair.Value;
                else if (pair.Key.PointType == EMapPointType.Ancient) ancientView = pair.Value;
            }

            float levels = _graph.Rows + 1f; // 行 0..Rows（Boss 在 Rows）
            float paperBottom = _mapRect.height;
            float topY = _specialMargin + HalfHeight(bossView);
            float bottomY = paperBottom - _specialMargin - HalfHeight(ancientView);
            float pitchY = (bottomY - topY - _ancientGap - _bossGap) / Mathf.Max(1f, levels - 1f);
            float pitchX = (_mapRect.width - _padding.x * 2f) / Mathf.Max(1, _graph.Width - 1);
            float centerX = _mapRect.width * 0.5f;

            foreach (KeyValuePair<MapNode, UI_MapNode> pair in _views)
            {
                MapNode node = pair.Key;
                bool special = node.PointType is EMapPointType.Ancient or EMapPointType.Boss;
                Vector2 jitter = special ? Vector2.zero : StaggerOf(node);
                float half = HalfHeight(pair.Value);
                float x = special
                    ? centerX
                    : Mathf.Clamp(_padding.x + node.Coord.Col * pitchX + jitter.x, _padding.x,
                        _mapRect.width - _padding.x);
                // 两端特殊节点不参与行公式：Boss 恒贴 topY、先古恒贴 bottomY，中间行扣掉两侧留白后均分
                float y = node.PointType == EMapPointType.Boss ? topY
                    : node.Coord.Row == 0 ? bottomY
                    : bottomY - _ancientGap - node.Coord.Row * pitchY + jitter.y;
                y = Mathf.Clamp(y, half, paperBottom - half);
                _positions[node] = new Vector2(x, y);
                ((RectTransform)pair.Value.transform).anchoredPosition = ToAnchored(new Vector2(x, y));
            }
        }

        // 节点实际半高：静态节点的尺寸由 Bind 按图标分辨率写入，必须先 Bind 再取
        private static float HalfHeight(UI_MapNode view_) =>
            view_ == null ? 0f : ((RectTransform)view_.transform).rect.height * view_.transform.localScale.y * 0.5f;

        // 只有同层存在多个节点时才错落：自成一层的起点/Boss/独苗节点保持正中，避免看起来像偏移错误
        private Vector2 StaggerOf(MapNode node_)
        {
            if (_rowCounts == null || node_.Coord.Row >= _rowCounts.Length || _rowCounts[node_.Coord.Row] < 2)
                return Vector2.zero;
            int col = node_.Coord.Col;
            int row = node_.Coord.Row;
            return new Vector2((Hash01(col, row, 17) * 2f - 1f) * _staggerPx.x,
                (Hash01(col, row, 91) * 2f - 1f) * _staggerPx.y);
        }

        // 纯函数哈希：同一 (col,row) 恒同值，节点与连线才能算出同一个位置（不得用 UnityEngine.Random）
        private static float Hash01(int col_, int row_, int salt_)
        {
            unchecked
            {
                int hash = col_ * 73856093 ^ row_ * 19349663 ^ salt_ * 83492791;
                return (hash & 0x7FFFFFFF) % 1000 / 999f;
            }
        }

        #endregion

        private Vector2 ToAnchored(Vector2 layerPos_) => new Vector2(layerPos_.x - _mapRect.width * 0.5f, -layerPos_.y);

        // 节点在层内的实际显示尺寸：rect 是图标分辨率，还要乘上节点缩放
        private static Vector2 ViewSizeOf(UI_MapNode view_)
        {
            RectTransform rect = (RectTransform)view_.transform;
            return new Vector2(rect.rect.width, rect.rect.height) * view_.transform.localScale.x;
        }

        #region 当前位置圆环：先古与 Boss 不出圈，非先古/Boss 的当前节点播一次停末帧

        private void RefreshCircle()
        {
            MapNode current = _graph.CurrentNode;
            if (current == null || current.PointType is EMapPointType.Ancient or EMapPointType.Boss ||
                !_positions.TryGetValue(current, out Vector2 pos))
            {
                HideCircle();
                return;
            }

            if (_circle == null) _circle = Instantiate(_circlePrefab, _nodeLayer, false);
            // 重开地图只摆位不重播
            if (_views.TryGetValue(current, out UI_MapNode view)) _circle.ShowAt(_nodeLayer, ToAnchored(pos), ViewSizeOf(view));
        }

        private void HideCircle()
        {
            if (_circle != null) _circle.Hide();
        }

        #endregion

        #region 打开定位：滚动对准当前节点（玩家从下往上爬，开图就该先看到自己）

        // 玩家从下往上爬：开图把视口对到当前位置——开局时当前节点在地图最底部，即"拖到最下面"；
        // 爬上去之后跟着爬升位置走。同一节点停留期间反复开图不重置，保留玩家的手动滚动
        private void ScrollToCurrentNode()
        {
            MapNode current = _graph.CurrentNode;
            if (current == null || _scrolledFor == current) return;
            if (!_positions.TryGetValue(current, out Vector2 pos)) return;
            _scrolledFor = current;

            _scroll ??= GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases(); // ScrollRect 的 bounds 要等布局刷新完才准：内容 4320、视口 1365
            float scrollable = _scroll.content.rect.height - _scroll.viewport.rect.height;
            if (scrollable <= 0f) return;
            // 当前节点摆在视口正中，顶/底越界时钳住——开局自然就贴到地图最下面
            float offsetFromTop = Mathf.Clamp(pos.y - _scroll.viewport.rect.height * 0.5f, 0f, scrollable);
            _scroll.verticalNormalizedPosition = 1f - offsetFromTop / scrollable;
        }

        #endregion

        #region 图例悬停高亮：按节点展示类型全量点亮/还原

        /// <summary>按展示类型切换高亮："？"节点只归"事件"图例、已揭示的按揭示后类型归队（口径见 UI_MapNode.ShownType）。</summary>
        public void HighlightType(EMapPointType type_, bool on_)
        {
            foreach (KeyValuePair<MapNode, UI_MapNode> pair in _views)
                if (pair.Value.ShownType == type_) pair.Value.SetHighlighted(on_);
        }

        #endregion
    }
}