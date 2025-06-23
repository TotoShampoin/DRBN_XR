Shader "Custom/StandardWthMaps"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Roughness ("Roughness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _NormalMap ("Normal map", 2D) = "bump" {}
        _ARMMap ("ARM (AO, Roughness, Metallic) map", 2D) = "white" {}
        _HeightMap ("Height map", 2D) = "black" {}
        _HeightScale ("Height Scale", Range(0, 0.2)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma shader_feature _NORMALMAP

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _ARMMap;
        sampler2D _HeightMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_ARMMap;
            float2 uv_HeightMap;
            float3 viewDir;
        };

        half _Roughness;
        half _Metallic;
        fixed4 _Color;
        float _HeightScale;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Parallax mapping using height map
            float height = tex2D(_HeightMap, IN.uv_HeightMap).r;
            float2 parallaxOffset = (height - 0.5) * _HeightScale * normalize(IN.viewDir).xy;
            float2 parallaxUV = IN.uv_MainTex + parallaxOffset;

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, parallaxUV) * _Color;
            o.Albedo = c.rgb;

            // Sample ARM map
            fixed4 arm = tex2D(_ARMMap, parallaxUV);
            float ao = arm.r;
            float roughness = arm.g * _Roughness;
            float metallic = arm.b * _Metallic;

            // Use ARM map values
            o.Metallic = metallic;
            o.Smoothness = 1.0 - roughness;
            o.Occlusion = ao;

            // Normal map
            o.Normal = UnpackNormal(tex2D(_NormalMap, parallaxUV));

            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
