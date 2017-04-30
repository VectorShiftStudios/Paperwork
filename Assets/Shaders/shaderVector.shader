// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VectorLines"
{
	Properties 
	{	
		_LineColor ("Line Color", Color) = (1.0, 1.0, 1.0)
		_MainColor ("Main Color", Color) = (1.0, 1.0, 1.0)
		_FloorColor ("Floor Color", Color) = (0.0, 0.0, 0.0)
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }

		//Tags{ "Queue" = "Transparent" "RenderType" = "Opaque" }
		//Blend SrcAlpha OneMinusSrcAlpha
		//ZWrite Off

		Pass
		{		
			Cull Off
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			//float4 _SinTime;
			float4 _LineColor;
			float4 _FloorColor;
			float3 _MainColor;
		
			struct appdata
			{
				float4 vertex	: POSITION;
				float4 color	: COLOR;
			};
		
			struct v2f
			{
				float4 pos		: SV_POSITION;				
				float4 color	: COLOR;
			};

			v2f vert(appdata v)
			{
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.color = v.color;

				return o;
			}
			
			half4 frag (v2f i) : COLOR
			{
				float3 fColor = lerp(_FloorColor.rgb, i.color.rgb, i.color.a);

				float3 tint = lerp(_LineColor, _MainColor, sin(_Time.y * 5.0) * 0.5 + 0.5);

				fColor = lerp(fColor, tint, i.color.a);

				//fColor = fColor tint;

				return float4(fColor, 1.0);
			}
			
			ENDCG
		}
	}
}
