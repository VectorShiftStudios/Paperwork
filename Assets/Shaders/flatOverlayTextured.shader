Shader "Paperwork/Flat Overlay Textured"
{
	Properties 
	{		
		_MainTex("Main Texture", 2D) = "white" {}
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Fade("Fade", Range(0,1)) = 0.0
	}

	SubShader 
	{
		Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		
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
			sampler2D _MainTex;
			float _Fade;
		
			struct appdata
			{
				float4 vertex	: POSITION;
				float4 color	: COLOR;
				float2 texcoord : TEXCOORD0;
			};
		
			struct v2f
			{
				float4 pos		: SV_POSITION;				
				float4 color	: COLOR;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;

				return o;
			}
			
			half4 frag (v2f i) : COLOR
			{
				//float3 fColor = lerp(gFloorColour.rgb, i.color.rgb, i.color.a);
				//float3 tint = lerp(float3(1, 1, 1), _MainColor, sin(_Time.y * 10.0) * 0.5 + 0.5);
				//fColor *= tint;

				

				if (i.color.r == 0.0)
				{
					fixed4 col = tex2D(_MainTex, i.texcoord);
					return col;
				}

				clip(_Fade - i.color.a);

				return _Color;
			}
			
			ENDCG
		}
	}
}
