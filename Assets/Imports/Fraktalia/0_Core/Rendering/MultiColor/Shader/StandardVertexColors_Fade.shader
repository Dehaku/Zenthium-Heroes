Shader "Fraktalia/Core/StandardExpansion/VertexColor/Fade"
{
    Properties
    {
         [Header(Main Maps)]
        _MainTex("Albedo (RGB) Maps", 2D) = "white" {}
         _Color("Color", Color) = (1,1,1,1)

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
        _EmissionMap("Emission Maps", 2D) = "white" {}
        [HideInInspector]_EmissionColor("Emission Color", Color) = (0,0,0)


    }
        SubShader
        {
            Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
            LOD 200

            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard fullforwardshadows alpha:fade
            #pragma require 2darray
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0


            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _ParallaxMap;
            sampler2D _MetallicGlossMap;
            sampler2D _OcclusionMap;
            sampler2D _EmissionMap;


            float _HeightScale;

            struct Input
            {
                float2 uv_MainTex;
                float3 viewDir;

                float4 color : COLOR;
            };


            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
            float _BumpScale;
            float4 _EmissionColor;
            float _OcclusionStrength;

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



                    fixed2 uv_texture1 = IN.uv_MainTex.xy;

                    float height1 = tex2D(_BumpMap, uv_texture1).r;

                    uv_texture1.xy += ParallaxOffset(height1, _HeightScale, IN.viewDir);

                    fixed4 c1 = tex2D(_MainTex, uv_texture1) * IN.color;
                    o.Albedo = c1.rgb * _Color.rgb;

                    half4 bump1 = tex2D(_BumpMap, uv_texture1);
                    o.Normal = UnpackScaleNormalArray(bump1, _BumpScale);

                    half4 m1 = tex2D(_MetallicGlossMap, uv_texture1);


                    o.Metallic = (m1.r) * _Metallic;
                    o.Smoothness = (m1.a) * _Glossiness;

                    fixed4 o1 = tex2D(_OcclusionMap, uv_texture1);
                    o.Occlusion = o1.r * _OcclusionStrength;

                    fixed4 e1 = tex2D(_EmissionMap, uv_texture1);

                    o.Emission = e1 * _EmissionColor;

                    o.Alpha = c1.a * _Color.a;
                }
                ENDCG
        }
            FallBack "Diffuse"
}

