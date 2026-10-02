Shader "CodingDaniel/MEBuilder/SelectionPicker"
{
    Properties {}

    SubShader
    {
        Tags { "ProBuilderPicker"="EdgePass" }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Off
        Blend Off

        UsePass "CodingDaniel/MEBuilder/EdgePicker/Edges"
    }

    SubShader
    {
        Tags { "ProBuilderPicker"="VertexPass" }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Off
        Blend Off

        UsePass "CodingDaniel/MEBuilder/VertexPicker/Vertices"
    }

    SubShader
    {
        Tags { "ProBuilderPicker"="Base" }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Back
        Blend Off

        UsePass "CodingDaniel/MEBuilder/FacePicker/Base"
    }
}
