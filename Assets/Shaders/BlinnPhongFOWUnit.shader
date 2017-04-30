Shader "Custom/BlinnPhongFOWUnit"
{
	Properties 
	{
		_SpecColor ("Spec Color", Color) = (1.0, 1.0, 1.0)
		_MainColor ("Main Color", Color) = (1.0, 1.0, 1.0)		
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_NormTex ("Norms", 2D) = "bump" {}
		_AOTex ("Ambient Occlusion", 2D) = "white" {}
		_TeamMaskTex ("Team Mask (RGB)", 2D) = "black" {}
		
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
		sampler2D _TeamMaskTex;

		float3 _MainColor;
		float _Power;
		float _Intensity;

		float3 _TeamColor;
		
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
			half4 tm = tex2D (_TeamMaskTex, IN.uv_MainTex);
			half4 ao = tex2D (_AOTex, IN.uv_MainTex);
			float3 n = UnpackNormal(tex2D(_NormTex, IN.uv_MainTex));

			float2 uv;
			uv.x = IN.worldPos.x;
			uv.y = IN.worldPos.z;			
			uv = uv / 128;
			
			float fow = tex2D(g_FOWMap, uv).b;
			//fow = saturate(fow * 1.5) * 0.98 + 0.02;
			//fow = saturate(fow + 0.2);
			//fow = gViewData.r * fow + 1.0 - gViewData.r;

			float rim = 1.0 - saturate(dot (normalize(IN.viewDir), n));
			//rim = pow(rim, 2);
			
			float3 colLerp = lerp(_MainColor, _TeamColor * 0.1, tm.rrr);
			float3 color = c.rgb;// * colLerp;			

			o.Albedo = float3(0,0,0);//color * fow * ao.rrr;
			o.Normal = n;
			o.Specular = _Power;
			o.Gloss = _Intensity * fow * ao.rrr;
			o.Alpha = c.a;
			o.Emission = color;//rim * color * 2.0 * fow * ao.rrr;
		}
		ENDCG
	}
}
