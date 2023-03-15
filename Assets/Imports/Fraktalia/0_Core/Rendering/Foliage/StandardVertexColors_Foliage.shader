//WIP Grass shader

Shader "Fraktalia/Core/StandardExpansion/Foliage_UV3Pinned"
{
	Properties
	{
		[Header(Main Maps)]
		_MainTex("Albedo (RGB) Maps", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

		_Cutoff("Cutoff", Range(0,1)) = 1.0

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

		// Wind effect parameteres
		_WindFrecuency("Wind Frecuency",Range(0.001,100)) = 1
		_WindStrength("Wind Strength", Range(0, 2)) = 0.3
		_WindGustDistance("Distance between gusts",Range(0.001,50)) = .25
		_WindDirection("Wind Direction", vector) = (1,0, 1,0)
		_DetailTex("Detail (RGB)", 2D) = "white" {}

	}
	SubShader
	{
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}



		ZWrite On
		Cull Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf TwoSided vertex:vert alphatest:_Cutoff addshadow keepalpha 
		// #pragma require 2darray
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#include "UnityPBSLighting.cginc"

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
			float2 uv3_IndexTex;
			float3 viewDir;
			float3 worldNormal;
			float4 color : COLOR;
		};


		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		float _BumpScale;
		float4 _EmissionColor;
		float _OcclusionStrength;

		half _WindFrecuency;
		half _WindGustDistance;
		half _WindStrength;
		float3 _WindDirection;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)


		void vert(inout appdata_full v)
		{
			float4 localSpaceVertex = v.vertex;
			// Takes the mesh's verts and turns it into a point in world space
			// this is the equivalent of Transform.TransformPoint on the scripting side
			float4 worldSpaceVertex = mul(unity_ObjectToWorld, localSpaceVertex);

			// Height of the vertex in the range (0,1)
			float height = v.texcoord2.x / 256;

			worldSpaceVertex.x += sin(_Time.x * _WindFrecuency + worldSpaceVertex.x * _WindGustDistance) * height *
				_WindStrength * _WindDirection.x;
			worldSpaceVertex.z += sin(_Time.x * _WindFrecuency + worldSpaceVertex.z * _WindGustDistance) * height *
				_WindStrength * _WindDirection.z;

			// takes the new modified position of the vert in world space and then puts it back in local space
			v.vertex = mul(unity_WorldToObject, worldSpaceVertex);
		}

		// inline half4 LightingCustom(SurfaceOutputStandard s, half3 lightDir, UnityGI gi)
		// {
		// 	float3 n = s.Normal;
		// 	//s.Normal = lightDir;
		// 	half4 col1 = LightingStandard(s, lightDir, gi);
		// 	//s.Normal = -s.Normal;
		// 	//half4 col2 = LightingStandard(s, lightDir, gi);
		// 	//s.Normal = -s.Normal;
		// 	s.Normal = n;
		// 	return col1; // +col2;
		// }

		// inline void LightingCustom_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
		// {
		// 	LightingStandard_GI(s, data, gi);
		// }

		half4 LightingTwoSided(SurfaceOutputStandard s, half3 lightDir, half atten)
		{
			half NdotL = dot(s.Normal, lightDir);
			half INdotL = dot(-s.Normal, lightDir);
			// Figure out if we should use the inverse normal or the regular normal based on light direction.
			half diff = (NdotL < 0) ? INdotL : NdotL;
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * (diff * atten);
			c.a = s.Alpha;
			half3 modifiedNormal = INdotL;
			half NdotL2 = dot(modifiedNormal, lightDir);
			half diff2 = NdotL2;
			half4 c2;
			c2.rgb = s.Albedo * _LightColor0.rgb * (diff2 * atten);
			c2.a = s.Alpha;
			return (c + c2);
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed2 uv_texture1 = IN.uv_MainTex.xy;
			float height1 = tex2D(_BumpMap, uv_texture1).r;
			uv_texture1.xy += ParallaxOffset(height1, _HeightScale, IN.viewDir);
			fixed4 c1 = tex2D(_MainTex, uv_texture1);
			o.Albedo = c1.rgb * _Color.rgb;
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