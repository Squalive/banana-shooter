// Unlit handle-arrow shader (original implementation).
// Vertex color is shaded by how directly the arrow faces the camera and
// converted to linear space so results match the previous look.
Shader "CodingDaniel/MEHandles/Models/ArrowShader"
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

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 color  : COLOR;
			};

			struct v2f
			{
				float4 pos   : SV_POSITION;
				float4 color : COLOR;
			};

			fixed4 _Color;

			// Fast sRGB -> linear approximation (piecewise-less polynomial fit).
			inline half3 GammaToLinearApprox(half3 c)
			{
				return c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
			}

			v2f vert(appdata v)
			{
				v2f o;

				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				float3 viewNormal  = mul((float3x3)UNITY_MATRIX_V, worldNormal);
				// Faces looking straight at the camera are brightest.
				float facing = saturate(dot(viewNormal, float3(0, 0, 1)));

				o.pos = UnityObjectToClipPos(v.vertex);
				o.color.rgb = GammaToLinearApprox(v.color.rgb * (facing * 1.5));
				o.color.a = v.color.a;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return _Color * i.color;
			}
			ENDCG
		}
	}
}
