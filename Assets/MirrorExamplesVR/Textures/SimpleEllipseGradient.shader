Shader "Custom/SimpleEllipseGradient"
{
    // Mirrors TransparentCircle.shader exactly (Opaque queue, no Blend, discard inside ellipse).
    // The only behavioural difference vs TransparentCircle: the periphery outputs pure black
    // instead of sampling _MainTex. Keeping the same render path guarantees the ellipse
    // geometry matches FovOnly's under XR Single-Pass Instanced stereo (no asymmetric
    // sampling artifacts that alpha-blended UI quads can exhibit there).
    Properties
    {
        // RawImage always assigns _MainTex; declare it (unused in frag) to keep Unity quiet.
        _MainTex ("Texture", 2D) = "white" {}
        _RadiusX ("RadiusX", Float) = 0.24
        _RadiusY ("RadiusY", Float) = 0.19
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                if (d < 1.0) discard;
                return fixed4(0.0, 0.0, 0.0, 1.0);
            }
            ENDCG
        }
    }
}
