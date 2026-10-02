// Edge picker (original implementation) for runtime ProBuilder editing.
// Edge colors come from vertex color and are alpha-tested; pass "Edges" is
// referenced by SelectionPicker.
Shader "CodingDaniel/MEBuilder/EdgePicker"
{
    Properties {}

    SubShader
    {
        Tags
        {
            "ProBuilderPicker"="EdgePass"
            "IgnoreProjector"="True"
            "DisableBatching"="True"
        }

        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Off
        Blend Off

        Pass
        {
            Name "Edges"
            AlphaTest Greater .25

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
                o.pos = UnityObjectToClipPos(v.vertex);
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
