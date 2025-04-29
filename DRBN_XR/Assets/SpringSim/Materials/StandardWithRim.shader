Shader "Custom/StandardWithRim"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _RimGamma ("Rim Gamma", Range(0,5)) = 1.0

        _StdColor ("Albedo", Color) = (1,1,1,1)
        _StdMetallic ("Metallic", Range(0,1)) = 1.0
        _StdSmoothness ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 viewDir;
        };

        fixed4 _Color;
        float _RimGamma;

        fixed4 _StdColor;
        float _StdMetallic;
        float _StdSmoothness;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 normal = normalize(IN.worldNormal);
            float rim = 1.0 - saturate(dot(normal, normalize(IN.viewDir)));
            fixed4 r = _Color * fixed4(1,1,1,pow(rim, _RimGamma));

            // Albedo comes from a texture tinted by color
            fixed4 c = _StdColor;
            // o.Albedo = c.rgb * (1 - r.a);
            o.Albedo = lerp(c, r, r.a);
            o.Emission = r.rgb * r.a;
            o.Metallic = _StdMetallic;
            o.Smoothness = _StdSmoothness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
