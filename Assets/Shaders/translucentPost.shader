// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TranslucentPost"
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
				
				return half4(c.rgb, c.a * 0.7);
			}
			
			ENDCG
		}
	}
}
