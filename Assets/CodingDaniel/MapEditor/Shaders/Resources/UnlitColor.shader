// Instanced unlit solid-color shader (original implementation) used as the
// generic fallback and simple fill material for editor overlays.
Shader "Hidden/MECommon/UnlitColor"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_ZWrite("ZWrite", Float) = 0.0
		_HandleZTest("_HandleZTest", Int) = 8
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100
		Cull Off
		ZTest[_HandleZTest]
		ZWrite[_ZWrite]
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
			UNITY_INSTANCING_BUFFER_END(Props)

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				return i.color * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
			}
			ENDCG
		}
	}
}
