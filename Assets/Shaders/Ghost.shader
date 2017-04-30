Shader "Custom/Ghost"
{
	Properties 
	{
		_SpecColor ("Spec Color", Color) = (1.0, 1.0, 1.0)
		_MainColor ("Main Color", Color) = (1.0, 1.0, 1.0)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_NormTex ("Norms", 2D) = "bump" {}
		
		_Alpha ("Alpha Base", Range(0,1)) = 0.0
	}
	
	SubShader 
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Blend SrcAlpha One
		LOD 200
		
		CGPROGRAM

		#pragma target 3.0
		#pragma surface surf BlinnPhong


		sampler2D _MainTex;
		sampler2D _NormTex;

		float4 _MainColor;
		float _Alpha;
		
		float pulse;

		float4 gViewData;

		sampler2D g_FOWMap;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float3 viewDir;
			float3 worldNormal; INTERNAL_DATA
		};

		void surf (Input IN, inout SurfaceOutput o) {
			
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			float3 n = UnpackNormal(tex2D(_NormTex, IN.uv_MainTex));

			float2 uv;
			uv.x = IN.worldPos.x;
			uv.y = IN.worldPos.z;			
			uv = uv / 128;
						
			float fow = tex2D(g_FOWMap, uv).b;
			fow = saturate(fow * 1.5) * 0.98 + 0.02;
			fow = gViewData.r * fow + 1.0 - gViewData.r;

			float rim = 1.0 - saturate(dot (normalize(IN.viewDir), n));			
			rim = pow(rim, 2);
			rim = saturate(rim + _Alpha);

			float fade = (sin(_Time.a) * 0.5 + 0.5) * 0.7 + 0.3;
			
			o.Albedo = 0; //* fow;			
			o.Normal = n;
			o.Specular = 0;
			o.Gloss = 0;
			o.Alpha = 1.0;
			o.Emission = _MainColor.rgb;
		}
		ENDCG
	}
}
