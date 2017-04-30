Shader "Unlit/DebugLines" 
{
	SubShader
	{
		// With Depth
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			//ZWrite Off
			//ZTest Always
			Cull Off

			BindChannels
			{
				Bind "vertex", vertex
				Bind "color", color
			}
		}

		// No Depth
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest Always
			Cull Off

			BindChannels
			{
				Bind "vertex", vertex
				Bind "color", color
			}
		}
	}
}