using _DreamKeep.EngineUtility;
using _DreamKeep.Utility;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>单位命中框：挂在单位根节点上，指针进出时淡入淡出名牌与描边，并兼管目标提示点亮与箭头命中判定
    /// （判定用的就是本节点的矩形，与悬停范围同源）。被手牌、牌堆等上层界面压住不算悬停由 EventSystem 的层序负责；
    /// 配置不在这里，一律读所在 UI_Actor 的只读出口，所以组件挂上即生效、无需 Inspector 接线。</summary>
    public sealed class UI_ActorHoverBox : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string
            OutlineShaderName = "Spine/Outline/SkeletonGraphic"; // 描边 shader 变体：Inspector 里勾选 Outline 就是换到它

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        [Header("悬停反馈")] [SerializeField] private float _hoverFadeDuration = 0.12f;

        private UI_Actor _actor;
        private bool _targetTipShown;

        // 悬停反馈状态：描边材质副本只在首次悬停克隆、之后常驻，共享材质资产全程不写
        private Material _outlineMaterial;
        private Color _outlineColor;
        private bool _outlineUnavailable;
        private float _hoverFade;
        private Tween _hoverTween;
        private bool _hovered;
        private bool _nameShown;

        private void Awake() => _actor = GetComponentInParent<UI_Actor>();

        // 销毁即收掉悬停补间与描边副本
        private void OnDestroy()
        {
            _hoverTween?.Kill();
            if (_outlineMaterial) Destroy(_outlineMaterial);
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);

        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

        #region 目标提示与命中判定：由瞄准中间件 BattleTargetDirector 按"这一轮会命中的目标"调用

        /// <summary>显示或隐藏目标提示（状态没变就跳过，避免重复 SetActive）。</summary>
        public void ShowTargetTip(bool show_)
        {
            if (!_actor.HoverReticle || _targetTipShown == show_) return;
            _targetTipShown = show_;
            _actor.HoverReticle.gameObject.SetActive(show_);
        }

        /// <summary>世界点是否落在本单位命中区内：就是本节点的矩形，与指针悬停范围同源。</summary>
        public bool ContainsWorldPoint(Vector3 world_) => UiGeometry.Contains((RectTransform)transform, world_);

        #endregion

        #region 悬停演出：指针停在单位范围内时淡入名牌与描边

        /// <summary>悬停反馈总开关：显示时淡入名牌与描边，移出时反向淡出；重复同一状态不做任何事。</summary>
        public void SetHovered(bool show_)
        {
            if (_hovered == show_) return;
            _hovered = show_;
            if (show_)
            {
                EnsureOutlineMaterial();
                ShowName();
            }

            UiTween.PlayScalar(ref _hoverTween, () => _hoverFade,
                    WriteHoverFade, show_ ? 1f : 0f, _hoverFadeDuration)
                .OnComplete(HideNameIfIdle);
        }

        #endregion

        // 渐入系数的唯一写入口：名牌 alpha 与描边颜色按同一系数走。描边 pass 是预乘混合、片元输出整色乘描边遮盖度，
        // 故降不透明度必须整色缩放——只把 a 压到 0 会留下 RGB 加色亮斑
        private void WriteHoverFade(float fade_)
        {
            _hoverFade = fade_;
            if (_actor.ActorName) _actor.ActorName.alpha = fade_;
            if (_outlineMaterial) _outlineMaterial.SetColor(OutlineColorId, _outlineColor * fade_);
        }

        // 描边材质副本：首次悬停克隆一份并切到描边 shader 变体，赋给骨架后常驻；描边宽度与颜色随克隆自带
        private void EnsureOutlineMaterial()
        {
            if (_outlineMaterial || _outlineUnavailable || !_actor.SkeletonMaterial) return;
            Shader shader = _actor.OutlineShader ? _actor.OutlineShader : Shader.Find(OutlineShaderName);
            if (!shader)
            {
                _outlineUnavailable = true;
                Debug.LogError(
                    $"[{nameof(UI_ActorHoverBox)}] 找不到描边 shader：{OutlineShaderName}，悬停描边不可用（可在预制体上接 Shader 引用）");
                return;
            }

            _outlineMaterial = new Material(_actor.SkeletonMaterial) { shader = shader };
            _outlineColor = _outlineMaterial.GetColor(OutlineColorId);
            _outlineMaterial.SetColor(OutlineColorId, _outlineColor * _hoverFade); // 先抹到当前系数，免得换材质那一帧闪出满描边
            if (_actor.Skeleton) _actor.Skeleton.material = _outlineMaterial;
        }

        // 名牌显隐：亮出前先对齐当前系数（免露出上次残留），淡出补间走完才收起来；补间被杀时不收，留给下一次淡入接续
        private void ShowName()
        {
            if (!_actor.ActorName || _nameShown) return;
            _nameShown = true;
            _actor.ActorName.alpha = _hoverFade;
            _actor.ActorName.gameObject.SetActive(true);
        }

        private void HideNameIfIdle()
        {
            if (!_actor.ActorName || _hovered) return;
            _nameShown = false;
            _actor.ActorName.gameObject.SetActive(false);
        }
    }
}