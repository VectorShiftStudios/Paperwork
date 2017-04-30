Shader "Custom/IconVector"
{
	Properties 
	{	
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

			float4x4 worldMat;

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

				//o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.pos = mul(worldMat, v.vertex);
				o.color = v.color;

				return o;
			}
			
			half4 frag (v2f i) : COLOR
			{
				//return float4(1, 1, 1, 1);
				return float4(1, 1, 1, i.color.a);
			}
			
			ENDCG
		}
	}
}
