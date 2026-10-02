// Selection-outline composite (original implementation): merges the tinted
// glow over the scene color.
Shader "CodingDaniel/MEOutlines/Composite"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}

		// The object mask must be declared here with a black default. Outline.hlsl samples it, and a
		// shader texture that is not declared has no default of its own and resolves to white whenever
		// the binding is missing - which would draw the ring over the entire screen.
		_PrepassTex("Prepass", 2D) = "black" {}
	}
	SubShader
	{
		Pass
		{
			HLSLPROGRAM

			#include "Outline.hlsl"

			#pragma multi_compile_instancing
			#pragma vertex FullscreenVert
			#pragma fragment CompositeFrag

			ENDHLSL
		}
	}
}
