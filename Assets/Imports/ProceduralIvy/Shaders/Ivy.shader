Shader "TNTC/Ivy"{
    Properties{
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ColorEnd ("Color End", Color) = (1, 1, 1, 1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Amount("Amount", Range(-1.0, 0.5)) = 0
        _Radius("Radius", float) = 0
        [MaterialToggle] _Horizontal("Horizontal", Float) = 0.1
    }

    SubShader{
        Tags { "RenderType"="Opaque"}
        LOD 100
        
        CGPROGRAM
        #pragma surface surf WrapLambert vertex:vert addshadow
        // #pragma surface surf NoLighting 
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        float4 _Color;
        float4 _ColorEnd;
        float _Amount;
        float _Radius;
        float _Horizontal;

        struct Input {
            float2 uv_MainTex;
            float2 uv_NormalMap;
        };

        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten){
            fixed4 c;
            c.rgb = s.Albedo; 
            c.a = s.Alpha;
            return c;
        }

        half4 LightingWrapLambert (SurfaceOutput s, half3 lightDir, half atten) {
            half NdotL = dot (s.Normal, lightDir);
            half diff = NdotL * 0.5 + 0.5;
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * clamp(diff * atten, 0.4, 1);
            c.a = s.Alpha;
            return c;
        }

        void vert (inout appdata_full v) {
            v.vertex.xyz -= v.normal * smoothstep(0, .5, v.texcoord.y - _Amount ) * (_Radius);
        }

        void surf (Input IN, inout SurfaceOutput o) {
            half4 c = tex2D (_MainTex, IN.uv_MainTex + float2(0, -_Amount));
            o.Normal = UnpackNormal (tex2D (_NormalMap, IN.uv_NormalMap));
            o.Albedo = c.rgb * lerp(_Color, _ColorEnd, IN.uv_MainTex.y);
            if(_Horizontal == 1){
                o.Alpha = 1 - saturate(IN.uv_MainTex.x - (_Amount));
            }else{
                o.Alpha = 1- smoothstep(0, .5, IN.uv_MainTex.y - _Amount );
            }
            clip (o.Alpha - 0.05);
        }
        ENDCG
        
    }
}