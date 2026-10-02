// Unlit solid-color shader (original implementation), drawn on top of the
// scene (ZTest Always) while still writing depth.
Shader "CodingDaniel/MEHandles/Models/DepthMaskColor"
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
				o.vertex = UnityObjectToClipPos(v.vertex);
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
