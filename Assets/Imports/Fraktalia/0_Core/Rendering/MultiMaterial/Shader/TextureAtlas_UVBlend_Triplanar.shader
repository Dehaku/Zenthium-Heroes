Shader "Fraktalia/Core/MultiTexture/TextureAtlas_UVBlend_Triplanar"
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
        _UV3XPower("UV3 X Power, Shift, Slice", Vector) = (0.025,0,0)
        _UV3YPower("UV3 Y Power, Shift, Slice", Vector) = (0,0,1)
        _UV4XPower("UV4 X Power, Shift, Slice", Vector) = (0,0,2)
        _UV4YPower("UV4 Y Power, Shift, Slice", Vector) = (0,0,3)

     

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
        [KeywordDependent(PULSATING)] _PulseFrequency("Pulse Frequency UV3X, Y, UV4X, Y", Vector) = (2,2,2,2)
        [KeywordDependent(PULSATING)] _PulseAmplitude("Pulse Amplitude UV3X, Y, UV4X, Y", Vector) = (0,0,0,0)


      

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
        float4 _PulseFrequency;
        float4 _PulseAmplitude;
#endif

        float4 _TransformTex;

        sampler2D _IndexTex;
        sampler2D _IndexTex2;

        
        float3 _UV3XPower;
        float3 _UV3YPower;
        float3 _UV4XPower;
        float3 _UV4YPower;
       
    
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

        float2 convertToSliceUV(float3 uv)
        {
            float2 texUV = abs(uv.xy);
            

            float2 index = uv.z;

            float texturesizeX = 1 / _TextureAtlas_Width;
            float texturesizeY = 1 / _TextureAtlas_Height;

            float2 uv1slice = float2(texUV.x % texturesizeX, texUV.y % texturesizeY);
           
            uv1slice.x += texturesizeX * (int)(index % _TextureAtlas_Width);
            uv1slice.y += texturesizeY * ((int)(index / _TextureAtlas_Width) % _TextureAtlas_Height);
            return uv1slice;
        }

        float4 GET_FROMTEXTUREARRAY_TRIPLANAR(sampler2D tex, float3 uvxy1, float3 uvxz1, float3 uvyz1, float3 bf)
        {
            float4 o1 = tex2D(tex, convertToSliceUV(uvxy1));
            float4 result1 = (o1)  * bf.z;
            o1 = tex2D(tex, convertToSliceUV(uvxz1));
            float4 result2 = (o1)  * bf.y;
            o1 = tex2D(tex, convertToSliceUV(uvyz1));
            float4 result3 = (o1)  * bf.x;
            return result1 + result2 + result3;
        }

        void GET_FROMTEXTUREARRAY_TRIPLANAR_RESULTSONLY(sampler2D tex, float3 uvxy1, float3 uvxz1, float3 uvyz1, float3 bf, out float4 result1, out float4 result2, out float4 result3)
        {
            float4 o1 = tex2D(tex, convertToSliceUV(uvxy1));
            result3 = (o1)  * bf.z;
            o1 = tex2D(tex, convertToSliceUV(uvxz1));
            result2 = (o1)  * bf.y;
            o1 = tex2D(tex, convertToSliceUV(uvyz1));
            result1 = (o1)  * bf.x;
        }

        float4 GET_FROMBASE_TRIPLANAR(sampler2D tex, float2 uvxy1, float2 uvxz1, float2 uvyz1, float3 bf)
        {
            float4 o1 = tex2D(tex, uvxy1);
            float4 result1 = (o1)*bf.z;
            o1 = tex2D(tex, uvxz1);
            float4 result2 = (o1)*bf.y;
            o1 = tex2D(tex, uvyz1);
            float4 result3 = (o1)*bf.x;
            return result1 + result2 + result3;
        }

        void GET_FROMBASE_TRIPLANAR_RESULTSONLY(sampler2D tex, float2 uvxy1, float2 uvxz1, float2 uvyz1, float3 bf, out float4 result1, out float4 result2, out float4 result3)
        {
            float4 o1 = tex2D(tex, uvxy1);
            result3 = (o1)*bf.z;
            o1 = tex2D(tex, uvxz1);
            result2 = (o1)*bf.y;
            o1 = tex2D(tex, uvyz1);
            result1 = (o1)*bf.x;
        }

        float CalculateTesselationLayer(float2 trix, float2 triy, float2 triz, float3 bf, float power, float textureindex)
        {
            float3 xy1 = fixed3(triz, textureindex);
            float3 xz1 = fixed3(triy, textureindex);
            float3 yz1 = fixed3(trix, textureindex);

            float o2 = tex2Dlod(_ParallaxMap, float4(convertToSliceUV(xy1), 0, 0)).r;
            float result1 = o2 * bf.z;
            o2 = tex2Dlod(_ParallaxMap, float4(convertToSliceUV(xz1), 0, 0)).r;
            float result2 = o2 * bf.y;
            o2 = tex2Dlod(_ParallaxMap, float4(convertToSliceUV(yz1), 0, 0)).r;
            float result3 = o2 * bf.x;
            float result = result1 + result2 + result3;
            return result * power;
        }

