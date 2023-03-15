Shader "Fraktalia/Core/MultiTexture/TextureAtlas_UV3_Triplanar"
{
    Properties
    {
        _TransformTex("Texture Transform", Vector) = (0, 0, 1, 1)

        [Header(Main Maps)]    
        [SingleLine(_Color)] _DiffuseMap ("Diffuse (Albedo) Maps", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1,1,1,1)
                
        [SingleLine(_Metallic)] _MetallicGlossMap("Metallic Maps", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        
        [HideInInspector] _Metallic("Metallic", Range(0,1)) = 0.0

         
        [SingleLine(_BumpScale)] _BumpMap("Normal Maps", 2D) = "bump" {}
        [HideInInspector] _BumpScale("Normal Scale", Float) = 1.0
            

        [SingleLine(_Parallax)]
        _ParallaxMap("Height Maps", 2D) = "grey" {}
        [HideInInspector]_Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
        
        [SingleLine(_OcclusionStrength)]
        _OcclusionMap("Occlusion Maps", 2D) = "white" {}
        [HideInInspector]_OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
           
        [SingleLine(_EmissionColor)]
        _EmissionMap("Emission Maps", 2D) = "black" {}       
        [HideInInspector]_EmissionColor("Emission Color", Color) = (0,0,0)

        _TextureAtlas_Width("Texture Atlas Width", float) = 2
        _TextureAtlas_Height("Texture Atlas Width", float) = 2

        [HideInInspector]_IndexTex("Albedo (RGB)", 2D) = "black" {}
        [HideInInspector]_IndexTex2("Albedo (RGB)", 2D) = "black" {}

        [Header(Texture Blending)]
        [SingleLine]
        _BlendMap("Blend Map", 2D) = "white" {}
        _BlendScale("Blend Scale", Float) = 1.0
        _BlendShift("Blend Shift", Float) = 0.5
        _SliceRange("Initial Slice", Range(-16,16)) = 6
        _UV3Power("UV3 Power", float) = 1
       
        [MultiCompileOption(USEBASEMATERIAL)]
        USEBASEMATERIAL("Use base material", float) = 0
        [KeywordDependent(USEBASEMATERIAL)] _BaseSupression("Base Supression", float) = 1 
        [KeywordDependent(USEBASEMATERIAL)] _BaseUVMultiplier("Base UV Multiplier", float) = 1
        [SingleLine(_BaseColor, USEBASEMATERIAL)] _BaseDiffuseMap("Diffuse (Albedo) Maps", 2D) = "white" {}
        [HideInInspector] _BaseColor("Color", Color) = (1,1,1,1)
        [SingleLine(_BaseMetallic, USEBASEMATERIAL)] _BaseMetallicGlossMap("Metallic Maps", 2D) = "white" {}
        [KeywordDependent(USEBASEMATERIAL)]_BaseGlossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _BaseMetallic("Metallic", Range(0,1)) = 0.0
        [SingleLine(_BaseBumpScale, USEBASEMATERIAL)] _BaseBumpMap("Normal Maps", 2D) = "bump" {}
        [HideInInspector] _BaseBumpScale("Normal Scale", Float) = 1.0
        [SingleLine(_BaseParallax, USEBASEMATERIAL)]
        _BaseParallaxMap("Height Maps", 2D) = "grey" {}
        [HideInInspector]_BaseParallax("Height Scale", Range(0.005, 0.08)) = 0.02
        [SingleLine(_BaseOcclusionStrength, USEBASEMATERIAL)]
        _BaseOcclusionMap("Occlusion Maps", 2D) = "white" {}
        [HideInInspector]_BaseOcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [SingleLine(_BaseEmissionColor, USEBASEMATERIAL)]
        _BaseEmissionMap("Emission Maps", 2D) = "black" {}
        [HideInInspector]_BaseEmissionColor("Emission Color", Color) = (0,0,0)


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
      
        CGPROGRAM
        #pragma shader_feature USE_COLOR_FOR_SEAMLESS_TESSELLATION
        #pragma shader_feature TESSELLATION
        #pragma shader_feature USEBASEMATERIAL
        #pragma shader_feature PULSATING

#if TESSELLATION
        #pragma surface surf Standard fullforwardshadows vertex:disp tessellate:tessDistance addshadow  
#else
        #pragma surface surf Standard fullforwardshadows
#endif


        #pragma target 2.5


        #include "Tessellation.cginc"
        #include "MultiTexture.cginc"

        sampler2D _DiffuseMap;
        sampler2D _BumpMap;
        sampler2D _ParallaxMap;
        sampler2D _MetallicGlossMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;
        float _Glossiness;
        float _Metallic;
        float4 _Color;
        float _BumpScale;
        float4 _EmissionColor;
        float _OcclusionStrength;
        float _Parallax;

#if USEBASEMATERIAL
        sampler2D _BaseDiffuseMap;
        sampler2D _BaseBumpMap;
        sampler2D _BaseParallaxMap;
        sampler2D _BaseMetallicGlossMap;
        sampler2D _BaseOcclusionMap;
        sampler2D _BaseEmissionMap;
    
        half _BaseGlossiness;
        half _BaseMetallic;
        float4 _BaseColor;
        float _BaseBumpScale;
        float4 _BaseEmissionColor;
        float _BaseOcclusionStrength;
        float _BaseParallax;

        float _BaseSupression;
        float _BaseUVMultiplier;
#endif

#if PULSATING
        float _PulseFrequency;
        float _PulseAmplitude;
#endif

        float4 _TransformTex;

        sampler2D _IndexTex;
        sampler2D _IndexTex2;      
        sampler2D _BlendMap;

        float _BlendScale;
        float _BlendShift;
        float _UV3Power;
        float _SliceRange;
        
        float _MapScale;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float2 uv_MainTex;
            float2 uv3_IndexTex;
            float2 uv4_IndexTex2; //2 at end to match defined _IndexTex2 to prevent redefinition
            float3 viewDir;        
            INTERNAL_DATA
        };

      

        float _Tess;
        float _TesselationPower;
        float _Tess_MinEdgeDistance;
        float _TesselationOffset;
        float minDist;
        float maxDist;

        float _TextureAtlas_Width;
        float _TextureAtlas_Height;


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

#pragma shader_feature SEAMLESS_TESSELATION

        float4 tessDistance(appdata_full v0, appdata_full v1, appdata_full v2) {        
#if !TESSELLATION
            return 1;
#endif

            return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
        }

        float CalculateTesselationLayer(float2 trix, float2 triy, float2 triz, float3 bf, float power, float textureindex)
        {
            float3 xy1 = fixed3(triz, textureindex);
            float3 xz1 = fixed3(triy, textureindex);
            float3 yz1 = fixed3(trix, textureindex);
            float result = _GetFromTextureAtlas_LOD(_ParallaxMap, xy1, xz1, yz1, bf, _TextureAtlas_Width, _TextureAtlas_Height).r;
            return result * power;
        }

#if USEBASEMATERIAL
        float CalculateBaseTesselationLayer(float2 trix, float2 triy, float2 triz, float3 bf, float power)
        {
            float3 xy1 = float3(triz * _BaseUVMultiplier,0);
            float3 xz1 = float3(triy * _BaseUVMultiplier,0);
            float3 yz1 = float3(trix * _BaseUVMultiplier,0);
            float result = _GetFromTextureAtlas_LOD(_BaseParallaxMap, xy1, xz1, yz1, bf).r;          
            return result * power;
        }
#endif

        void disp(inout appdata_full v)
        {           
#if !TESSELLATION
            return;
#endif
              
            float blendvalue = (tex2Dlod(_BlendMap, float4(v.texcoord.xy, 0, 0)).r) * _BlendScale;
#if PULSATING
            blendvalue += sin(_Time.y * _PulseFrequency) * _PulseAmplitude;
#endif
            _SliceRange += v.texcoord2.x * _UV3Power * (blendvalue + _BlendShift);
            _SliceRange = max(0, _SliceRange);
            int textureindex = _SliceRange;
            int textureindexnext = clamp(textureindex - 1, 0, 3);
            textureindex = clamp(textureindex, 0, 3);
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

            float2 uv3_IndexTex = float2(v.texcoord2.x, v.texcoord2.y);
               
#if USEBASEMATERIAL
            textureindex = _SliceRange;
            textureindexnext = clamp(textureindex - 1, -1, 3);
            textureindex = clamp(textureindex, -1, 3);

            float layer1;
            float layer2;
            if ((textureindex) < 0) {
                layer1 = CalculateBaseTesselationLayer(trix, triy, triz, bf, blendfactor);
            }
            else {
                layer1 = CalculateTesselationLayer(trix, triy, triz, bf, blendfactor, textureindex);
            }

            if ((textureindexnext) < 0) {
                layer2 = CalculateBaseTesselationLayer(trix, triy, triz, bf, 1 - blendfactor);
            }
            else {
                layer2 = CalculateTesselationLayer(trix, triy, triz, bf, 1 - blendfactor, textureindexnext);
            }
#else 
            float layer1 = CalculateTesselationLayer(trix, triy, triz, bf, blendfactor, textureindex);
            float layer2 = CalculateTesselationLayer(trix, triy, triz, bf, 1 - blendfactor, textureindexnext);
#endif

            float displacement = layer1 + layer2;

#if USEBASEMATERIAL
            //displacement += base;
#endif


            displacement *= _TesselationPower;
            displacement += _TesselationOffset;


#if USE_COLOR_FOR_SEAMLESS_TESSELLATION
            float factor = min(_Tess_MinEdgeDistance, min(v.color.r, min(v.color.g, v.color.b)));
            displacement *= factor;
#endif     
            
            v.vertex.xyz += v.normal * displacement; 
        }

        struct Result {
            float4  Diffuse;
            half3   Normal;
            float4  Metallic;
            float   Smoothness;
            float4  Occlusion;
            float4  Emission;
        };

        Result CalculateLayer(Input IN, float2 trix, float2 triy, float2 triz, float3 bf, float power, float textureindex)
        {
            float4 result;
            Result output;          
            float3 xy1 = fixed3(triz, textureindex);
            float3 xz1 = fixed3(triy, textureindex);
            float3 yz1 = fixed3(trix, textureindex);
             
            float3 viewDir = IN.viewDir;
            float4 parallaxX, parallaxY, parallaxZ;
            _GetFromTextureAtlas_RESULTSONLY(_ParallaxMap, xy1, xz1, yz1, bf, _TextureAtlas_Width, _TextureAtlas_Height, parallaxX, parallaxY, parallaxZ);

            xy1.xy += ParallaxOffset((parallaxZ.w), _Parallax, viewDir) * (power/ _TextureAtlas_Width);
            xz1.xy += ParallaxOffset((parallaxY.w), _Parallax, viewDir) * (power/ _TextureAtlas_Width);
            yz1.xy += ParallaxOffset((parallaxX.w), _Parallax, viewDir) * (power/ _TextureAtlas_Width);
                  
            output.Diffuse = _GetFromTextureAtlas(_DiffuseMap, xy1, xz1, yz1, bf, _TextureAtlas_Width, _TextureAtlas_Height) * power;

            float4 normalX, normalY, normalZ;
            _GetFromTextureAtlas_RESULTSONLY(_BumpMap, xy1, xz1, yz1, bf, _TextureAtlas_Width, _TextureAtlas_Height, normalX, normalY, normalZ);
            float4 normalvalue = (normalX + normalY + normalZ) ;
            output.Normal = UnpackScaleNormal(normalvalue , _BumpScale*3 * power);

            result = _GetFromTextureAtlas(_MetallicGlossMap, xy1, xz1, yz1, bf, _TextureAtlas_Width, _TextureAtlas_Height);
            output.Metallic = result * power;
            output.Smoothness = result.a * power;

            result = _GetFromTextureAtlas(_OcclusionMap, xy1, xz1, yz1, bf, _TextureAtlas_Width, _TextureAtlas_Height);
            output.Occlusion = result * power;

            result = _GetFromTextureAtlas(_EmissionMap, xy1, xz1, yz1, bf, _TextureAtlas_Width, _TextureAtlas_Height);
            output.Emission = result * power;

            return output;
        }

#if USEBASEMATERIAL
        Result CalculateBaseLayer(Input IN, float2 trix, float2 triy, float2 triz, float3 bf, float power)
        {
            float4 result;
            Result output;
            float3 xy1 = fixed3(triz, 0);
            float3 xz1 = fixed3(triy, 0);
            float3 yz1 = fixed3(trix, 0);

            float3 viewDir = IN.viewDir;
            float4 parallaxX, parallaxY, parallaxZ;
            _GetFromTextureAtlas_RESULTSONLY(_BaseParallaxMap, xy1, xz1, yz1, bf, parallaxX, parallaxY, parallaxZ);

            xy1.xy += ParallaxOffset((parallaxZ.w), _BaseParallax, viewDir) * (power / _TextureAtlas_Width);
            xz1.xy += ParallaxOffset((parallaxY.w), _BaseParallax, viewDir) * (power / _TextureAtlas_Width);
            yz1.xy += ParallaxOffset((parallaxX.w), _BaseParallax, viewDir) * (power / _TextureAtlas_Width);

            output.Diffuse = _GetFromTextureAtlas(_BaseDiffuseMap, xy1, xz1, yz1, bf) * power * _BaseColor;

            float4 normalX, normalY, normalZ;
            _GetFromTextureAtlas_RESULTSONLY(_BaseBumpMap, xy1, xz1, yz1, bf, normalX, normalY, normalZ);
            float4 normalvalue = (normalX + normalY + normalZ);
            output.Normal = UnpackScaleNormal(normalvalue, _BaseBumpScale * 3 * power);

            result = _GetFromTextureAtlas(_BaseMetallicGlossMap, xy1, xz1, yz1, bf);
            output.Metallic = result * power * _BaseMetallic;
            output.Smoothness = result.a * power * _BaseGlossiness;

            result = _GetFromTextureAtlas(_BaseOcclusionMap, xy1, xz1, yz1, bf);
            output.Occlusion = result * power *_BaseOcclusionStrength;

            result = _GetFromTextureAtlas(_BaseEmissionMap, xy1, xz1, yz1, bf);
            output.Emission = result * power * _BaseEmissionColor;

            return output;
        }
#endif

        Result MergeResults(Result result1, Result result2)
        {
            Result output;
            output.Diffuse.rgb = (result1.Diffuse.rgb + result2.Diffuse.rgb);
            output.Normal = result1.Normal + result2.Normal;
            output.Metallic = (result1.Metallic + result2.Metallic);
            output.Smoothness = (result1.Smoothness + result2.Smoothness);
            output.Occlusion = (result1.Occlusion + result2.Occlusion);
            output.Emission = result1.Emission + result2.Emission;
            output.Diffuse.a = (result1.Diffuse.a + result2.Diffuse.a );
            return output;
        }


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            _SliceRange;

            float blendvalue = (tex2D(_BlendMap, IN.uv_MainTex).r) * _BlendScale;
#if PULSATING
            blendvalue += sin(_Time.y * _PulseFrequency) * _PulseAmplitude;
#endif
            _SliceRange += IN.uv3_IndexTex.x * _UV3Power * (blendvalue + _BlendShift);
            _SliceRange = max(0, _SliceRange);           
            float blendfactor = _SliceRange % 1;
            

            float4 result;                  
            float3 position = mul(unity_WorldToObject, float4(IN.worldPos, 1)) * _MapScale;
            float3 normal = WorldNormalVector(IN, o.Normal);
            float3 localNormal = mul(unity_WorldToObject, float4(normal, 0));
            float3 bf = normalize(abs(localNormal));
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
                            
            int textureindex = _SliceRange;
            int textureindexnext = clamp(textureindex - 1, 0, 3);
            textureindex = clamp(textureindex, 0, 3);

#if USEBASEMATERIAL
            textureindex = _SliceRange;
            textureindexnext = clamp(textureindex - 1, -1, 3);
            textureindex = clamp(textureindex, -1, 3);
            
            Result layer1;
            Result layer2;
            if ((textureindex) < 0) {               
                layer1 = CalculateBaseLayer(IN, trix, triy, triz, bf, blendfactor);
            }
            else {
                layer1 = CalculateLayer(IN, trix, triy, triz, bf, blendfactor, textureindex);
            }
           
            if ((textureindexnext) < 0) {
                layer2 = CalculateBaseLayer(IN, trix, triy, triz, bf, 1 - blendfactor);
            }
            else {
                layer2 = CalculateLayer(IN, trix, triy, triz, bf, 1 - blendfactor, textureindexnext);
            }

           

#else         
            Result layer1 = CalculateLayer(IN, trix, triy, triz, bf, blendfactor, textureindex);
            Result layer2 = CalculateLayer(IN, trix, triy, triz, bf, 1 - blendfactor, textureindexnext);
#endif



           
               
            Result merged = MergeResults(layer1, layer2);

      
            o.Albedo = (merged.Diffuse.rgb) * _Color.rgb;
            o.Normal = merged.Normal;
            o.Metallic =  (merged.Metallic) * _Metallic;
            o.Smoothness = (merged.Smoothness)* _Glossiness;
            o.Occlusion = (merged.Occlusion) * _OcclusionStrength;
            o.Emission = merged.Emission * _EmissionColor;
            o.Alpha = (merged.Diffuse.a) * _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
    CustomEditor "TextureArrayGUI"
}
