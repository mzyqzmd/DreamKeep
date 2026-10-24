Shader "DreamKeep/UI/Relic Trigger"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Base)]
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Boost ("Brightness Boost", Range(1, 3)) = 1
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 1

        [Header(Ripple)]
        _RippleColor ("Ripple Color", Color) = (1, 1, 1, 1)
        _RippleStrength ("Ripple Strength", Range(0, 4)) = 0
        _Progress ("Progress", Range(0, 1)) = 0
        _Expand ("Expand (diameter ratio)", Range(1, 3)) = 2
        _Thickness ("Ring Thickness", Range(0.01, 0.4)) = 0.08
        _RingOffset ("Ring Offset", Range(0.05, 1)) = 0.5
        _FadePower ("Fade Power", Range(0.1, 8)) = 2

        [Header(Icon)]
        _IconTex ("Icon Texture", 2D) = "white" {}
        _IconRect ("Icon Rect (xy min, zw size)", Vector) = (0, 0, 1, 1)
        _IconScale ("Icon Scale In Rect", Range(0.05, 1)) = 0.5

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
            Name "RelicTrigger"

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

            sampler2D _MainTex;
            sampler2D _IconTex;
            fixed4 _Color;
            fixed4 _RippleColor;
            float4 _IconRect;
            float _Boost;
            float _BaseAlpha;
            float _RippleStrength;
            float _Progress;
            float _Expand;
            float _Thickness;
            float _RingOffset;
            float _FadePower;
            float _IconScale;
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

            // 图标轮廓覆盖度：把 uv 映射到"图标放大 scale_ 倍"后的局部空间再采样图标 alpha，返回该点是否落在轮廓内。
            // 越界必须显式判零——图标贴图是 Clamp，边缘像素会被无限拉伸成一条边。
            float IconCoverage(float2 uv_, float scale_)
            {
                float2 iconLocal = (uv_ - 0.5) / max(_IconScale * scale_, 1e-4) + 0.5;
                float2 iconUv = _IconRect.xy + iconLocal * _IconRect.zw;
                float alpha = tex2D(_IconTex, iconUv).a;
                float inside = step(0.0, iconLocal.x) * step(iconLocal.x, 1.0) *
                               step(0.0, iconLocal.y) * step(iconLocal.y, 1.0);
                return smoothstep(0.35, 0.65, alpha) * inside;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 底图通道：图标层画 sprite（_BaseAlpha = 1），涟漪层留空（_BaseAlpha = 0）
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                col.rgb *= _Boost;
                col.a *= _BaseAlpha;

                // 涟漪：常态强度为零时整段跳过——同一次绘制内 uniform 一致，没有分支发散代价
                if (_RippleStrength > 0.0)
                {
                    // 两圈错相位，各自从图标轮廓向外扩到 _Expand 倍，扩得越远越淡
                    float ring = 0.0;
                    [unroll]
                    for (int k = 0; k < 2; k++)
                    {
                        float phase = frac(_Progress + k * _RingOffset);
                        float scale = 1.0 + phase * (_Expand - 1.0);
                        float outer = IconCoverage(i.texcoord, scale);
                        float inner = IconCoverage(i.texcoord, max(scale - _Thickness, 0.01));
                        ring += saturate(outer - inner) * pow(1.0 - phase, _FadePower);
                    }

                    // 环只允许长在图标之外：再乘一次当前轮廓的遮挡，把图标内部压成零
                    ring *= saturate(1.0 - IconCoverage(i.texcoord, 1.0));
                    col.rgb += _RippleColor.rgb * ring * _RippleStrength;
                    col.a = saturate(col.a + ring * _RippleStrength * _RippleColor.a);
                }

                #ifdef UNITY_UI_CLIP_RECT
                float2 clipPoint = i.worldPosition.xy;
                float2 insideClip = step(_ClipRect.xy, clipPoint) * step(clipPoint, _ClipRect.zw);
                col.a *= insideClip.x * insideClip.y;
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
