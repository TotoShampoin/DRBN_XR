Shader "Instanced/VoxelCube"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorRamp ("Color Ramp", 2D) = "white" {}
        _MinValue ("Min Value", Range(-5, 5)) = -1
        _MaxValue ("Max Value", Range(-5, 5)) = 1
        _VoxelSize ("Voxel size", Range(0, 1)) = 0.1
        _Opacity ("Opacity", Range(0, 1)) = 0.5
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

            uniform int _Width;
            uniform int _Height;
            uniform int _Depth;
            sampler3D _Texture;
            sampler3D _Normals;
            uniform float4x4 _LocalToWorld;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _ColorRamp;
            float4 _ColorRamp_ST;
            float _MinValue;
            float _MaxValue;
            float _Opacity;

            float _VoxelSize;
            float3 _BoundsMin;
            float3 _BoundsMax;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 pos : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.instanceID = v.instanceID;

                // Map instanceID to 3D grid coordinates
                int z = v.instanceID / (_Width * _Height);
                int y = (v.instanceID / _Width) % _Height;
                int x = v.instanceID % _Width;

                // Compute normalized position in [0,1] range for sampling
                float3 normalizedPos = float3(x, y, z) / float3(_Width - 1, _Height - 1, _Depth - 1);

                // Map normalized position to bounds
                float3 instancePosition = lerp(_BoundsMin, _BoundsMax, normalizedPos);
                
                // // Calculate billboard vertices based on particle size
                // float3 cameraRight = normalize(UNITY_MATRIX_IT_MV[0].xyz);
                // float3 cameraUp = normalize(UNITY_MATRIX_IT_MV[1].xyz);
                
                // float3 vertex = v.vertex.xyz * _VoxelSize;
                // float4 worldPos = mul(_LocalToWorld, float4(instancePosition, 1))
                //                + float4(cameraRight, 0) * vertex.x 
                //                + float4(cameraUp, 0) * vertex.y;
                // o.pos = mul(UNITY_MATRIX_VP, worldPos);

                // Standard transform (no billboard)
                float4 worldPos = mul(_LocalToWorld, float4(instancePosition + v.vertex.xyz * _VoxelSize, 1));
                o.pos = mul(UNITY_MATRIX_VP, worldPos);

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Map instanceID to 3D grid coordinates
                int z = i.instanceID / (_Width * _Height);
                int y = (i.instanceID / _Width) % _Height;
                int x = i.instanceID % _Width;

                // Compute normalized position in [0,1] range for sampling
                float3 normalizedPos = float3(x, y, z) / float3(_Width - 1, _Height - 1, _Depth - 1);

                // Sample the 3D texture at this position
                float value = tex3D(_Texture, normalizedPos).r;

                // COLOR MODE
                float t = saturate((value - _MinValue) / (_MaxValue - _MinValue));
                // Sample color ramp using t
                fixed4 rampColor = tex2D(_ColorRamp, float2(t, 0.5));
                fixed4 col = rampColor * value;
                col.a = 1;
                if(value < 0) discard;

                // fixed4 col = fixed4(norm, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
