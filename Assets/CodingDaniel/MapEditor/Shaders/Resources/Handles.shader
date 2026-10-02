// Gizmo handle shader (original implementation). Quads are expanded by a
// per-vertex offset to a constant on-screen size; vertices that face away
// from the camera are dimmed using _MinAlpha.
Shader "CodingDaniel/MEGizmos/Handles"
{
	Properties
	{
		_Color("Color", Color) = (0,0,0,1)
		_Scale("Scale", Float) = 1.0
		_ZWrite("__zw", Float) = 0.0
		_ZTest("__zt", Float) = 0.0
		_VertexOffset("VertexOffset", Float) = 0.0
		_Offset("_Offset", Float) = 0.0
		_MinAlpha("_MinAlpha", Float) = 0.2
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent+2" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Back
			ZTest[_ZTest]
			ZWrite[_ZWrite]
			Offset[_Offset], [_Offset]

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

			struct vertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 offset : TEXCOORD0;
				float4 color  : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput
			{
				float4 pos   : SV_POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float _Scale;
			fixed4 _Color;
			float _VertexOffset;
			float _MinAlpha;

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				// View-space position (flipped so the dot product with the
				// view normal determines front/back facing in perspective).
				float3 viewPos = lerp(-UnityObjectToViewPos(input.vertex.xyz),
					float3(0, 0, 1), unity_OrthoParams.w);

				float3 worldNorm = UnityObjectToWorldNormal(input.normal);
				float3 viewNorm  = mul((float3x3)UNITY_MATRIX_V, worldNorm);
				float facing = step(0, dot(viewNorm, viewPos));

				// Constant screen-size expansion factor.
				float proj = unity_CameraProjection[1].y;
				float screenH = _ScreenParams.y;
				float orthoH = unity_OrthoParams.y;

				float4 v = float4(UnityObjectToViewPos(input.vertex.xyz), 1);
				v += float4(0, 0, _VertexOffset, 0);

				float dist = v.z;
				float focal = 1 / proj;
				float denom = lerp(dist * 6 * _Scale / screenH * focal,
					orthoH * 6 * _Scale / screenH, unity_OrthoParams.w);

				output.pos = mul(UNITY_MATRIX_P, v -
					float4(input.offset.x * denom, input.offset.y * denom, 0, 0));

				output.color = input.color;
				output.color.a = max(_MinAlpha, facing);
				return output;
			}

			float4 frag(vertexOutput input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);
				return input.color * _Color;
			}
			ENDCG
		}
	}
}
