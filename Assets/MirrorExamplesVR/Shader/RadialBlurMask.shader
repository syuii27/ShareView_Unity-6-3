Shader "Custom/RadialBlurMask" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Float) = 2.5
        _MaskRadius ("Mask Radius", Float) = 0.5
        _MaskCenter ("Mask Center", Vector) = (0.5, 0.5, 0, 0)
        _BorderWidth ("Border Width", Float) = 0.1
        _Transparency ("Transparency", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float _BlurStrength;
            float _MaskRadius;
            float2 _MaskCenter;
            float _BorderWidth;
            float _Transparency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float dist = distance(uv, _MaskCenter);
                float4 color = tex2D(_MainTex, uv);
                float edgeStart = _MaskRadius - _BorderWidth / 10;

                if (dist < edgeStart)
                {
                    color.a *= _Transparency;; // Completely transparent in the center
                }
                else if (dist < _MaskRadius)
                {
                    // Smooth transition to blur
                    float alpha = (dist - edgeStart) / (_BorderWidth / 10);
                    color.a *= smoothstep(0.0, 1.0, alpha);
                }
                else
                {
                    float blurAmount = _BlurStrength * (dist - _MaskRadius);
                    float2 blurredUV = uv + normalize(uv - _MaskCenter) * blurAmount;
                    color = tex2D(_MainTex, blurredUV);
                }
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
