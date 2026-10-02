// Constant screen-size point/quad handle shader (original implementation).
// Each input point is projected to pixel space and expanded into a fixed-size
// screen quad in the geometry stage, so vertex handles stay a constant size.
Shader "CodingDaniel/MEHandles/PointBillboard"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Scale("Scale", Range(0, 20)) = 7
        _HandleZTest("_HandleZTest", Int) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+5"
            "IgnoreProjector" = "True"
            "ForceNoShadowCasting" = "True"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "VertexPass"
            Lighting Off
            ZTest[_HandleZTest]
            ZWrite On
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1, -1

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geo
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct GS_INPUT
            {
                float4 pos   : POSITION;
                float4 color : COLOR;
            };

            struct FS_INPUT
            {
                float4 pos   : POSITION;
                float4 color : COLOR;
            };

            float _Scale;
            float4 _Color;

            // Orthographic camera? 1 = yes, 0 = no.
            #define ORTHO (1 - UNITY_MATRIX_P[3][3])

            const float ORTHO_CAM_OFFSET = .0001;

            // Clip -> NDC -> screen pixels.
            float4 ClipToScreen(float4 v)
            {
                v.xy /= v.w;
                v.xy = v.xy * .5 + .5;
                v.xy *= _ScreenParams.xy;
                return v;
            }

            // Screen pixels -> NDC -> clip (with a tiny orthographic bias).
            float4 ScreenToClip(float4 v)
            {
                v.z -= ORTHO_CAM_OFFSET * ORTHO;
                v.xy /= _ScreenParams.xy;
                v.xy = (v.xy - .5) / .5;
                v.xy *= v.w;
                return v;
            }

            // Fast sRGB -> linear approximation.
            inline float4 SRGBToLinearApprox(float4 c)
            {
                if (IsGammaSpace())
                {
                    return c;
                }
                return c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
            }

            GS_INPUT vert(appdata v)
            {
                GS_INPUT output = (GS_INPUT)0;

                output.pos = float4(UnityObjectToViewPos(v.vertex.xyz), 1);
                output.pos.xyz *= lerp(.99, .95, ORTHO);
                output.pos = mul(UNITY_MATRIX_P, output.pos);
                output.pos = ClipToScreen(output.pos);
                output.color = SRGBToLinearApprox(v.color);

                return output;
            }

            // Expand each point into a screen-aligned quad of size _Scale.
            [maxvertexcount(4)]
            void geo(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
            {
                FS_INPUT outV;
                outV.color = p[0].color;

                outV.pos = ScreenToClip(float4(p[0].pos.x + _Scale, p[0].pos.y - _Scale, p[0].pos.z, p[0].pos.w));
                triStream.Append(outV);

                outV.pos = ScreenToClip(float4(p[0].pos.x + _Scale, p[0].pos.y + _Scale, p[0].pos.z, p[0].pos.w));
                triStream.Append(outV);

                outV.pos = ScreenToClip(float4(p[0].pos.x - _Scale, p[0].pos.y - _Scale, p[0].pos.z, p[0].pos.w));
                triStream.Append(outV);

                outV.pos = ScreenToClip(float4(p[0].pos.x - _Scale, p[0].pos.y + _Scale, p[0].pos.z, p[0].pos.w));
                triStream.Append(outV);
            }

            float4 frag(FS_INPUT input) : COLOR
            {
                return _Color * input.color;
            }
            ENDCG
        }
    }
}
