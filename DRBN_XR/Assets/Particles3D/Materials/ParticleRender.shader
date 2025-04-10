Shader "Instanced/ParticleRender"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0,0,0,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _ParticleSize ("Particle Size", Float) = 0.1
        _PartitionResolution ("Partition Resolution", Int) = 0
    }
    SubShader
    {
        Tags {
            "RenderType"="Opaque"
            "Queue"="Transparent"
			"IgnoreProjector" = "True"
			"DisableBatching" = "True"
        }
        LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

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
            float4 _ColorA;
            float4 _ColorB;
            float _ParticleSize;
            int _PartitionResolution;
            int _ParticleCount;
            uniform float4x4 _LocalToWorld;
            
            // This is the GPU buffer containing particle positions
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<uint> _Indices;
            StructuredBuffer<uint> _SubgridIndices;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Get position from the buffer
                float3 worldPosition = _Positions[_Indices[v.instanceID]];
                // float3 worldPosition = _Positions[v.instanceID];
                // worldPosition = mul(_LocalToWorld, float4(worldPosition, 1)).xyz;
                
                // Apply object-to-world transformation
                // worldPosition = 
                //     mul((float3x3)unity_ObjectToWorld, worldPosition) + 
                //     float3(
                //         unity_ObjectToWorld._m03,
                //         unity_ObjectToWorld._m13,
                //         unity_ObjectToWorld._m23);
                
                // Calculate billboard vertices based on particle size
                float3 cameraRight = normalize(UNITY_MATRIX_IT_MV[0].xyz);
                float3 cameraUp = normalize(UNITY_MATRIX_IT_MV[1].xyz);
                
                float3 localPos = v.vertex.xyz * _ParticleSize;
                float3 worldPos = mul(_LocalToWorld, worldPosition)
                               + cameraRight * localPos.x 
                               + cameraUp * localPos.y;
                
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.instanceID = v.instanceID;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                uint index1 = _SubgridIndices[i.instanceID] % _PartitionResolution;
                uint index2 = (_SubgridIndices[i.instanceID] / _PartitionResolution) % _PartitionResolution;
                uint index3 = _SubgridIndices[i.instanceID] / (_PartitionResolution * _PartitionResolution);
                float3 normalizedIndex = float3(
                    float(index1) / float(_PartitionResolution - 1),
                    float(index2) / float(_PartitionResolution - 1),
                    float(index3) / float(_PartitionResolution - 1)
                );

                // fixed4 c = lerp(_ColorA, _ColorB, normalizedIndex);
                fixed4 c = fixed4(normalizedIndex, 1);
                if(_Indices[i.instanceID] >= _ParticleCount)
                    c = fixed4(1,0,1,1);
                // c.rgb = normalize(c.rgb);

                fixed4 col = tex2D(_MainTex, i.uv) * c;
                // fixed4 col = tex2D(_MainTex, i.uv) ;
                if(length(i.uv * 2 - 1) > 1) {
                    col.a = 0;
                    discard;
                }
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
