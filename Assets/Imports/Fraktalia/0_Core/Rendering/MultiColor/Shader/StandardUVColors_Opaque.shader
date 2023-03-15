Shader "Fraktalia/Core/StandardExpansion/UVColors/Opaque"
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

        _UVColorPower("_UVColor Power", float) = 0.00390625
        _UVColorInitial("Initial UV Color", Color) = (1,1,1)

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert
       
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct appdata
        {
            float4 vertex    : POSITION;  // The vertex position in model space.
            float3 normal    : NORMAL;    // The vertex normal in model space.
            float4 texcoord  : TEXCOORD0; // The first UV coordinate.
            float4 texcoord1 : TEXCOORD1; // The second UV coordinate.
            float4 texcoord2 : TEXCOORD2; // The third UV coordinate.
            float4 texcoord3 : TEXCOORD3; // The fourfth UV coordinate.
            float4 texcoord4 : TEXCOORD4; // The fifth UV coordinate. // requires Unity 2018.2+
            float4 texcoord5 : TEXCOORD5; // The sixth UV coordinate. // requires Unity 2018.2+
            //float4 texcoord6 : TEXCOORD6; // The seventh UV coordinate. // requires Unity 2018.2+
            //float4 texcoord7 : TEXCOORD7; // The eigthieth UV coordinate. // requires Unity 2018.2+ // maximum amount of channels supported
            float4 tangent   : TANGENT;   // The tangent vector in Model Space (used for normal mapping).
            float4 color     : COLOR;     // Per-vertex color
        };

        struct Input
        {
            float2 uv_MainTex;
            float3 color;
            float3 viewDir;
        };

        sampler2D _MainTex;
    
        sampler2D _BumpMap;
        sampler2D _ParallaxMap;
        sampler2D _MetallicGlossMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;


      

        float _HeightScale;
        float _UVColorPower;
       

        
        sampler2D _Green;
        sampler2D _Blue;

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _BumpScale;
        float4 _EmissionColor;
        float _OcclusionStrength;
        float3 _UVColorInitial;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            o.uv_MainTex = v.texcoord.xy;
            o.color = float3(v.texcoord2.x, v.texcoord3.x, v.texcoord4.x);
          
        }


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
           

                     
            fixed2 uv_texture1 = IN.uv_MainTex.xy;
          
            float height1 = tex2D(_BumpMap, uv_texture1).r;
          
            uv_texture1.xy += ParallaxOffset(height1, _HeightScale, IN.viewDir);

            float3 uvcolor = _UVColorInitial + IN.color * _UVColorPower;

            fixed4 c1 = tex2D(_MainTex, uv_texture1);
            o.Albedo = c1.rgb * _Color.rgb * uvcolor.rgb;

            half4 bump1 = tex2D(_BumpMap, uv_texture1);         
            o.Normal = UnpackScaleNormal(bump1, _BumpScale);

            half4 m1 = tex2D(_MetallicGlossMap, uv_texture1);
          

            o.Metallic = (m1.r) * _Metallic;
            o.Smoothness = (m1.a) * _Glossiness;

            fixed4 o1 = tex2D(_OcclusionMap, uv_texture1);       
            o.Occlusion = o1.r * _OcclusionStrength;

            fixed4 e1 = tex2D(_EmissionMap, uv_texture1);
          
            o.Emission = e1 * _EmissionColor;

            o.Alpha = c1.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}


