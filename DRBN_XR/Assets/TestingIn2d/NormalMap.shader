Shader "Unlit/TextureRender"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "white" {}
        _Z ("Z", Range(0, 1)) = 0.5
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
            };

            struct v2f
            {
                float3 uvw : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler3D _MainTex;
            float _Z;
            float _Threshold;
            float _ThresholdThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvw = float3(v.uv, _Z);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 val = tex3D(_MainTex, i.uvw);
                return val;
            }
            ENDCG
        }
    }
}
