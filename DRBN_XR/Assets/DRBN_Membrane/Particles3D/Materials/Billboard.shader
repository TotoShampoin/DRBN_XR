Shader "Unlit/Billboard"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"DisableBatching" = "True"
		}

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
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
				UNITY_FOG_COORDS(1)
				float4 pos : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;

			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_INSTANCING_BUFFER_END(Props)

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				
				float3 vpos = mul(
					(float3x3)unity_ObjectToWorld,
					v.vertex.xyz);
				float4 worldCoord = float4(
					unity_ObjectToWorld._m03, 
					unity_ObjectToWorld._m13, 
					unity_ObjectToWorld._m23, 1);
					float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) 
					+ float4(vpos, 0);
				o.pos = mul(UNITY_MATRIX_P, viewPos);
				o.uv = v.uv.xy;

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				if(length(i.uv * 2 - 1) > 1) {
					col.a = 0;
				}
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
