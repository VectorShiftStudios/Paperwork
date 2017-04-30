// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Trail"
{
	Properties 
	{		
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}

	SubShader 
	{
		//Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Tags{ "Queue" = "Transparent" "DisableBatching" = "True" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite off
		
		Pass
		{		
			Cull Off
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			//float4 _SinTime;
			float4 gFloorColour;
			float4 _Color;
		
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
				//float3 fColor = lerp(gFloorColour.rgb, i.color.rgb, i.color.a);
				//float3 tint = lerp(float3(1, 1, 1), _MainColor, sin(_Time.y * 10.0) * 0.5 + 0.5);
				//fColor *= tint;

				return _Color * i.color;
			}
			
			ENDCG
		}
	}
}
