Shader "DreamKeep/UI/Card Ripple"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Ripple)]
        _Color ("Ripple Color", Color) = (1, 1, 1, 1)
        _Strength ("Strength", Range(0, 4)) = 1
        _Speed ("Speed", Range(-2, 4)) = 0.5
        _WaveCount ("Wave Count", Range(1, 12)) = 3
        _Thickness ("Thickness (of gap)", Range(0.02, 0.9)) = 0.25
        _Softness ("Softness", Range(0, 1)) = 0.35
        _Range ("Range", Range(0.01, 0.3)) = 0.15
        _FadePower ("Fade Power", Range(0.1, 8)) = 2
        _CornerRadius ("Corner Radius", Range(0, 0.3)) = 0.06
        _Phase ("Phase", Range(0, 1)) = 0

        [Header(Geometry)]
        _EdgeInset ("Card Edge Inset", Range(0, 0.4)) = 0.11538
        _Aspect ("Card Aspect H/W", Float) = 1.4115

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "CardRipple"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _Strength;
            float _Speed;
            float _WaveCount;
            float _Thickness;
            float _Softness;
            float _Range;
            float _FadePower;
            float _CornerRadius;
            float _Phase;
            float _EdgeInset;
            float _Aspect;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            // 圆角矩形有向距离：p 以"卡牌宽度"为单位、原点在卡牌中心，halfSize 为卡牌半尺寸，返回 d > 0 表示卡外
            float RoundedBoxDistance(float2 p_, float2 halfSize_, float radius_)
            {
                float2 q = abs(p_) - halfSize_ + radius_;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius_;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV 先还原成"以卡牌宽度为单位"的各向同性坐标：矩形比卡牌大 _EdgeInset 一圈
                float2 uv = i.texcoord - 0.5;
                float rectToCard = 0.5 / max(0.5 - _EdgeInset, 1e-4);
                float2 p = uv * rectToCard * float2(1.0, _Aspect);
                float2 halfSize = float2(0.5, 0.5 * _Aspect);
                float d = RoundedBoxDistance(p, halfSize, _CornerRadius);

                // 卡内不画：涟漪只作卡牌外围光效，卡内本来也被卡图遮挡
                clip(d);

                // 周期波：波峰随 _Time 增大而向外推进，层间距 = 范围 / 层数
                float range = max(_Range, 1e-4);
                float period = max(range / max(_WaveCount, 1.0), 1e-4);
                float wavePhase = frac(d / period - _Time.y * _Speed + _Phase);
                float crestDistance = min(wavePhase, 1.0 - wavePhase) * period;

                // 环：环宽按层间距的比例给出，柔化程度决定边缘是硬边（仅抗锯齿）还是雾状
                float ringWidth = saturate(_Thickness) * period;
                float halfWidth = max(ringWidth * 0.5, 1e-5);
                float aa = max(fwidth(crestDistance) * 0.75, 1e-5);
                float softEdge = min(lerp(aa, halfWidth, saturate(_Softness)), halfWidth * 0.9);
                float ring = 1.0 - smoothstep(halfWidth - softEdge, halfWidth + softEdge, crestDistance);

                // 向外渐隐：卡边最亮，到 _Range 处归零
                float fade = pow(saturate(1.0 - d / range), max(_FadePower, 0.01));

                fixed4 col = i.color;
                col.a *= ring * fade * max(_Strength, 0.0);

                #ifdef UNITY_UI_CLIP_RECT
                float2 clipPoint = i.worldPosition.xy;
                float2 inside = step(_ClipRect.xy, clipPoint) * step(clipPoint, _ClipRect.zw);
                col.a *= inside.x * inside.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
