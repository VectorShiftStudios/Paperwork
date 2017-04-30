﻿Shader "Unlit/DebugLinesNoDepth" 
{
	SubShader
	{
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