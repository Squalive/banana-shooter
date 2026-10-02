// Unlit solid-color shader (original implementation) that projects the mesh
// onto the camera-facing plane (screen-space circle ring) while honoring an
// adjustable depth test so rotation-handle rings occlude correctly.
Shader "CodingDaniel/MEHandles/Models/SSCircleShader"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_ZTest("ZTest", Float) = 0.0
		[Enum(Off,0,On,1)]_ZWrite("ZWrite", Float) = 1.0
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent-1" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest[_ZTest]
			ZWrite[_ZWrite]

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

				// Keep the object at its view-space origin and spread the local
				// xy out by world scale, which turns the mesh into a
				// camera-facing circle of constant size.
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
