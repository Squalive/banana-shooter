// Vertex picker (original implementation) for runtime ProBuilder editing.
// Each vertex is offset in pixel space using texcoord1 so vertices stay
// tappable at a constant screen size; pass "Vertices" is referenced by
// SelectionPicker.
Shader "CodingDaniel/MEBuilder/VertexPicker"
{
    Properties {}

    SubShader
    {
        Tags
        {
            "ProBuilderPicker"="VertexPass"
            "RenderType"="Transparent"
            "RenderQueue"="Transparent"
            "IgnoreProjector"="True"
            "DisableBatching"="True"
        }

        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Off
        Blend Off
        Offset -1, -1

        Pass
        {
            Name "Vertices"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Orthographic camera? 1 = yes, 0 = no.
            #define ORTHO (1 - UNITY_MATRIX_P[3][3])

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
                v2f o;

                // Pull the vertex slightly toward the camera so markers draw
                // on top of geometry, then project.
                o.pos = float4(UnityObjectToViewPos(v.vertex.xyz), 1);
                o.pos.xyz *= lerp(.99, .95, ORTHO);
                o.pos = mul(UNITY_MATRIX_P, o.pos);

                // Convert to pixel space, apply the per-vertex pixel offset,
                // then transform back to clip space.
                float4 clip = o.pos;
                clip.xy /= clip.w;
                clip.xy = clip.xy * .5 + .5;
                clip.xy *= _ScreenParams.xy;

                clip.xy += v.texcoord1.xy * 3.5;
                clip.z -= .0001 * ORTHO;

                clip.xy /= _ScreenParams.xy;
                clip.xy = (clip.xy - .5) / .5;
                clip.xy *= clip.w;

                o.pos = clip;
                o.uv = v.texcoord.xy;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : COLOR
            {
                return i.color;
            }
            ENDCG
        }
    }
}
