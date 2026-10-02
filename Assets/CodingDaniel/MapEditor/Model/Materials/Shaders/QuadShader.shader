// Unlit solid-color shader (original implementation) that ignores the depth
// buffer, used for the axis-aligned move/scale quads.
Shader "CodingDaniel/MEHandles/Models/QuadShader"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		[Enum(Off,0,On,1)]_ZWrite("ZWrite", Float) = 1.0
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite[_ZWrite]
			ZTest Always
			Cull Off

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
