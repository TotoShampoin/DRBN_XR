Shader "Unlit/SlideRenderer"
{
    Properties
    {
        _PDF("PDF", 3D) = "white" {}
        _Slice("Slice", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags {
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent"
        }
        Lighting On
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        LOD 100

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler3D _PDF;
            float4 _PDF_ST;
            float _Slice;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _PDF);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed col = tex3D(_PDF, float3(i.uv.x, _Slice, i.uv.y)).r;

                if(col > 0.0f) {
                    return fixed4(0.0f, 0.5f, 1.0f, col);
                } else {
                    return fixed4(1.0f, 0.5f, 0.0f, -col);
                }
            }
            ENDCG
        }
    }
}
