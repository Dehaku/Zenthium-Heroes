Shader "Fraktalia/Core/MultiTexture/MultiTextureArray_UV3_Pulse"
{
    Properties
    {
       [Header(Main Maps)]
        _DiffuseMap("Albedo (RGB) Maps", 2DArray) = "white" {}
         _Color("Color", Color) = (1,1,1,1)

        [SingleLine(_Metallic)] _MetallicGlossMap("Metallic Maps", 2DArray) = "white" {}
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5

        [HideInInspector] _Metallic("Metallic", Range(0,1)) = 0.0


        [SingleLine(_BumpScale)] _BumpMap("Normal Maps", 2DArray) = "bump" {}
        [HideInInspector] _BumpScale("Normal Scale", Float) = 1.0


        [SingleLine(_Parallax)]
        _ParallaxMap("Height Maps", 2DArray) = "grey" {}
        [HideInInspector]_Parallax("Height Scale", Range(0.005, 0.08)) = 0.02

        [SingleLine(_OcclusionStrength)]
        _OcclusionMap("Occlusion Maps", 2DArray) = "white" {}
        [HideInInspector]_OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [SingleLine(_EmissionColor)]
        _EmissionMap("Emission Maps", 2DArray) = "black" {}
        [HideInInspector]_EmissionColor("Emission Color", Color) = (0,0,0)


        [Header(Texture Blending)]
        [SingleLine]
        _BlendMap("Blend Map", 2D) = "white" {}
        _BlendScale("Blend Scale", Float) = 1.0
        _BlendShift("Blend Shift", Float) = 0.5
        [HideInInspector]_IndexTex("Albedo (RGB)", 2D) = "black" {}

        _SliceRange("Initial Slice", Range(-16,16)) = 6
        _UV2Power("UV2 Power", float) = 1

        [Header(Pulse Settings)]
        _PulseFrequency ("Pulse Frequency", float) = 1
        _PulseAmplitude ("Pulse Amplitude", float) = 1



    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma require 2darray
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0


        UNITY_DECLARE_TEX2DARRAY(_DiffuseMap);
        UNITY_DECLARE_TEX2DARRAY(_BumpMap);
        UNITY_DECLARE_TEX2DARRAY(_ParallaxMap);
        UNITY_DECLARE_TEX2DARRAY(_MetallicGlossMap);
        UNITY_DECLARE_TEX2DARRAY(_OcclusionMap);
        UNITY_DECLARE_TEX2DARRAY(_EmissionMap);



        sampler2D _IndexTex;
        sampler2D _BlendMap;
        float _BlendScale;
        float _BlendShift;
        float _UV2Power;
        float _Parallax;


        struct Input
        {
            float2 uv_DiffuseMap;
            float2 uv3_IndexTex;
            float3 viewDir;
        };

        half _Glossiness;
        half _Metallic;

        fixed4 _Color;
        float _BumpScale;
        float _SliceRange;
        float4 _EmissionColor;
        float _OcclusionStrength;

        float _PulseFrequency;
        float _PulseAmplitude;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        half3 UnpackScaleNormalArray(half4 packednormal, half bumpScale)
        {
#if defined(UNITY_NO_DXT5nm)
            return packednormal.xyz * 2 - 1;
#else
            half3 normal;
            normal.xy = (packednormal.rg * 2 - 1);
#if (SHADER_TARGET >= 30)
            // SM2.0: instruction count limitation
            // SM2.0: normal scaler is not supported
            normal.xy *= bumpScale;
#endif
            normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
            return normal;
#endif
        }


        void surf(Input IN, inout SurfaceOutputStandard o)
        {

            _SliceRange;

            float blendvalue = (tex2D(_BlendMap, IN.uv_DiffuseMap).r) * _BlendScale;
            blendvalue += sin(_Time.y * _PulseFrequency) * _PulseAmplitude;


            _SliceRange += IN.uv3_IndexTex.x * _UV2Power * (blendvalue + _BlendShift);
            _SliceRange = max(0, _SliceRange);
            int textureindex = _SliceRange;
            float blendfactor = _SliceRange % 1;


            fixed3 uv_texture1 = fixed3(IN.uv_DiffuseMap.x, IN.uv_DiffuseMap.y, textureindex);
            fixed3 uv_texture2 = fixed3(IN.uv_DiffuseMap.x, IN.uv_DiffuseMap.y, textureindex + 1);

            float height1 = UNITY_SAMPLE_TEX2DARRAY(_ParallaxMap, uv_texture1).r * (1 - blendfactor);
            float height2 = UNITY_SAMPLE_TEX2DARRAY(_ParallaxMap, uv_texture2).r * (blendfactor);
            uv_texture1.xy += ParallaxOffset(height1 + height2, _Parallax, IN.viewDir);
            uv_texture2.xy += ParallaxOffset(height1 + height2, _Parallax, IN.viewDir);


            fixed4 c1 = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMap, uv_texture1) * _Color * (1 - blendfactor);
            fixed4 c2 = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMap, uv_texture2) * _Color * (blendfactor);
            o.Albedo = c1.rgb + c2.rgb;

            half4 bump1 = UNITY_SAMPLE_TEX2DARRAY(_BumpMap, uv_texture1) * (1 - blendfactor);
            half4 bump2 = UNITY_SAMPLE_TEX2DARRAY(_BumpMap, uv_texture2) * (blendfactor);
            o.Normal = UnpackScaleNormalArray(bump1 + bump2, _BumpScale);

            half4 m1 = UNITY_SAMPLE_TEX2DARRAY(_MetallicGlossMap, uv_texture1);
            half4 m2 = UNITY_SAMPLE_TEX2DARRAY(_MetallicGlossMap, uv_texture2);
            m1 *= (1 - blendfactor);
            m2 *= (blendfactor);

            o.Metallic = (m1.r + m2.r) * _Metallic;
            o.Smoothness = (m1.a + m2.a) * _Glossiness;



            fixed4 o1 = UNITY_SAMPLE_TEX2DARRAY(_OcclusionMap, uv_texture1) * (1 - blendfactor);
            fixed4 o2 = UNITY_SAMPLE_TEX2DARRAY(_OcclusionMap, uv_texture2) * (blendfactor);
            o.Occlusion = (o1.r + o2.r) * _OcclusionStrength;

            fixed4 e1 = UNITY_SAMPLE_TEX2DARRAY(_EmissionMap, uv_texture1) * (1 - blendfactor);
            fixed4 e2 = UNITY_SAMPLE_TEX2DARRAY(_EmissionMap, uv_texture2) * (blendfactor);
            o.Emission = (e1 + e2) * _EmissionColor;



            o.Alpha = c1.a + c2.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
    CustomEditor "TextureArrayGUI"
}
