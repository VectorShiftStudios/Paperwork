// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Veggie"
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
		#pragma surface surf BlinnPhong addshadow vertex:vert

		#define SIDE_TO_SIDE_FREQ1 1.975
		#define SIDE_TO_SIDE_FREQ2 0.793
		#define UP_AND_DOWN_FREQ1 0.375
		#define UP_AND_DOWN_FREQ2 0.193

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

		float4 SmoothCurve( float4 x ) {  
			return x * x *( 3.0 - 2.0 * x );  
		}  

		float4 TriangleWave( float4 x ) {  
			return abs( frac( x + 0.5 ) * 2.0 - 1.0 );  
		}
		
		float4 SmoothTriangleWave( float4 x ) {  
			return SmoothCurve( TriangleWave( x ) );  
		}  


		void vert(inout appdata_full v)
		{
			//50.0f;
			float windSpeed = 50.0;
			float windIntensity = 0.03;
			float scale = 0.2f;
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex);

			
			//v.vertex.x += sin(_Time * windSpeed + (worldPos.x * worldPos.z) * 0.01f) * (v.vertex.y * 0.04) * windIntensity;
			v.vertex.x += sin(_Time * windSpeed + (worldPos.x * worldPos.z) * scale) * windIntensity;
			v.vertex.y += sin(_Time * windSpeed + (worldPos.x * worldPos.z) * scale) * windIntensity;
			v.vertex.z += cos(_Time * windSpeed + (worldPos.x * worldPos.z) * scale) * windIntensity;

			/*
			// Crytek Vegetation Wind
			float fSpeed = 0.01;
			float fVtxPhase = dot(worldPos.xyz, 1);

			float2 vWavesIn = _Time + float2(fVtxPhase, 1);
			float4 vWaves = (frac( vWavesIn.xxyy *
							   float4(SIDE_TO_SIDE_FREQ1, SIDE_TO_SIDE_FREQ2, UP_AND_DOWN_FREQ1, UP_AND_DOWN_FREQ2) ) *
							   2.0 - 1.0 ) * fSpeed * 1;

			vWaves = SmoothCurve( vWaves );
			float2 vWavesSum = vWaves.xz + vWaves.yw;

			v.vertex.xyz += vWavesSum.x * 1.0 * 0.01;
			v.vertex.y += vWavesSum.y * 1.0;
			*/

			// Fade based on distance to player
			//float3 direction = (worldPos - _playerWorldPos);
			//float dist = direction.x * direction.x + direction.y * direction.y + direction.z * direction.z;
						
			//dist = 0.5 - dist * 0.003;
			//dist = 0.5 - dist * 0.001;
			//dist = 0.6 - dist * 0.001;
			//dist = 1;

			/*
			v.color.r = 1 - saturate(dist);
			v.color.r *= v.color.r;
			v.color.r = 1 - v.color.r;
			*/
		}

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
			fow = gViewData.r * fow + 1.0 - gViewData.r;

			float rim = 1.0 - saturate(dot (normalize(IN.viewDir), n));
			//rim = saturate(rim - 0.5) * 2;
			//rim = pow(rim, 2);
			
			float3 color = c.rgb * _MainColor;			

			o.Albedo = color * fow * ao.rrr;
			o.Normal = n;
			o.Specular = 0;//_Power;
			o.Gloss = 0;//_Intensity * fow * ao.rrr;
			o.Alpha = c.a;
			o.Emission = rim * color * 1.0 * fow * ao.rrr;
		}
		ENDCG
	}
}
