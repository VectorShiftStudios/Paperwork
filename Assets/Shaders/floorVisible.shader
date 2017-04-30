Shader "Paperwork/floorVisible"
{
	SubShader 
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry+2" }
		
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf BlinnPhong

		struct Input
		{
			float4 color : COLOR;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			o.Albedo = float3(0,0,0);
			o.Emission = IN.color.rgb;
		}
		ENDCG
	}
}
