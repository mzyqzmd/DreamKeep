Shader "DreamKeep/VFX/Screen Shake"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [Header(Shake)]
        _Strength ("Strength", Range(0, 1)) = 0
        _Amplitude ("Amplitude", Float) = 0.012
        _Frequency ("Frequency", Float) = 26
        _Chromatic ("Chromatic", Float) = 0.0035
        _ShakeTime ("Shake Time", Float) = 0
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float _Strength;
            float _Amplitude;
            float _Frequency;
            float _Chromatic;
            float _ShakeTime;

            // 横向逐行扭曲 + R/B 通道轻微反向偏移；越界走 clamp 采样（拉伸边缘像素），不露黑边
            fixed4 frag(v2f_img i) : SV_Target
            {
                float wave = sin(i.uv.y * _Frequency + _ShakeTime * 40.0);
                float offset = wave * _Amplitude * _Strength;
                float split = _Chromatic * _Strength;

                fixed3 color;
                color.r = tex2D(_MainTex, clamp(i.uv + float2(offset + split, 0.0), 0.0, 1.0)).r;
                color.g = tex2D(_MainTex, clamp(i.uv + float2(offset, 0.0), 0.0, 1.0)).g;
                color.b = tex2D(_MainTex, clamp(i.uv + float2(offset - split, 0.0), 0.0, 1.0)).b;
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
}
