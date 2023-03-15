Shader "Fraktalia/Core/MultiTexture/MultiTextureArray_UV3_Tesselation_Triplanar"
{
    Properties
    {
        _TransformTex("Texture Transform", Vector) = (0, 0, 1, 1)


        [Header(Main Maps)]    
        [SingleLine(_Color)] _DiffuseMap ("Diffuse (Albedo) Maps", 2DArray) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1,1,1,1)
                
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
        _UV3Power("UV3 Power", float) = 1

        


        [Header(Triplanar Settings)]
        _MapScale("Triplanar Scale", Float) = 1
       
        [MultiCompileOption(TESSELLATION)]
        TESSELLATION("Tessellation", float) = 0
       

        [KeywordDependent(TESSELLATION)] _Tess("Tessellation", Range(1,32)) = 4
        [KeywordDependent(TESSELLATION)] minDist("Tess Min Distance", float) = 10
        [KeywordDependent(TESSELLATION)] maxDist("Tess Max Distance", float) = 25
        [KeywordDependent(TESSELLATION)] _TesselationPower("Power Tesselation", Range(-100, 100)) = 1
        [KeywordDependent(TESSELLATION)] _TesselationOffset("Offset Tesselation", Range(-100, 100)) = 1

        [MultiCompileToggle(USE_COLOR_FOR_SEAMLESS_TESSELLATION, TESSELLATION)]
        USE_COLOR_FOR_SEAMLESS_TESSELLATION("Tessellation", float) = 0
        [KeywordDependent(TESSELLATION)] _Tess_MinEdgeDistance("MinEdge Tesselation", Range(0, 1)) = 1

        [MultiCompileOption(PULSATING)]
        PULSATING("Tessellation", float) = 0
        [KeywordDependent(PULSATING)] _PulseFrequency("Pulse Frequency", float) = 1
        [KeywordDependent(PULSATING)] _PulseAmplitude("Pulse Amplitude", float) = 1

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        //Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        CGPROGRAM

        
        //#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

        //#pragma shader_feature _EMISSION
        //#pragma shader_feature _METALLICGLOSSMAP
        //#pragma shader_feature ___ _DETAIL_MULX2
        //#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        //#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
        //#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
        //#pragma shader_feature _PARALLAXMAP
        #pragma shader_feature USE_COLOR_FOR_SEAMLESS_TESSELLATION
        #pragma shader_feature TESSELLATION
        #pragma shader_feature PULSATING


        #pragma surface surf Standard fullforwardshadows vertex:disp tessellate:tessDistance

        #pragma require 2darray




        
        #pragma target 4.6
        #include "Tessellation.cginc"

        UNITY_DECLARE_TEX2DARRAY(_DiffuseMap);
        UNITY_DECLARE_TEX2DARRAY(_BumpMap);
        UNITY_DECLARE_TEX2DARRAY(_ParallaxMap);
        UNITY_DECLARE_TEX2DARRAY(_MetallicGlossMap);
        UNITY_DECLARE_TEX2DARRAY(_OcclusionMap);
        UNITY_DECLARE_TEX2DARRAY(_EmissionMap);

        
        float4 _TransformTex;

        sampler2D _IndexTex;
        sampler2D _BlendMap;
        float _BlendScale;
        float _BlendShift;
        float _UV3Power;
        float _Parallax;

        float _MapScale;
       
        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float2 uv_MainTex;
            float2 uv3_IndexTex;
            float3 viewDir;        
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;

        fixed4 _Color;
        float _BumpScale;
        float _SliceRange;
        float4 _EmissionColor;
        float _OcclusionStrength;
        float _Tess;
        float _TesselationPower;
        float _Tess_MinEdgeDistance;
        float _TesselationOffset;
        float minDist;
        float maxDist;

#if PULSATING
        float _PulseFrequency;
        float _PulseAmplitude;
#endif

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

#pragma shader_feature SEAMLESS_TESSELATION

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

        float4 tessDistance(appdata_full v0, appdata_full v1, appdata_full v2) {        
#if !TESSELLATION
            return 1;
#endif

            return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
        }

       

        #define GET_BLEND_FROMTEXTUREARRAY(tex,uv1,uv2,blendfactor, output) \
        { \
            float4 o1 = UNITY_SAMPLE_TEX2DARRAY(tex, uv1) * (1 - blendfactor); \
            float4 o2 = UNITY_SAMPLE_TEX2DARRAY(tex, uv2) * (blendfactor); \
            output = o1 + o2; \
        }

        #define GET_BLEND_FROMTEXTUREARRAY_TRIPLANAR(tex, uvxy1, uvxz1, uvyz1, uvxy2, uvxz2, uvyz2, bf, output) \
        { \
            float4 o1 = UNITY_SAMPLE_TEX2DARRAY(tex, uvxy1) * (1 - blendfactor); \
            float4 o2 = UNITY_SAMPLE_TEX2DARRAY(tex, uvxy2) * (blendfactor); \
            float4 result1 = (o1 + o2)  * bf.z; \
            o1 = UNITY_SAMPLE_TEX2DARRAY(tex, uvxz1) * (1 - blendfactor); \
            o2 = UNITY_SAMPLE_TEX2DARRAY(tex, uvxz2) * (blendfactor); \
            float4 result2 = (o1 + o2)  * bf.y; \
            o1 = UNITY_SAMPLE_TEX2DARRAY(tex, uvyz1) * (1 - blendfactor); \
            o2 = UNITY_SAMPLE_TEX2DARRAY(tex, uvyz2) * (blendfactor); \
            float4 result3 = (o1 + o2)  * bf.x; \
            output = result1 + result2 + result3; \
        }

        #define GET_BLEND_FROMTEXTUREARRAY_TRIPLANAR_RESULTSONLY(tex, uvxy1, uvxz1, uvyz1, uvxy2, uvxz2, uvyz2, bf, result1, result2, result3) \
        { \
            float4 o1 = UNITY_SAMPLE_TEX2DARRAY(tex, uvxy1) * (1 - blendfactor); \
            float4 o2 = UNITY_SAMPLE_TEX2DARRAY(tex, uvxy2) * (blendfactor); \
            result3 = (o1 + o2)  * bf.z; \
            o1 = UNITY_SAMPLE_TEX2DARRAY(tex, uvxz1) * (1 - blendfactor); \
            o2 = UNITY_SAMPLE_TEX2DARRAY(tex, uvxz2) * (blendfactor); \
            result2 = (o1 + o2)  * bf.y; \
            o1 = UNITY_SAMPLE_TEX2DARRAY(tex, uvyz1) * (1 - blendfactor); \
            o2 = UNITY_SAMPLE_TEX2DARRAY(tex, uvyz2) * (blendfactor); \
            result1 = (o1 + o2)  * bf.x; \
        }

        void disp(inout appdata_full v)
        {
#if !TESSELLATION
            return;
#endif


            float4 result;

            float blendvalue = (tex2Dlod(_BlendMap, float4(v.texcoord.xy, 0, 0)).r) * _BlendScale;

#if PULSATING
            blendvalue += sin(_Time.y * _PulseFrequency) * _PulseAmplitude;
#endif

            _SliceRange += v.texcoord2.x * _UV3Power * (blendvalue + _BlendShift);
            _SliceRange = max(0, _SliceRange);
            int textureindex = _SliceRange;
            float blendfactor = _SliceRange % 1;

            float3 position = v.vertex.xyz * _MapScale;
            float3 normal = v.normal;

            float3 bf = normalize(abs(normal));
            bf /= dot(bf, (float3)1);

            float2 trix = position.zy;
            float2 triy = position.xz;
            float2 triz = position.xy;

            trix += _TransformTex.xy;
            triy += _TransformTex.xy;
            triz += _TransformTex.xy;

            trix *= _TransformTex.zw;
            triy *= _TransformTex.zw;
            triz *= _TransformTex.zw;

            if (normal.x < 0) {
                trix.x = -trix.x;
            }
            if (normal.y < 0) {
                triy.x = -triy.x;
            }
            if (normal.z >= 0) {
                triz.x = -triz.x;
            }

            float3 xy1 = fixed3(triz, textureindex);
            float3 xy2 = fixed3(triz, textureindex + 1);
            float3 xz1 = fixed3(triy, textureindex);
            float3 xz2 = fixed3(triy, textureindex + 1);
            float3 yz1 = fixed3(trix, textureindex);
            float3 yz2 = fixed3(trix, textureindex + 1);

          
            float4 o1 = UNITY_SAMPLE_TEX2DARRAY_LOD(_ParallaxMap, xy1,0) * (1 - blendfactor);
            float4 o2 = UNITY_SAMPLE_TEX2DARRAY_LOD(_ParallaxMap, xy2,0) * (blendfactor);
            float4 result1 = (o1 + o2) * bf.z;
            o1 = UNITY_SAMPLE_TEX2DARRAY_LOD(_ParallaxMap, xz1,0) * (1 - blendfactor);
            o2 = UNITY_SAMPLE_TEX2DARRAY_LOD(_ParallaxMap, xz2,0) * (blendfactor);
            float4 result2 = (o1 + o2) * bf.y;
            o1 = UNITY_SAMPLE_TEX2DARRAY_LOD(_ParallaxMap, yz1,0) * (1 - blendfactor);
            o2 = UNITY_SAMPLE_TEX2DARRAY_LOD(_ParallaxMap, yz2,0) * (blendfactor);
            float4 result3 = (o1 + o2) * bf.x;
            float4 output = result1 + result2 + result3;

            float  displacement = output.r;

            displacement *= _TesselationPower;
            displacement += _TesselationOffset;

#if USE_COLOR_FOR_SEAMLESS_TESSELLATION
            float factor = min(_Tess_MinEdgeDistance, min(v.color.r, min(v.color.g, v.color.b)));
            displacement *= factor;
#endif
            //if (factor >= _Tess_MinEdgeDistance)
            //{
            //    v.vertex.xyz += v.normal *displacement;
            //}
            //else
            //{
            //    float interpolation = factor / _Tess_MinEdgeDistance;
            //    v.vertex.xyz += v.normal *displacement * interpolation;
            //}


            v.vertex.xyz += v.normal * displacement;



        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 result;
            
            float blendvalue = (tex2D(_BlendMap, IN.uv_MainTex).r) * _BlendScale;
#if PULSATING
            blendvalue += sin(_Time.y * _PulseFrequency) * _PulseAmplitude;
#endif

            _SliceRange += IN.uv3_IndexTex.x * _UV3Power * (blendvalue+ _BlendShift);
            _SliceRange = max(0, _SliceRange);
            int textureindex = _SliceRange;
            float blendfactor = _SliceRange % 1;

            float3 position = mul(unity_WorldToObject, float4(IN.worldPos, 1)) * _MapScale;
            float3 normal = WorldNormalVector(IN, o.Normal);

            float3 bf = normalize(abs(normal));
            bf /= dot(bf, (float3)1);

            float2 trix = position.zy;
            float2 triy = position.xz;
            float2 triz = position.xy;

            trix += _TransformTex.xy;
            triy += _TransformTex.xy;
            triz += _TransformTex.xy;

            trix *= _TransformTex.zw;
            triy *= _TransformTex.zw;
            triz *= _TransformTex.zw;

            if (normal.x < 0) {
                trix.x = -trix.x;
            }
            if (normal.y < 0) {
                triy.x = -triy.x;
            }
            if (normal.z >= 0) {
                triz.x = -triz.x;
            }

            float3 xy1 = fixed3(triz, textureindex);
            float3 xy2 = fixed3(triz, textureindex + 1);
            float3 xz1 = fixed3(triy, textureindex);
            float3 xz2 = fixed3(triy, textureindex + 1);
            float3 yz1 = fixed3(trix, textureindex);
            float3 yz2 = fixed3(trix, textureindex + 1);       
       
            float4 parallaxX, parallaxY, parallaxZ;
            GET_BLEND_FROMTEXTUREARRAY_TRIPLANAR_RESULTSONLY(_ParallaxMap, xy1, xz1, yz1, xy2, xz2, yz2, bf, parallaxX, parallaxY, parallaxZ);
            xy1.xy += ParallaxOffset(parallaxZ.r, _Parallax, IN.viewDir);
            xy2.xy += ParallaxOffset(parallaxZ.r, _Parallax, IN.viewDir);
            xz1.xy += ParallaxOffset(parallaxY.r, _Parallax, IN.viewDir);
            xz2.xy += ParallaxOffset(parallaxY.r, _Parallax, IN.viewDir);
            yz1.xy += ParallaxOffset(parallaxX.r, _Parallax, IN.viewDir);
            yz2.xy += ParallaxOffset(parallaxX.r, _Parallax, IN.viewDir);
      
            GET_BLEND_FROMTEXTUREARRAY_TRIPLANAR(_DiffuseMap, xy1, xz1, yz1, xy2, xz2, yz2, bf, result);        
            o.Albedo = result.rgb;
            o.Alpha = result.a;
                     
            float4 normalX, normalY, normalZ;
            GET_BLEND_FROMTEXTUREARRAY_TRIPLANAR_RESULTSONLY(_BumpMap, xy1, xz1, yz1, xy2, xz2, yz2, bf, normalX, normalY, normalZ);        
            
            float4 normalvalue = normalX + normalY + normalZ;
            o.Normal = UnpackScaleNormalArray(normalvalue, _BumpScale);

            GET_BLEND_FROMTEXTUREARRAY_TRIPLANAR(_MetallicGlossMap, xy1, xz1, yz1, xy2, xz2, yz2, bf, result);
            o.Metallic = result * _Metallic;
            o.Smoothness = result.a * _Glossiness;
          
            GET_BLEND_FROMTEXTUREARRAY_TRIPLANAR(_OcclusionMap, xy1, xz1, yz1, xy2, xz2, yz2, bf, result); 
            o.Occlusion = result * _OcclusionStrength;

            GET_BLEND_FROMTEXTUREARRAY_TRIPLANAR(_EmissionMap, xy1, xz1, yz1, xy2, xz2, yz2, bf, result); 
            o.Emission = result * _EmissionColor;      
        }
        ENDCG
    }
    FallBack "Diffuse"
    CustomEditor "TextureArrayGUI"
}
