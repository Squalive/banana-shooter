// Unlit vertex-colored shape shader (original implementation) used by the
// transform-handle faces. Faces are shaded by how directly they point at the
// camera; color is converted to linear space on the way out.
Shader "CodingDaniel/MEHandles/Shape" {
	Properties
	{
		_Color("Color", Color) = (1,1,1,0.1)
		_ZWrite("ZWrite", Float) = 0.0
		_ZTest("ZTest", Float) = 0.0
		_Cull("Cull", Float) = 0.0
		_OFactors("OFactors", Float) = 0.0
		_OUnits("OUnits", Float) = 0.0
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Back
			ZTest[_ZTest]
			ZWrite[_ZWrite]
			Offset [_OFactors], [_OUnits]

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			struct vertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
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

			fixed4 _Color;

			// Fast sRGB -> linear approximation.
			inline half3 SRGBToLinearApprox(half3 c)
			{
				return c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
			}

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 worldNormal = UnityObjectToWorldNormal(input.normal);
				float3 viewNormal  = mul((float3x3)UNITY_MATRIX_V, worldNormal);
				float facing = saturate(dot(viewNormal, float3(0, 0, 1)));

				output.pos = UnityObjectToClipPos(input.vertex);
				output.color.rgb = SRGBToLinearApprox(input.color.rgb * (facing * 1.5));
				output.color.a = input.color.a;
				return output;
			}

			float4 frag(vertexOutput input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);
				return _Color * input.color;
			}
			ENDCG
		}
	}
}