#if USEBASEMATERIAL
        float CalculateBaseTesselationLayer(float2 trix, float2 triy, float2 triz, float3 bf, float power)
        {
            float2 xy1 = triz * _BaseUVMultiplier;
            float2 xz1 = triy * _BaseUVMultiplier;
            float2 yz1 = trix * _BaseUVMultiplier;

            float o2 = tex2Dlod(_BaseParallaxMap, float4((xy1), 0, 0)).r;
            float result1 = o2 * bf.z;
            o2 = tex2Dlod(_BaseParallaxMap, float4((xz1), 0, 0)).r;
            float result2 = o2 * bf.y;
            o2 = tex2Dlod(_BaseParallaxMap, float4((yz1), 0, 0)).r;
            float result3 = o2 * bf.x;
            float result = result1 + result2 + result3;
            return result * power;
        }
#endif

        void disp(inout appdata_full v)
        {           
#if !TESSELLATION
            return;
#endif
              
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
            float2 uv4_IndexTex = float2(v.texcoord3.x, v.texcoord3.y);
       
#if PULSATING
            _UV3XPower.x += sin(_Time.y * _PulseFrequency.x) * _PulseAmplitude.x;
            _UV3YPower.x += sin(_Time.y * _PulseFrequency.y) * _PulseAmplitude.y;
            _UV4XPower.x += sin(_Time.y * _PulseFrequency.z) * _PulseAmplitude.z;
            _UV4YPower.x += sin(_Time.y * _PulseFrequency.w) * _PulseAmplitude.w;
#endif


            float layer1factor = clamp(uv3_IndexTex.x * _UV3XPower.x + _UV3XPower.y, 0, 1);
            float layer2factor = clamp(uv3_IndexTex.y * _UV3YPower.x + _UV3YPower.y, 0, 1);
            float layer3factor = clamp(uv4_IndexTex.x * _UV4XPower.x + _UV4XPower.y, 0, 1);
            float layer4factor = clamp(uv4_IndexTex.y * _UV4YPower.x + _UV4YPower.y, 0, 1);



            float sumPower = (layer1factor + layer2factor + layer3factor + layer4factor);
#if USEBASEMATERIAL
            float layer0factor = _BaseSupression;
            sumPower += layer0factor;
            layer0factor /= sumPower;
            float base = CalculateBaseTesselationLayer(trix, triy, triz, bf, layer0factor);
#endif

            layer1factor /= sumPower;
            layer2factor /= sumPower;
            layer3factor /= sumPower;
            layer4factor /= sumPower;


            float layeruv3 = CalculateTesselationLayer(trix, triy, triz, bf, layer1factor, _UV3XPower.z);
            float layeruv4 = CalculateTesselationLayer(trix, triy, triz, bf, layer2factor, _UV3YPower.z);
            float layeruv5 = CalculateTesselationLayer(trix, triy, triz, bf, layer3factor, _UV4XPower.z);
            float layeruv6 = CalculateTesselationLayer(trix, triy, triz, bf, layer4factor, _UV4YPower.z);
            float displacement = layeruv3 + layeruv4 + layeruv5 + layeruv6;

#if USEBASEMATERIAL
            displacement += base;
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
            GET_FROMTEXTUREARRAY_TRIPLANAR_RESULTSONLY(_ParallaxMap, xy1, xz1, yz1, bf, parallaxX, parallaxY, parallaxZ);

            xy1.xy += ParallaxOffset((parallaxZ.w), _Parallax, viewDir) * (power/ _TextureAtlas_Width);
            xz1.xy += ParallaxOffset((parallaxY.w), _Parallax, viewDir) * (power/ _TextureAtlas_Width);
            yz1.xy += ParallaxOffset((parallaxX.w), _Parallax, viewDir) * (power/ _TextureAtlas_Width);
                  
            output.Diffuse = GET_FROMTEXTUREARRAY_TRIPLANAR(_DiffuseMap, xy1, xz1, yz1, bf) * power;

            float4 normalX, normalY, normalZ;
            GET_FROMTEXTUREARRAY_TRIPLANAR_RESULTSONLY(_BumpMap, xy1, xz1, yz1, bf, normalX, normalY, normalZ);
            float4 normalvalue = (normalX + normalY + normalZ) ;
            output.Normal = UnpackScaleNormal(normalvalue , _BumpScale*3 * power);

            result = GET_FROMTEXTUREARRAY_TRIPLANAR(_MetallicGlossMap, xy1, xz1, yz1, bf);
            output.Metallic = result * power;
            output.Smoothness = result.a * power;

            result = GET_FROMTEXTUREARRAY_TRIPLANAR(_OcclusionMap, xy1, xz1, yz1, bf);
            output.Occlusion = result * power;

            result = GET_FROMTEXTUREARRAY_TRIPLANAR(_EmissionMap, xy1, xz1, yz1, bf);
            output.Emission = result * power;

            return output;
        }

