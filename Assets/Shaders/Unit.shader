// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Unit"
{
	Properties 
	{		
		_MainColor ("Main Color", Color) = (1.0, 1.0, 1.0)
		_OutlineColor ("Main Color", Color) = (0.0, 0.0, 0.0)
		_PlayerColor("Player Color", Color) = (1.0, 1.0, 1.0)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Stam("Stamina", Range(0,1)) = 1.0
	}
	
	SubShader 
	{
		Tags 
		{
			"RenderType"="Opaque" 
		}

		Cull Off

		LOD 200

		CGINCLUDE
		#pragma target 3.0

		sampler2D _MainTex;
		float3 _MainColor;
		float3 _OutlineColor;
		float4 _PlayerColor;
		float _Stam;

		struct appdata 
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
		};
			
		struct v2f
		{
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
			float4 uv1 : TEXCOORD1;
		};

		v2f vertFat(appdata v)
		{
			v2f o;
			float4 vFat = v.vertex;
			vFat.xyz += normalize(v.normal) * 0.05;
			o.pos = UnityObjectToClipPos(vFat);
			o.uv = v.texcoord;
			o.uv1 = v.texcoord1;
			return o;
		}

		v2f vert(appdata v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
			o.uv1 = v.texcoord1;
			return o;
		}

		half4 fragFat(v2f i) : SV_Target
		{
			return half4(_OutlineColor.rgb, 1);
		}

		half4 frag(v2f i) : SV_Target
		{	
			float4 tex = tex2D(_MainTex, i.uv.xy);

			float3 c = float3(1, 1, 1) * tex.g;
			//c += float3(0.8, 0.0, 0.0) * tex.r;
			c += _PlayerColor.rgb * tex.r;

			float3 stamCol = c;

			if (i.uv1.y > _Stam)
				stamCol = _MainColor;
			
			return half4(stamCol, 1.0);
			//return half4(c.r, c.g, c.b, 1.0);

			//float3 tint = lerp(float3(1, 1, 1), _MainColor, sin(_Time.y * 10.0) * 0.5 + 0.5);				
			//return half4(tint, 1);
		}
		ENDCG

		/*
		Pass
		{
			//Cull Front
			ZWrite Off
				
			CGPROGRAM
			#pragma vertex vertFat
			#pragma fragment fragFat
			ENDCG
		}
		*/
		
		Pass
		{
			/*
			// For Pass 1
			Stencil
			{
				Ref 1
				Comp Always
				Pass Keep
			}
			*/

			/*
			Stencil
			{
				Ref 1
				Comp Always
				Pass Replace
				//Fail Keep
			}
			*/			

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
