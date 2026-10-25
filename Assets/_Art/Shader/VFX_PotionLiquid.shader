Shader "DreamKeep/VFX/Potion Liquid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Base)]
        _Tint ("Liquid Tint", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _LumaBoost ("Brightness", Range(0.5, 2)) = 1.15

        [Header(Flow)]
        _Tile ("Tile", Range(0.5, 8)) = 1.6
        _ScrollSpeed ("Scroll Speed", Range(-2, 2)) = 0.35
        _DistortStrength ("Distort Strength", Range(0, 0.2)) = 0.03
        _DistortSpeed ("Distort Speed", Range(0, 4)) = 0.8
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.9)) = 0.35

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
            Name "PotionLiquid"

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
            fixed4 _Tint;
            float _Alpha;
            float _LumaBoost;
            float _Tile;
            float _ScrollSpeed;
            float _DistortStrength;
            float _DistortSpeed;
            float _EdgeSoftness;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 液面流动：先按 _Tile 平铺并随时间横向平移，再把同一张图当噪声取两个通道做位移，
                // 得到"液面在缓慢翻涌"的效果（贴图 wrap 必须是 Repeat，否则接缝会露出来）
                float2 uv = i.texcoord * _Tile + float2(_Time.y * _ScrollSpeed, 0);
                float2 noise = tex2D(_MainTex, uv * 0.5 + float2(0, _Time.y * _DistortSpeed)).ba - 0.5;
                float4 tex = tex2D(_MainTex, uv + noise * _DistortStrength * 2.0);

                // 亮度乘色：保留贴图的明暗层次，只把色相换成药水色
                float luma = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float3 rgb = luma * _Tint.rgb * _LumaBoost;

                // 柔边：素材自带羽化，这里再压一道低 alpha 的台阶，让边缘在放大后不发脏
                float alpha = smoothstep(0.0, _EdgeSoftness, tex.a) * tex.a * _Alpha * i.color.a * _Tint.a;

                fixed4 col = fixed4(rgb, alpha);

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
