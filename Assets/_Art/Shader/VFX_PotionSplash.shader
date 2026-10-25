Shader "DreamKeep/VFX/Potion Splash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Base)]
        _Tint ("Liquid Tint", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _LumaBoost ("Brightness", Range(0.5, 2)) = 1.15

        [Header(Erosion)]
        _Progress ("Progress", Range(0, 1)) = 0
        _ErosionWidth ("Erosion Width", Range(0.01, 0.9)) = 0.35

        [Header(Flow)]
        _FlowScale ("Flow Scale", Range(1, 40)) = 12
        _FlowSpeed ("Flow Speed", Range(0, 4)) = 1.2
        _FlowStrength ("Flow Strength", Range(0, 0.05)) = 0.012

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
            Name "PotionSplash"

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
            float _Progress;
            float _ErosionWidth;
            float _FlowScale;
            float _FlowSpeed;
            float _FlowStrength;
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
                // 液体蠕动：两组正交正弦合成位移。贴图自身没有噪声通道可借，就只用切空间里的波形，
                // 水平与竖直各走一条主频，副频反相抵消掉同向平移感
                float phase = _Time.y * _FlowSpeed;
                float2 uv = i.texcoord;
                float2 flow;
                flow.x = sin(uv.y * _FlowScale + phase) + 0.5 * sin(uv.y * _FlowScale * 2.3 - phase * 1.7);
                flow.y = cos(uv.x * _FlowScale * 0.9 - phase * 1.3) + 0.5 * cos(uv.x * _FlowScale * 2.1 + phase * 0.8);
                float4 tex = tex2D(_MainTex, uv + flow * _FlowStrength);

                // 亮度乘色：保留贴图的明暗层次，只把色相换成药水色，避免素材原有的粉紫透上来
                float luma = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float3 rgb = luma * _Tint.rgb * _LumaBoost;

                // 侵蚀：alpha 低于阈值的部分先消失，阈值随进度从负值升到 1，于是整张图从边缘（低 alpha）向内吃掉；
                // 起点取负宽度，保证进度为零时完全不动作
                float threshold = _Progress * (1.0 + _ErosionWidth) - _ErosionWidth;
                float mask = smoothstep(threshold, threshold + _ErosionWidth, tex.a);
                float alpha = mask * tex.a * _Alpha * i.color.a * _Tint.a;

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
