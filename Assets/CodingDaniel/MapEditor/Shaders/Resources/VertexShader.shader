// Screen-space vertex handle shader (original implementation, no
// geometry-shader requirement). Each mesh vertex is projected, then offset in
// pixel space using the per-vertex texcoord1, so point/quad handles keep a
// constant on-screen size.
Shader "CodingDaniel/MEBuilder/VertexShader"
{
	Properties
	{
		_Scale("Scale", Range(1,7)) = 3.3
		_Color("Color", Color) = (1,1,1,1)
		_HandleZTest("_HandleZTest", Int) = 8
	}

	SubShader
	{
		Tags
		{
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"DisableBatching" = "True"
		}

		Lighting Off
		ZTest[_HandleZTest]
		ZWrite On
		Cull Off
		Blend Off
		Offset -1,-1

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			// Global multiplier updated from code (PBBuiltinMaterials.ScaleMultiplicator).
			float _PBScaleMultiplicator = 1;
			float _Scale;
			float4 _Color;

			struct appdata
			{
				float4 vertex    : POSITION;
				float3 normal    : NORMAL;
				float4 color     : COLOR;
				float2 texcoord  : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos   : SV_POSITION;
				float2 uv    : TEXCOORD0;
				float4 color : COLOR;
			};

			v2f vert(appdata v)
			{
				float isOrtho = (1 - UNITY_MATRIX_P[3][3]);

				v2f o;
				// Keep handle pixels roughly constant regardless of distance.
				float3 viewPos = UnityObjectToViewPos(v.vertex.xyz);
				viewPos.xyz *= lerp(.99, .95, isOrtho);

				// Project and remap to pixel space so we can add a pixel offset.
				float4 clip = mul(UNITY_MATRIX_P, float4(viewPos, 1));
				clip.xy /= clip.w;
				clip.xy = clip.xy * .5 + .5;
				clip.xy *= _ScreenParams.xy;

				clip.xy += v.texcoord1.xy * _Scale * _PBScaleMultiplicator;
				clip.z -= (.0001 + v.normal.x) * isOrtho;

				// Map back into clip space.
				clip.xy /= _ScreenParams.xy;
				clip.xy = (clip.xy - .5) / .5;
				clip.xy *= clip.w;

				o.pos = clip;
				o.uv = v.texcoord.xy;
				o.color = v.color;
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				return _Color * i.color;
			}
			ENDCG
		}
	}
}
