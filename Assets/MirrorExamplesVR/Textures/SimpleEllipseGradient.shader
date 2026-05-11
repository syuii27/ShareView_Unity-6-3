Shader "Custom/SimpleEllipseGradient"
{
    Properties
    {
        // RawImage always assigns _MainTex; declare it (unused in frag) to avoid
        // "doesn't have a texture property '_MainTex'" warnings.
        _MainTex ("Texture", 2D) = "white" {}
        _RadiusX ("RadiusX", Float) = 0.24
        _RadiusY ("RadiusY", Float) = 0.19
        _FadeWidth ("FadeWidth", Float) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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

            float _RadiusX;
            float _RadiusY;
            float _FadeWidth;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dx = (i.uv.x - 0.5) / max(_RadiusX, 1e-5);
                float dy = (i.uv.y - 0.5) / max(_RadiusY, 1e-5);
                float d  = sqrt(dx * dx + dy * dy);
                float a  = saturate((d - 1.0) / max(_FadeWidth, 1e-5));
                return fixed4(0.0, 0.0, 0.0, a);
            }
            ENDCG
        }
    }
}
