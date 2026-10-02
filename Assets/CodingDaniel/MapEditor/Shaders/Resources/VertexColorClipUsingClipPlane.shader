// Vertex-colored handle geometry clipped against a camera-facing plane at the
// object origin (original implementation): parts of a handle that sit behind
// the object it belongs to are culled so handles stay readable.
Shader "CodingDaniel/MEHandles/VertexColorClipUsingClipPlane" {
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_ZWrite("ZWrite", Float) = 0.0
		_ZTest("ZTest", Float) = 0.0
		_Cull("Cull", Float) = 0.0
	}
	SubShader
	{
		Tags{ "Queue" = "Geometry+5" "IgnoreProjector" = "True" "RenderType" = "Opaque" }
		Pass
		{
			Cull[_Cull]
			ZTest Off
			ZWrite Off

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

			struct vertexInput
			{
				float4 vertex : POSITION;
				float4 color  : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput
			{
				float4 pos           : SV_POSITION;
				float3 planeOrigin   : TEXCOORD0;
				float3 planeNormal   : TEXCOORD1;
				float3 worldPosition : TEXCOORD2;
				float4 color         : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// Per-draw tint, supplied through the material property block. This shader previously used
			// the mesh's vertex colour alone, but the handle ring mesh (CreateWireCircle) has no colour
			// channel - so all three rotation rings drew the same colour instead of X/Y/Z.
			float4 _Color;

			// Fast sRGB -> linear approximation.
			inline half3 SRGBToLinearApprox(half3 c)
			{
				return c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
			}

			// Signed distance from an arbitrary world point to a plane.
			float SignedPlaneDistance(float3 origin, float3 normal, float3 worldPoint)
			{
				return dot(normal, worldPoint - origin);
			}

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.pos = UnityObjectToClipPos(input.vertex);

				// Clip plane sits at the object origin and faces the camera.
				output.planeOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
				output.planeNormal = normalize(_WorldSpaceCameraPos - output.planeOrigin);
				output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;

				output.color.rgb = SRGBToLinearApprox(input.color.rgb);
				output.color.a = input.color.a;
				return output;
			}

			float4 frag(vertexOutput input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);
				float d = SignedPlaneDistance(input.planeOrigin, input.planeNormal, input.worldPosition);
				clip(d);
				return input.color * _Color;
			}
			ENDCG
		}
	}
}
