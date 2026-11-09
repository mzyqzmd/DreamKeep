using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>地图标注墨迹控制器：面板开着且指针在纸面视口内时，右键拖动按采样间距生成连续折线墨迹、
    /// 中键把橡皮半径内的采样点删掉并把剩余线切成多段（只动玩家笔迹，不碰装饰线与节点）。笔迹锚定墨层随滚动整体移动。
    /// 快捷键恒可用；GlowIcon 由 ActiveTool 单一真源裁决（手势优先、全局只亮一个）；画笔/橡皮模式互斥，
    /// 进入后左键接管绘制/擦除（拦截层吃掉拖拽、地图不滚），右键/中键快捷键手势结束回空模式、模式内左键可连续操作。
    /// 墨迹限本局：订阅 MapRebuilt，地图重建即全清。</summary>
    public sealed class UI_MapInk : MonoBehaviour, ISaveable
    {
        private const int LinePoolCapacity = 1024; // 折线视图池缓存上限：超限归还按池契约直接销毁
        private const int TotalPointCap = 20000; // 笔迹总点数硬上限：存档体积与写盘耗时的护栏
        private const string KeyPoints = "ink.points";
        private const string KeyLengths = "ink.lengths";

        [Tooltip("墨迹视觉配置（颜色/线宽/采样间距/橡皮半径）")] [SerializeField]
        private SO_MapInkVisual _visual;

        [Tooltip("折线视图预制体（UI_MapInkLine，raycastTarget 关）")] [SerializeField]
        private UI_MapInkLine _linePrefab;

        [Tooltip("墨层（与 Node 层同矩形、置顶）：折线视图统一挂这里随滚动移动")] [SerializeField]
        private RectTransform _inkLayer;

        [Tooltip("左键拦截层（墨层上的透明 Image）：模式激活时开射线，吃掉拖拽事件使地图不被左键拖走")] [SerializeField]
        private Image _inputBlocker;

        /// <summary>画笔模式：与橡皮模式互斥；激活时左键绘制。</summary>
        public bool PenMode { get; private set; }

        /// <summary>橡皮模式：与画笔模式互斥；激活时左键擦除。</summary>
        public bool EraserMode { get; private set; }

        /// <summary>画笔手势进行中（右键，或画笔模式下的左键）。</summary>
        public bool PenGesture => _drawing != null;

        /// <summary>橡皮手势进行中（中键，或橡皮模式下的左键）。</summary>
        public bool EraserGesture => _erasing;

        /// <summary>当前工具（GlowIcon 单一真源）：手势优先，其次按钮模式，都没有即空模式。</summary>
        public EMapInkTool ActiveTool => PenGesture ? EMapInkTool.Pen
            : EraserGesture ? EMapInkTool.Eraser
            : PenMode ? EMapInkTool.Pen
            : EraserMode ? EMapInkTool.Eraser
            : EMapInkTool.None;

        /// <summary>工具态变化广播：工具栏据此按 ActiveTool 刷 GlowIcon。</summary>
        public event Action ModeChanged;

        private readonly List<InkStroke> _strokes = new();
        private MonoObjectPool<UI_MapInkLine> _linePool;
        private Canvas _canvas;
        private UI_MapView _view;
        private InkStroke _drawing; // 进行中的一笔：null 即未在画
        private bool _erasing;
        private bool _drawEndsPenMode; // 本笔由右键起笔：松开时回空模式（模式内左键起笔不退）
        private int _totalPoints;
        private float[] _restorePoints; // 读档待建点列：保留供 MapRebuilt 重建，换局时随本对象销毁
        private int[] _restoreLengths;
        private bool _restoreApplied; // 防止同一份待建点列被重复叠笔

        // 池出生父级为墨层：视图矩形与点列由笔画自己摆
        private MonoObjectPool<UI_MapInkLine> LinePool =>
            _linePool ??= new MonoObjectPool<UI_MapInkLine>(_inkLayer, LinePoolCapacity);

        // Overlay 画布无需相机，其余按画布 worldCamera 换算
        private Camera CastCamera
        {
            get
            {
                _canvas ??= GetComponentInParent<Canvas>(true);
                return _canvas.worldCamera;
            }
        }

        [Inject]
        public void Construct(UI_MapView view_)
        {
            _view = view_;
            _view.MapRebuilt += OnMapRebuilt; // 换局重建即清墨；读档待建点列在清空后幂等重建
        }

        private void OnDestroy()
        {
            if (_view != null) _view.MapRebuilt -= OnMapRebuilt;
        }

        private void OnMapRebuilt()
        {
            ClearAllStrokes();
            _restoreApplied = false; // 视图已清空，允许按同一份待建点列重建
            ApplyRestoredStrokes();
        }

        // 面板收起兜底：按键的 Up 不会来，收掉进行中手势并回空模式（事件同步刷 GlowIcon）
        private void OnDisable()
        {
            AbortActiveGesture();
            ClearModesToEmpty();
        }

        private void Update()
        {
            if (!PointerInput.TryGetScreenPosition(out Vector2 pointerScreen)) return;
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(_inkLayer, pointerScreen, CastCamera);
            bool leftDown = PointerInput.LeftPressedThisFrame;
            bool leftUp = PointerInput.LeftReleasedThisFrame;

            // 画笔：右键恒为快捷键；画笔模式下左键接管（松左键不退模式，仍可连续画）
            if (PointerInput.RightPressedThisFrame) BeginDraw(pointerScreen, inside, true);
            else if (PenMode && leftDown) BeginDraw(pointerScreen, inside, false);
            else if (_drawing != null && (PointerInput.RightHeld || (PenMode && PointerInput.LeftHeld)))
                ExtendDraw(pointerScreen, inside);
            if (PointerInput.RightReleasedThisFrame || (PenMode && leftUp)) EndDraw();

            // 橡皮：中键恒为快捷键；橡皮模式下左键接管
            if ((PointerInput.MiddlePressedThisFrame || (EraserMode && leftDown)) && inside)
            {
                _erasing = true;
                NotifyStateChanged();
            }
            if (_erasing && (PointerInput.MiddleHeld || (EraserMode && PointerInput.LeftHeld)) && inside)
                EraseAt(pointerScreen);
            if (PointerInput.MiddleReleasedThisFrame) EndErase(true);
            else if (EraserMode && leftUp) EndErase(false);
        }

        #region 对外入口：模式与清空（UI_MapToolBar 唯一依赖面）

        /// <summary>画笔模式开关：进入时挤掉橡皮模式（互斥），变化才广播，同值调用无副作用。</summary>
        public void SetPenMode(bool on_)
        {
            if (PenMode == on_) return;
            if (on_) EraserMode = false; // 直接写字段避免递归：两模式只广播一次
            PenMode = on_;
            NotifyStateChanged();
        }

        /// <summary>橡皮模式开关：进入时挤掉画笔模式，同上。</summary>
        public void SetEraserMode(bool on_)
        {
            if (EraserMode == on_) return;
            if (on_) PenMode = false;
            EraserMode = on_;
            NotifyStateChanged();
        }

        public void TogglePenMode() => SetPenMode(!PenMode);

        public void ToggleEraserMode() => SetEraserMode(!EraserMode);

        /// <summary>清空全部墨迹（清空按钮与换局重建共用）：进行中手势一并作废。</summary>
        public void ClearAllStrokes()
        {
            AbortActiveGesture();
            for (int i = 0; i < _strokes.Count; i++) _strokes[i].Clear(LinePool);
            _strokes.Clear();
            _totalPoints = 0;
        }

        #endregion

        #region 存档接入：笔迹点列拍平成平行表，读档经 MapRebuilt 订阅链幂等重建

        public void Capture(ISaveWriter writer_)
        {
            List<float> points = new(_totalPoints * 2);
            int[] lengths = new int[_strokes.Count];
            for (int i = 0; i < _strokes.Count; i++)
            {
                IReadOnlyList<Vector2> line = _strokes[i].Points;
                lengths[i] = line.Count;
                for (int p = 0; p < line.Count; p++)
                {
                    points.Add(line[p].x);
                    points.Add(line[p].y);
                }
            }

            writer_.Write(KeyPoints, points.ToArray());
            writer_.Write(KeyLengths, lengths);
        }

        public void Restore(ISaveReader reader_)
        {
            _restorePoints = reader_.Read<float[]>(KeyPoints, Array.Empty<float>());
            _restoreLengths = reader_.Read<int[]>(KeyLengths, Array.Empty<int>());
            _restoreApplied = false;
            ApplyRestoredStrokes(); // 装配期 MapRebuilt 若已早于 Restore 触发，此处直接补建
        }

        // 幂等标记保证同一份待建点列只叠一次；点列本身保留，供晚到的 MapRebuilt 重建
        private void ApplyRestoredStrokes()
        {
            if (_restorePoints == null || _restoreApplied) return;
            _restoreApplied = true;
            float[] points = _restorePoints;
            int[] lengths = _restoreLengths ?? Array.Empty<int>();

            int cursor = 0;
            foreach (int length in lengths)
            {
                int floats = Mathf.Max(0, length) * 2;
                if (cursor + floats > points.Length) break;
                List<Vector2> line = new(length);
                for (int p = 0; p < floats; p += 2)
                    line.Add(new Vector2(points[cursor + p], points[cursor + p + 1]));
                cursor += floats;
                if (line.Count < 2 || _totalPoints + line.Count > TotalPointCap) continue;
                _strokes.Add(InkStroke.FromExactPoints(line, _inkLayer, LinePool, _linePrefab, _visual));
            }

            RecountPoints();
        }

        // 结构变化（擦除切段、收笔、作废手势、读档重建）后重算，避免增量统计与切段结果脱节
        private void RecountPoints()
        {
            int total = _drawing == null ? 0 : _drawing.Points.Count;
            for (int i = 0; i < _strokes.Count; i++) total += _strokes[i].Points.Count;
            _totalPoints = total;
        }

        #endregion

        // 起笔：只在纸面视口内生效，首点立即落下保证单击也起笔
        private void BeginDraw(Vector2 mousePos_, bool inside_, bool endsMode_)
        {
            if (_drawing != null) AbortActiveGesture(); // 异常输入（上一笔没收到 Up）兜底：先回收，避免残留孤儿视图
            if (!inside_ || _totalPoints >= TotalPointCap || !TryToInkLayer(mousePos_, out Vector2 pos)) return;
            _drawing = new InkStroke();
            if (_drawing.AddPoint(pos, _inkLayer, LinePool, _linePrefab, _visual)) _totalPoints++;
            _drawEndsPenMode = endsMode_;
            NotifyStateChanged(); // 手势开始：点亮画笔 GlowIcon
        }

        // 行笔：拖出视口即停采样（留缺口），拖回继续
        private void ExtendDraw(Vector2 mousePos_, bool inside_)
        {
            if (!inside_ || _totalPoints >= TotalPointCap || !TryToInkLayer(mousePos_, out Vector2 pos)) return;
            if (_drawing.AddPoint(pos, _inkLayer, LinePool, _linePrefab, _visual)) _totalPoints++;
        }

        // 收笔：够两点的并入笔迹列表（供橡皮擦），单点空笔还池丢弃；右键起笔的手势结束回空模式
        private void EndDraw()
        {
            if (_drawing == null) return;
            if (_drawing.IsEmpty) _drawing.Clear(LinePool);
            else _strokes.Add(_drawing);
            _drawing = null;
            RecountPoints();
            bool endsMode = _drawEndsPenMode;
            _drawEndsPenMode = false;
            NotifyStateChanged(); // 手势结束：熄灭画笔 GlowIcon
            if (endsMode) ClearModesToEmpty();
        }

        // 擦除手势收尾：中键松开回空模式；橡皮模式下的左键松开保持模式可连续擦
        private void EndErase(bool endsMode_)
        {
            if (!_erasing) return;
            _erasing = false;
            NotifyStateChanged(); // 手势结束：熄灭橡皮 GlowIcon
            if (endsMode_) ClearModesToEmpty();
        }

        // 擦除：半径内采样点删除，剩余点按缺口切成多段独立线，每段新建一条笔迹
        private void EraseAt(Vector2 mousePos_)
        {
            if (!TryToInkLayer(mousePos_, out Vector2 pos)) return;
            float radius = _visual.EraseRadiusPx;
            for (int i = _strokes.Count - 1; i >= 0; i--)
            {
                IReadOnlyList<List<Vector2>> segments = _strokes[i].EraseAt(pos, radius, LinePool);
                if (segments == null) continue; // 没擦到点：原段保持不动
                _strokes.RemoveAt(i);
                for (int s = 0; s < segments.Count; s++)
                    _strokes.Add(InkStroke.FromExactPoints(segments[s], _inkLayer, LinePool, _linePrefab, _visual));
                RecountPoints();
            }
        }

        // 进行中手势作废（清空/收起兜底）：进行中的一笔直接弃掉
        private void AbortActiveGesture()
        {
            if (_drawing != null)
            {
                _drawing.Clear(LinePool);
                _drawing = null;
            }

            _erasing = false;
            _drawEndsPenMode = false;
            RecountPoints();
            NotifyStateChanged();
        }

        // 快捷键手势结束回空：两模式一起清，一次广播让 Glow 全灭、拦截层关闭（左键恢复拖地图）
        private void ClearModesToEmpty()
        {
            if (!PenMode && !EraserMode) return;
            PenMode = false;
            EraserMode = false;
            NotifyStateChanged();
        }

        // 状态收口：模式期间开拦截层（左键不拖地图），并让工具栏按 ActiveTool 刷 GlowIcon
        private void NotifyStateChanged()
        {
            if (_inputBlocker) _inputBlocker.raycastTarget = PenMode || EraserMode;
            ModeChanged?.Invoke();
        }

        // 屏幕点 → 墨层局部坐标：画与擦共用同一换算，笔迹才与指针所见自洽（滚动中换算随墨层世界位实时变化）
        private bool TryToInkLayer(Vector2 screenPos_, out Vector2 local_) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_inkLayer, screenPos_, CastCamera, out local_);
    }
}