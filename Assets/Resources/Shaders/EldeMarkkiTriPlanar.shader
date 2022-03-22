Shader "Eldemarkki/Triplanar Shader"
{
	Properties
	{
		XColor("X Color", Color) = (0.42, 0.42, 0.42, 1)
		YColor("Y Color", Color) = (0.44, 0.74, 0.01, 1)
		NegativeYColor("Negative Y Color", Color) = (0.32, 0.32, 0.32, 1)
		Sensitivity("Sensitivity", Range(0,1)) = 0.6
		SensitivityBlend("Sensitivity Blend", Range(0, 0.2)) = 0.1
		BlendStepCount("Blend Step Count", Int) = 2
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque"}

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		fixed4 XColor;
		fixed4 YColor;
		fixed4 NegativeYColor;
		fixed Sensitivity;
		fixed SensitivityBlend;
		int BlendStepCount;

		struct Input
		{
			float3 worldNormal;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed x = abs(IN.worldNormal.x);
			fixed yNormal = IN.worldNormal.y;

			fixed3 col = fixed3(0, 0, 0);

			fixed dotP = dot(yNormal, fixed3(0, 1, 0));
			fixed t = (dotP - (Sensitivity - SensitivityBlend)) / ((Sensitivity + SensitivityBlend) - (Sensitivity - SensitivityBlend));
			t = floor(t * (BlendStepCount + 1)) / (BlendStepCount + 1);

			bool yNormalLessThanZero = yNormal < 0;
			fixed3 a = (yNormalLessThanZero) * (NegativeYColor);
			fixed3 b = (!yNormalLessThanZero) * (XColor + clamp(t, 0, 1) * (YColor - XColor));
			col = a + b;

			o.Albedo = col;
		}
		ENDCG
	}
		FallBack "Diffuse"
}