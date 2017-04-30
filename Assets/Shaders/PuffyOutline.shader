Shader "Custom/PuffyOutline"
{
	Properties 
	{
		_MainColor ("Main Color", Color) = (1.0, 1.0, 1.0)
	}
	
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull Front
		
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf BlinnPhong addshadow vertex:vert

		float3 _MainColor;
	
		struct Input {
			float3 worldPos;
			float3 viewDir;
			float3 worldNormal; INTERNAL_DATA
		};

		void vert(inout appdata_full v)
		{
			v.vertex.xyz += v.normal * 0.02f;
		}

		void surf (Input IN, inout SurfaceOutput o)
		{
			//o.Albedo = _MainColor;
			o.Alpha = 1.0;
			o.Emission = _MainColor;
		}
		ENDCG
	}
}
