// Camera-facing textured quad shader (original implementation) used for
// gizmo icons. The mesh is flattened onto a plane parallel to the camera at
// the object origin; CONSTANT_SCALE keeps it at world scale 1 when enabled.
Shader "CodingDaniel/MECommon/Billboard"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Texture Image", 2D) = "white" {}
		_Cutoff("Cutoff", Float) = 0.01
		[Toggle(CONSTANT_SCALE)]
		_ConstantScale("Constant Scale", Float) = 0
	}
	SubShader
	{
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZTest LEqual
		ZWrite On

		Tags{ "Queue" = "Transparent+20" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma shader_feature CONSTANT_SCALE

			sampler2D _MainTex;
			fixed4 _Color;
			float _Cutoff;

			struct vertexInput
			{
				float4 vertex : POSITION;
				float4 tex    : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput
			{
				float4 pos  : SV_POSITION;
				float4 tex  : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#ifdef CONSTANT_SCALE
				float scaleX = 1;
				float scaleY = 1;
#else
				// Spread the local quad by the object's world scale so it
				// occupies the object's extents.
				float scaleX = length(mul(unity_ObjectToWorld, float4(1, 0, 0, 0)));
				float scaleY = length(mul(unity_ObjectToWorld, float4(0, 1, 0, 0)));
#endif

				// Flatten onto a plane at the object origin, facing the camera.
				float4 viewPos = float4(UnityObjectToViewPos(float3(0, 0, 0)), 1);
				output.pos = mul(UNITY_MATRIX_P, viewPos
					- float4(input.vertex.x * scaleX, input.vertex.y * scaleY, 0, 0));
				output.tex = input.tex;
				return output;
			}

			float4 frag(vertexOutput input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);
				float4 color = _Color * tex2D(_MainTex, input.tex.xy);
				clip(color.a - _Cutoff);
				return color;
			}
			ENDCG
		}
	}
}
