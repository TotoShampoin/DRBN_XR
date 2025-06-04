Shader "Instanced/ParticleMesh"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _ParticleSize ("Particle Size", Float) = 0.1
        _VelocityColorRamp ("Velocity Color Ramp", 2D) = "white" {}
        _MaxVelocity ("Max Velocity", Float) = 10.0
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
            #pragma multi_compile_fog
            #pragma multi_compile_isntancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 pos : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _ParticleSize;
            int _ParticleCount;
            uniform float4x4 _LocalToWorld;
            sampler2D _VelocityColorRamp;
            float4 _VelocitColorRamp_ST;
            float _MaxVelocity;
            
            // This is the GPU buffer containing particle positions
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float3> _Velocities;

            v2f vert (appdata v)
            {
                v2f o;
                
                float3 instancePosition = _Positions[v.instanceID];
                
                float3 vertex = v.vertex.xyz * _ParticleSize;
                float4 worldPos = mul(_LocalToWorld, float4(vertex, 1));
                worldPos.xyz += instancePosition;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.instanceID = v.instanceID;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 velocity = _Velocities[i.instanceID];
                float velocityMag = length(velocity);
                float normalizedVelocity = saturate(velocityMag / _MaxVelocity);
                fixed4 c = tex2D(_VelocityColorRamp, float2(normalizedVelocity, 0.5));

                fixed4 col = tex2D(_MainTex, i.uv) * c;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
