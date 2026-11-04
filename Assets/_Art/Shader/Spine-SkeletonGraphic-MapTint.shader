// 通道重着色版 SkeletonGraphic：图集的红/蓝/白三色部件解码为「主色调 + 黑层色 + 白原样」，
// 照参考项目口径——红通道→主色、蓝通道→黑层、绿(白)通道→原样；地图 Boss 节点专用。
// 源自插件 Spine-SkeletonGraphic.shader 与其 NormalPass.cginc（内联展开，避免相对路径 include 依赖），
// 仅在 frag 插入重映射段；Stencil / PMA 混合 / ClipRect / 直通 Alpha / CanvasGroup 兼容 Toggle 全部保留。
Shader "DreamKeep/UI/Spine SkeletonGraphic Map Tint"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
		[Toggle(_CANVAS_GROUP_COMPATIBLE)] _CanvasGroupCompatible("CanvasGroup Compatible", Int) = 1
		_Color ("Tint", Color) = (1,1,1,1)
		_MainColor ("主色调（红通道）", Color) = (0.671, 0.58, 0.478, 1)
		_DarkColor ("黑层色（蓝通道）", Color) = (0.15, 0.12, 0.09, 1)

		[HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector][Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255

		[HideInInspector] _ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
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
		Fog { Mode Off }
		Blend One OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "Normal"

		CGPROGRAM
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
			#pragma shader_feature _ _CANVAS_GROUP_COMPATIBLE
			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			struct VertexInput {
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput {
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			fixed4 _MainColor;
			fixed4 _DarkColor;

			// —— Spine-Common.cginc 内联 ——
			inline half3 TargetToGammaSpace(half3 targetColor) {
			#if UNITY_COLORSPACE_GAMMA
				return targetColor;
			#else
				return LinearToGammaSpace(targetColor);
			#endif
			}

			inline half3 GammaToTargetSpace(half3 gammaColor) {
			#if UNITY_COLORSPACE_GAMMA
				return gammaColor;
			#else
				return GammaToLinearSpace(gammaColor);
			#endif
			}

			inline half4 PMAGammaToTargetSpaceSaturated(half4 gammaPMAColor) {
			#if UNITY_COLORSPACE_GAMMA
				return gammaPMAColor;
			#else
				return gammaPMAColor.a == 0 ?
					half4(GammaToTargetSpace(gammaPMAColor.rgb), gammaPMAColor.a) :
					half4(saturate(GammaToTargetSpace(gammaPMAColor.rgb / gammaPMAColor.a)) * gammaPMAColor.a, gammaPMAColor.a);
			#endif
			}

			VertexOutput vert (VertexInput IN) {
				VertexOutput OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = IN.texcoord;

				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1);
				#endif

			#ifdef _CANVAS_GROUP_COMPATIBLE
				half4 vertexColor = IN.color;
				// CanvasGroup alpha sets vertex color alpha, but does not premultiply it to rgb components.
				vertexColor.rgb *= vertexColor.a;
			#else
				float4 vertexColor = PMAGammaToTargetSpaceSaturated(half4(TargetToGammaSpace(IN.color.rgb), IN.color.a));
			#endif
				OUT.color = vertexColor;
				OUT.color *= float4(_Color.rgb * _Color.a, _Color.a); // Combine a PMA version of _Color with vertexColor.

				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag (VertexOutput IN) : SV_Target
			{
				half4 texColor = tex2D(_MainTex, IN.texcoord);
				half alpha = texColor.a;

				// 图集部件为纯色：红(1,0,0)/蓝(0,0,1)/白(1,1,1)；白件三通道同为 1，须用 (1-g) 从红蓝掩码中排除。
				// 重映射在直通色域进行，完成后再乘 alpha 回 PMA，匹配 Blend One OneMinusSrcAlpha。
				half3 raw = texColor.rgb;
			#if !defined(_STRAIGHT_ALPHA_INPUT)
				raw = min(raw / max(alpha, 1e-4), half3(1, 1, 1)); // PMA 纹理先还原直通色再判通道
			#endif

				half3 remapped = raw.r * (1.0 - raw.g) * _MainColor.rgb   // 红 → 主色调
					+ raw.b * (1.0 - raw.g) * _DarkColor.rgb              // 蓝 → 黑层色
					+ raw.g * raw;                                        // 绿(白) → 原样保留

				half4 color = half4(remapped * alpha, alpha) + _TextureSampleAdd;
				color *= IN.color;
				color *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
}
