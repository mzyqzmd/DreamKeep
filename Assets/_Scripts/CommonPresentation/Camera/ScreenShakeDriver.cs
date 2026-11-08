using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>全屏抖动驱动：挂在常驻主相机上，受伤瞬间短暂接管 OnRenderImage 做全屏 UV 扭曲与轻色差，
    /// 强度在时长内线性衰减，归零即关闭自身——未播放时 OnRenderImage 不被调用，常态零开销。
    /// 由 VfxDefaultRoot 在整局容器装配时挂到常驻相机，随它销毁一并移除。</summary>
    public sealed class ScreenShakeDriver : MonoBehaviour
    {
        [SerializeField]private Shader _shader;
        [Header("幅度")] [Tooltip("UV 横向扭曲的最大偏移（屏幕宽度比例）")] [SerializeField]
        private float _amplitude = 0.012f;

        [Tooltip("逐行波形的密度：越大横向条纹越密")] [SerializeField]
        private float _frequency = 26f;

        [Tooltip("R/B 通道的反向偏移量（屏幕宽度比例）")] [SerializeField]
        private float _chromatic = 0.0035f;

        [Header("时长")] [Tooltip("抖动总时长（秒）：强度线性衰减，归零后自动关闭")] [SerializeField]
        private float _duration = 0.35f;

        private static readonly int Strength = Shader.PropertyToID("_Strength");
        private static readonly int Amplitude = Shader.PropertyToID("_Amplitude");
        private static readonly int Frequency = Shader.PropertyToID("_Frequency");
        private static readonly int Chromatic = Shader.PropertyToID("_Chromatic");
        private static readonly int ShakeTime = Shader.PropertyToID("_ShakeTime");
        
        private Material _material;
        private float _elapsed;

        private void Awake()
        {
            if (_shader) _material = new Material(_shader);
            enabled = false; // 常态不接管渲染：OnRenderImage 只在启用期间被调用
        }

        private void OnDestroy()
        {
            if (_material) Destroy(_material);
        }

        private void OnRenderImage(RenderTexture src_, RenderTexture dst_)
        {
            // 取不到 shader 或材质失效：退化为直通并关闭，避免每帧空转
            if (!_material)
            {
                Graphics.Blit(src_, dst_);
                enabled = false;
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= _duration)
            {
                enabled = false;
                Graphics.Blit(src_, dst_);
                return;
            }

            _material.SetFloat(Strength, 1f - _elapsed / _duration);
            _material.SetFloat(Amplitude, _amplitude);
            _material.SetFloat(Frequency, _frequency);
            _material.SetFloat(Chromatic, _chromatic);
            _material.SetFloat(ShakeTime, _elapsed);
            Graphics.Blit(src_, dst_, _material);
        }

        #region 对外入口：开始一次抖动

        /// <summary>开始一次抖动：重复触发即重置计时（同通道独占，不叠加），返回本次时长供编排方排后续演出。</summary>
        public float Play()
        {
            if (!_material) return 0f;
            _elapsed = 0f;
            enabled = true;
            return _duration;
        }

        #endregion
    }
}
