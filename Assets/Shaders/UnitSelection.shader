// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/UnitSelection"
{
	Properties 
	{		
		_MainColor ("Main Color", Color) = (1.0, 1.0, 1.0)		
		_OutlineColor ("Main Color", Color) = (0.0, 0.0, 0.0)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	SubShader 
	{
		Tags
		{
			"RenderType"="Opaque" 
		}

		LOD 200

		CGINCLUDE
			#pragma target 3.0

			sampler2D _MainTex;
			float3 _MainColor;
			float3 _OutlineColor;

			struct appdata 
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			
			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vertFat(appdata v)
			{
				v2f o;
				float4 vFat = v.vertex;
				vFat.xyz += v.normal * 0.035;
				o.pos = UnityObjectToClipPos(vFat);
				return o;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			half4 fragFat(v2f i) : SV_Target
			{
				return half4(_OutlineColor.rgb, 1);
			}

			half4 frag(v2f i) : SV_Target
			{				
				return half4(1,0,0, 1);
			}
		ENDCG

		Pass
		{
			Stencil
			{
				Ref 1
				Comp Always
				Pass Keep
			}

			ZWrite Off
				
			CGPROGRAM
			#pragma vertex vertFat
			#pragma fragment fragFat
			ENDCG
		}

		Pass
		{
			/*
			Stencil
			{
				Ref 1
				Comp Always
				Pass Replace
				//Fail Keep
			}*/			

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
