Shader "Custom/PunchOut"
{
	Properties 
	{		
	}
	
	SubShader 
	{
		Tags 
		{
			"RenderType"="Opaque" "Queue"="Geometry+1"
		}

		Cull Off
		ZWrite Off
		ColorMask 0

		Pass
		{
			Stencil
			{
				Ref 1
				Comp NotEqual
				Pass Replace
			}
		}
	}
}
