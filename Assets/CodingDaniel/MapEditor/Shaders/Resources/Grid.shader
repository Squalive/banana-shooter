// Map-editor floor grid (original implementation): vertex-colored grid quads
// that fade out with distance and skip pixels already tagged by the
// selection stencil.
Shader "CodingDaniel/MEHandles/Grid" {
	Properties
	{
		_GridColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_ZWrite("ZWrite", Float) = 0.0
		_ZTest("ZTest", Float) = 4.0
		_Cull("Cull", Float) = 0.0
		_FadeDistance("FadeDistance", Float) = 50.0
		_CameraSize("CameraSize", Float) = 1.0

		_StencilComp("Stencil Comparison", Float) = 5
		_Stencil("Stencil ID", Float) = 99
		_StencilOp("Stencil Operation", Float) = 0
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent+5" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Stencil
		{
			Ref[_Stencil]
			Comp NotEqual
			Pass[_StencilOp]
		}
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Cull[_Cull]
			ZTest[_ZTest]
			ZWrite Off
			Offset -0.02, 0

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			struct vertexInput
			{
				float4 vertex : POSITION;
				float4 color  : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput
			{
				float4 pos      : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float4 color    : COLOR;
			};

			float _FadeDistance;
			float _CameraSize;
			float4 _GridColor;

			// Fast sRGB -> linear approximation.
			inline half3 SRGBToLinearApprox(half3 c)
			{
				return c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
			}

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;
				UNITY_SETUP_INSTANCE_ID(input);

				output.pos = UnityObjectToClipPos(input.vertex);
				output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
				output.color.rgb = SRGBToLinearApprox(input.color.rgb);
				output.color.a = input.color.a;
				return output;
			}

			#define PERSP UNITY_MATRIX_P[3][3]
			#define ORTHO (1 - PERSP)

			float4 frag(vertexOutput input) : COLOR
			{
				float4 col = input.color;

				// Blend between orthographic (camera distance) and perspective
				// (camera size) fade behavior.
				float fadeFactor = (length(_WorldSpaceCameraPos - input.worldPos) * ORTHO
					+ _CameraSize * PERSP) / _FadeDistance;
				col.a = col.a * saturate(1 - fadeFactor);
				col.a = col.a * col.a;

				return col * _GridColor;
			}
			ENDCG
		}
	}
}
