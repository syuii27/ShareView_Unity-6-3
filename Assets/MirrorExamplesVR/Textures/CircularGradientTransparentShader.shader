Shader "Custom/CircularGradientTransparentShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeTransparency ("Edge Transparency", Range(0,1)) = 1.0
        _Radius ("Radius", Float) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _EdgeTransparency;
            float _Radius;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center the texture coordinates
                float2 centeredUV = i.uv - float2(0.5, 0.5);
                // Calculate distance from the center
                float dist = length(centeredUV);

                // Calculate transparency based on distance
                float alpha = smoothstep(_Radius, _Radius + 0.1, dist);
                alpha = lerp(1.0, _EdgeTransparency, alpha);

                // Display texture if within the radius, else discard pixel
                if (dist > _Radius + 0.1) discard;
                fixed4 color = tex2D(_MainTex, i.uv);
                color.a *= alpha;
                return color;
            }
            ENDCG
        }
    }
}
