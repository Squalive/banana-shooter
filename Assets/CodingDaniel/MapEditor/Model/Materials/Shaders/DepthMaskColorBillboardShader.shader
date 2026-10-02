// Unlit solid-color shader (original implementation) whose geometry is
// flattened onto the view plane, i.e. always faces the camera (billboard).
Shader "CodingDaniel/MEHandles/Models/DepthMaskColorBillboard"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent-1" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			fixed4 _Color;

			v2f vert(appdata v)
			{
				v2f o;

				// Take the object origin in view space and expand the mesh's
				// local xy by the object's world scale, keeping the mesh
				// parallel to the camera.
				float scaleX = length(mul(unity_ObjectToWorld, float4(1, 0, 0, 0)));
				float scaleY = length(mul(unity_ObjectToWorld, float4(0, 1, 0, 0)));

				float3 origin = UnityObjectToViewPos(float3(0, 0, 0));
				o.vertex = mul(UNITY_MATRIX_P,
					float4(origin.x - v.vertex.x * scaleX,
					       origin.y - v.vertex.y * scaleY,
					       origin.z, 1));

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return _Color;
			}
			ENDCG
		}
	}
}