#if USEBASEMATERIAL
        Result CalculateBaseLayer(Input IN, float2 trix, float2 triy, float2 triz, float3 bf, float power)
        {
            float4 result;
            Result output;
            float2 xy1 = triz * _BaseUVMultiplier;
            float2 xz1 = triy * _BaseUVMultiplier;
            float2 yz1 = trix * _BaseUVMultiplier;

            float3 viewDir = IN.viewDir;
            float4 parallaxX, parallaxY, parallaxZ;
            GET_FROMBASE_TRIPLANAR_RESULTSONLY(_BaseParallaxMap, xy1, xz1, yz1, bf, parallaxX, parallaxY, parallaxZ);

            xy1.xy += ParallaxOffset((parallaxZ.w), _BaseParallax, viewDir) * (power / _TextureAtlas_Width);
            xz1.xy += ParallaxOffset((parallaxY.w), _BaseParallax, viewDir) * (power / _TextureAtlas_Width);
            yz1.xy += ParallaxOffset((parallaxX.w), _BaseParallax, viewDir) * (power / _TextureAtlas_Width);

            output.Diffuse = GET_FROMBASE_TRIPLANAR(_BaseDiffuseMap, xy1, xz1, yz1, bf) * power * _BaseColor;

            float4 normalX, normalY, normalZ;
            GET_FROMBASE_TRIPLANAR_RESULTSONLY(_BaseBumpMap, xy1, xz1, yz1, bf, normalX, normalY, normalZ);
            float4 normalvalue = (normalX + normalY + normalZ);
            output.Normal = UnpackScaleNormal(normalvalue, _BaseBumpScale * 3 * power);

            result = GET_FROMBASE_TRIPLANAR(_BaseMetallicGlossMap, xy1, xz1, yz1, bf);
            output.Metallic = result * power * _BaseMetallic;
            output.Smoothness = result.a * power * _BaseGlossiness;

            result = GET_FROMBASE_TRIPLANAR(_BaseOcclusionMap, xy1, xz1, yz1, bf);
            output.Occlusion = result * power *_BaseOcclusionStrength;

            result = GET_FROMBASE_TRIPLANAR(_BaseEmissionMap, xy1, xz1, yz1, bf);
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
        
#if PULSATING
            _UV3XPower.x += sin(_Time.y * _PulseFrequency.x) * _PulseAmplitude.x;
            _UV3YPower.x += sin(_Time.y * _PulseFrequency.y) * _PulseAmplitude.y;
            _UV4XPower.x += sin(_Time.y * _PulseFrequency.z) * _PulseAmplitude.z;
            _UV4YPower.x += sin(_Time.y * _PulseFrequency.w) * _PulseAmplitude.w;
#endif


            float layer1factor = clamp(IN.uv3_IndexTex.x * _UV3XPower.x + _UV3XPower.y, 0,1);
            float layer2factor = clamp(IN.uv3_IndexTex.y * _UV3YPower.x + _UV3YPower.y, 0,1);
            float layer3factor = clamp(IN.uv4_IndexTex2.x * _UV4XPower.x + _UV4XPower.y, 0,1);
            float layer4factor = clamp(IN.uv4_IndexTex2.y * _UV4YPower.x + _UV4YPower.y, 0,1);
            
            float sumPower = (layer1factor + layer2factor + layer3factor + layer4factor);
           
#if USEBASEMATERIAL
            float layer0factor = _BaseSupression;
            sumPower += layer0factor;
            layer0factor /= sumPower;
            Result base = CalculateBaseLayer(IN, trix, triy, triz, bf, layer0factor);
#endif
            layer1factor /= sumPower;
            layer2factor /= sumPower;
            layer3factor /= sumPower;
            layer4factor /= sumPower;


            Result layeruv3 = CalculateLayer(IN, trix, triy, triz, bf, layer1factor, _UV3XPower.z);
            Result layeruv4 = CalculateLayer(IN, trix, triy, triz, bf, layer2factor, _UV3YPower.z);
            Result layeruv5 = CalculateLayer(IN, trix, triy, triz, bf, layer3factor, _UV4XPower.z);
            Result layeruv6 = CalculateLayer(IN, trix, triy, triz, bf, layer4factor, _UV4YPower.z);           
     
            Result merged = MergeResults(MergeResults(MergeResults(layeruv3, layeruv4), layeruv5), layeruv6);


#if USEBASEMATERIAL                  
            merged = MergeResults(merged, base);
#endif
      
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
