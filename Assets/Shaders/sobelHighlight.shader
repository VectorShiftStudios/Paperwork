// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/SobelHighlight"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "" {}
		_OutlineColor("Outline Color", Color) = (0.0, 0.0, 0.0)
	}

	SubShader 
	{	
		Pass
		{		
			Fog{ Mode off }

			ZTest Always 
			Cull Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float3 _OutlineColor;
		
			struct v2f
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata_img v)
			{
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv.xy = v.texcoord.xy;

				return o;
			}
			
			half4 frag (v2f i) : COLOR
			{
				float4 c = tex2D(_MainTex, i.uv);

				float thickness = 1.0;
				float offX = 1.0 / _ScreenParams.x * thickness;
				float offY = 1.0 / _ScreenParams.y * thickness;

				float2 o1 = float2(-offX, -offY);
				float2 o2 = float2(0, -offY);
				float2 o3 = float2(+offX, -offY);
				
				float2 o4 = float2(-offX, offY);
				float2 o5 = float2(+offX, offY);

				float2 o6 = float2(-offX, +offY);
				float2 o7 = float2(0, +offY);
				float2 o8 = float2(+offX, +offY);


				float s = c.a;

				float a = 0;
				a += abs(s - tex2D(_MainTex, i.uv + o1).a);
				a += abs(s - tex2D(_MainTex, i.uv + o2).a);
				a += abs(s - tex2D(_MainTex, i.uv + o3).a);
				a += abs(s - tex2D(_MainTex, i.uv + o4).a);
				a += abs(s - tex2D(_MainTex, i.uv + o5).a);
				a += abs(s - tex2D(_MainTex, i.uv + o6).a);
				a += abs(s - tex2D(_MainTex, i.uv + o7).a);
				a += abs(s - tex2D(_MainTex, i.uv + o8).a);

				a = saturate(a * 0.6);// -c.a;
				//a = saturate(a * 1.0);

				//return half4(a, a, a, 1);

				return half4(_OutlineColor, a);
			}
			
			ENDCG
		}
	}
}
