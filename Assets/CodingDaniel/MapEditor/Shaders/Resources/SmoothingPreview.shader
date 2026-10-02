// Smoothing-group preview (original implementation): translucent overlay on
// selected faces, pulled slightly toward the camera and optionally dithered.
Shader "CodingDaniel/MEBuilder/SmoothingPreview"
{
	Properties
	{
		_Opacity("Opacity", Float) = .5
	}

	SubShader
	{
		Tags { "IgnoreProjector" = "True" "RenderType" = "Transparent" "Queue" = "Transparent+2" }
		Lighting Off
		ZTest LEqual
		ZWrite Off
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float _Opacity;
			float _Dither;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 pos   : SV_POSITION;
				float4 color : COLOR;
			};

			v2f vert(appdata v)
			{
				v2f o;

				// Slight pull toward the camera keeps the overlay readable.
				o.pos = float4(UnityObjectToViewPos(v.vertex.xyz), 1);
				o.pos.xyz *= .98;
				o.pos = mul(UNITY_MATRIX_P, o.pos);
				o.color = v.color;

				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				// Optional checkerboard dither.
				i.pos.xy = floor(i.pos.xy) * .5;
				float checker = -frac(i.pos.x + i.pos.y);
				clip(lerp(1, checker, _Dither));

				return half4(i.color.rgb, i.color.a * _Opacity);
			}
			ENDCG
		}
	}
}
