Shader "Custom/BlinnPhongFOW"
{
	Properties 
	{
		_SpecColor ("Spec Color", Color) = (1.0, 1.0, 1.0)
		_MainColor ("Main Color", Color) = (1.0, 1.0, 1.0)		
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_NormTex ("Norms", 2D) = "bump" {}
		_AOTex ("Ambient Occlusion", 2D) = "white" {}
		
		_Power ("Spec Power", Range(0,1)) = 0.5
		_Intensity ("Spec Intensity", Range(0,1)) = 0.2
	}
	
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf BlinnPhong addshadow
		

		sampler2D _MainTex;
		sampler2D _NormTex;
		sampler2D _AOTex;

		float3 _MainColor;
		float _Power;
		float _Intensity;
		
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
			half4 ao = tex2D (_AOTex, IN.uv_MainTex);
			float3 n = UnpackNormal(tex2D(_NormTex, IN.uv_MainTex));

			float2 uv;
			uv.x = IN.worldPos.x;
			uv.y = IN.worldPos.z;			
			uv = uv / 128;
			
			float fow = tex2D(g_FOWMap, uv).b;
			fow = saturate(fow * 1.5) * 0.98 + 0.02;
			//fow = saturate(fow + 0.2);
			//fow = gViewData.r * fow + 1.0 - gViewData.r;

			float rim = 1.0 - saturate(dot (normalize(IN.viewDir), n));			
			
			float3 color = c.rgb * _MainColor;			

			o.Albedo = float3(0,0,0);//color * fow * ao.rrr;
			o.Normal = n;
			o.Specular = 0;// _Power;
			o.Gloss = 0;// _Intensity * fow * ao.rrr;
			o.Alpha = c.a;
			o.Emission = rim * color * 1.0 * fow * ao.rrr;
			o.Emission = color * ao.rrr * fow;
		}
		ENDCG
	}
}
