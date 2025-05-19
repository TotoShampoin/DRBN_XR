Shader "Unlit/TransparentRim"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _RimGamma ("Rim Gamma", Range(0,5)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 _Color;
            float _RimGamma;

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate view direction in view space
                float3 viewDir = normalize(-UnityObjectToViewPos(float4(0,0,0,1)).xyz);
                float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, i.normal));
                float dotNV = dot(normalVS, viewDir);

                float rim = 1.0 - saturate(dotNV);
                fixed4 col = _Color * fixed4(1,1,1,pow(rim, _RimGamma));
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
