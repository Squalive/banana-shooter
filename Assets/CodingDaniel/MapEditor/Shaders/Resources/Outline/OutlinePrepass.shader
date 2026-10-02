// Selection-outline prepass (original implementation): renders the selected
// objects as a solid red mask that the blur/composite passes operate on.
Shader "CodingDaniel/MEOutlines/Prepass"
{
	Properties
	{
	}
	SubShader
	{
		Pass
		{
			HLSLPROGRAM

			#include "Outline.hlsl"

			#pragma multi_compile_instancing
			#pragma vertex PrepassVert
			#pragma fragment PrepassFrag

			ENDHLSL
		}
	}
}
