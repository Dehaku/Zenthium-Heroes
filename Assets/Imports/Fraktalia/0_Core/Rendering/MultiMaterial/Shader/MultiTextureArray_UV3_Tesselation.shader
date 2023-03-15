Shader "Fraktalia/Core/MultiTexture/MultiTextureArray_UV3_Tesselation"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}

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

        [Header(Tessellation Settings)]
        _Tess("Tessellation", Range(1,32)) = 4         
        minDist("Tess Min Distance", float) = 10
        maxDist("Tess Max Distance", float) = 25
        _TesselationPower("Power Tesselation", Range(-100, 100)) = 1
        _TesselationOffset("Offset Tesselation", Range(-100, 100)) = 1
        _Tess_MinEdgeDistance("MinEdge Tesselation", Range(0, 1)) = 1

       

            

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



        sampler2D _IndexTex;
        sampler2D _BlendMap;
        float _BlendScale;
        float _BlendShift;
        float _UV3Power;
        float _Parallax;

       
        struct Input
        {
            float2 uv_MainTex;
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
        float _Tess;
        float _TesselationPower;
        float _Tess_MinEdgeDistance;
        float _TesselationOffset;
        float minDist;
        float maxDist;

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

        float4 tessDistance(appdata_full v0, appdata_full v1, appdata_full v2) {        
           
            return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
        }

        void disp(inout appdata_full v)
        {
            float blendvalue = (tex2Dlod(_BlendMap, float4(v.texcoord.xy, 0, 0)).r) * _BlendScale;
            _SliceRange += v.texcoord2.x * _UV3Power * (blendvalue + _BlendShift);
            _SliceRange = max(0, _SliceRange);
            int textureindex = _SliceRange;
            float blendfactor = _SliceRange % 1;

            fixed3 uv_texture1 = fixed3(v.texcoord.x, v.texcoord.y, textureindex);
            fixed3 uv_texture2 = fixed3(v.texcoord.x, v.texcoord.y, textureindex + 1);


            float height1 = UNITY_SAMPLE_TEX2DARRAY_LOD(_ParallaxMap, uv_texture1,0).r * (1 - blendfactor);
            float height2 = UNITY_SAMPLE_TEX2DARRAY_LOD(_ParallaxMap, uv_texture2,0).r * (blendfactor);
            
            float  displacement = height1+height2;

            displacement *= _TesselationPower;
            displacement += _TesselationOffset;

            float factor = min(_Tess_MinEdgeDistance, min(v.color.r, min(v.color.g, v.color.b)));
            displacement *= factor;

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
           
            _SliceRange;

            float blendvalue = (tex2D(_BlendMap, IN.uv_MainTex).r) * _BlendScale;
            _SliceRange += IN.uv3_IndexTex.x * _UV3Power * (blendvalue+ _BlendShift);
            _SliceRange = max(0, _SliceRange);
            int textureindex = _SliceRange;
            float blendfactor = _SliceRange % 1;
                         

            fixed3 uv_texture1 = fixed3(IN.uv_MainTex.x, IN.uv_MainTex.y, textureindex);
            fixed3 uv_texture2 = fixed3(IN.uv_MainTex.x, IN.uv_MainTex.y, textureindex+1);

            float height1 = UNITY_SAMPLE_TEX2DARRAY(_ParallaxMap, uv_texture1).r * (1 - blendfactor);       
            float height2 = UNITY_SAMPLE_TEX2DARRAY(_ParallaxMap, uv_texture2).r * (blendfactor);
            uv_texture1.xy += ParallaxOffset(height1+height2, _Parallax, IN.viewDir);
            uv_texture2.xy += ParallaxOffset(height1+height2, _Parallax, IN.viewDir);


            fixed4 c1 = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMap, uv_texture1) * _Color * (1 - blendfactor);
            fixed4 c2 = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMap, uv_texture2) * _Color * (blendfactor);
            o.Albedo = c1.rgb +c2.rgb;

            half4 bump1 = UNITY_SAMPLE_TEX2DARRAY(_BumpMap, uv_texture1)* (1 - blendfactor);
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


            
            o.Alpha = c1.a+c2.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
    CustomEditor "TextureArrayGUI"
}
