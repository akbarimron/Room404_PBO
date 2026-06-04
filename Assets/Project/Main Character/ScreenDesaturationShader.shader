Shader "Custom/ScreenDesaturation"
{
    Properties
    {
        _MainTex ("Main Texture (UI Default)", 2D) = "white" {}
        _Intensity ("Effect Intensity", Range(0, 1)) = 0
        _Desaturation ("Desaturation Amount", Range(0, 1)) = 0.85
        _Distortion ("Distortion Amount", Range(-1, 1)) = -0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _CameraOpaqueTexture;
            float _Intensity;
            float _Desaturation;
            float _Distortion;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // Apply Lens Distortion (pincushion/barrel distortion)
                float2 d = screenUV - 0.5;
                float r2 = dot(d, d);
                float2 distortedUV = 0.5 + d * (1.0 + _Distortion * r2 * _Intensity);

                // Sample screen opaque texture
                fixed4 screenColor = tex2D(_CameraOpaqueTexture, distortedUV);

                // Apply desaturation (monochrome)
                float gray = dot(screenColor.rgb, float3(0.299, 0.587, 0.114));
                fixed3 monochromeColor = lerp(screenColor.rgb, float3(gray, gray, gray), _Desaturation * _Intensity);

                // Apply chromatic aberration (channel split) at the edges
                float2 uvRed = 0.5 + d * (1.0 + (_Distortion + 0.05) * r2 * _Intensity);
                float2 uvBlue = 0.5 + d * (1.0 + (_Distortion - 0.05) * r2 * _Intensity);
                float red = tex2D(_CameraOpaqueTexture, uvRed).r;
                float blue = tex2D(_CameraOpaqueTexture, uvBlue).b;

                fixed3 finalColor = monochromeColor;
                finalColor.r = lerp(finalColor.r, red, _Intensity * 0.7);
                finalColor.b = lerp(finalColor.b, blue, _Intensity * 0.7);

                // Vignette effect (dark edges)
                float vignette = 1.0 - r2 * 2.0 * _Intensity;
                vignette = clamp(vignette, 0.1, 1.0);
                finalColor *= vignette;

                return fixed4(finalColor, _Intensity);
            }
            ENDCG
        }
    }
}
