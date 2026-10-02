// Shared code for the MapEditor outline render pass.
//
// Pipeline:
//   1. Prepass   - the layer's renderers are drawn solid white into _PrepassTex.
//   2. Composite - the mask is dilated into a band by _OutlineOffset, tinted with
//                  _OutlineColor and merged over a copy of the camera colour.
//
// The band is a max over eight offset samples rather than a blurred-then-subtracted glow: a
// dilation gives a uniform-width stroke by construction and needs no intermediate halo target.
#ifndef ME_URP_OUTLINE_INCLUDED
#define ME_URP_OUTLINE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct OutlineVertexInput
{
	float4 pos : POSITION;
	float2 uv  : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVertexOutput
{
	float4 clipPos : SV_POSITION;
	float2 uv      : TEXCOORD0;
	UNITY_VERTEX_OUTPUT_STEREO
};

TEXTURE2D_X(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;

// Shared by the composite pass. Declared once at file scope.
float _OutlineStrength;

// ---------------------------------------------------------------------------
// Prepass: mark the selection area.
// ---------------------------------------------------------------------------

struct PrepassVertexInput
{
	float4 pos : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PrepassVertexOutput
{
	float4 clipPos : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

PrepassVertexOutput PrepassVert(PrepassVertexInput input)
{
	PrepassVertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	output.clipPos = TransformObjectToHClip(input.pos.xyz);
	return output;
}

float4 PrepassFrag(PrepassVertexOutput input) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	return float4(1, 0, 0, 1);
}

// ---------------------------------------------------------------------------
// Fullscreen blit helper (used by the composite pass).
// ---------------------------------------------------------------------------

OutlineVertexOutput FullscreenVert(OutlineVertexInput input)
{
	OutlineVertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	output.clipPos = TransformObjectToHClip(input.pos.xyz);
	output.uv = input.uv;
	return output;
}

// ---------------------------------------------------------------------------
// Composite: dilate the mask into a band and merge it over the scene.
// ---------------------------------------------------------------------------

TEXTURE2D_X(_PrepassTex);
SAMPLER(sampler_PrepassTex);

// The dilation offset arrives already normalised into UV space from the render feature, so this
// shader needs no texel-size uniform of its own.
float2 _OutlineOffset;

float4 _OutlineColor;

// Samples the object mask. Only the red channel matters: the prepass writes solid white on the
// object and the target is cleared to black.
float SampleMask(float2 uv)
{
	return SAMPLE_TEXTURE2D_X(_PrepassTex, sampler_PrepassTex, uv).r;
}

float4 CompositeFrag(OutlineVertexOutput input) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
	float2 texel = _OutlineOffset;

	// An outline is the mask dilated by the radius and then reduced by the mask itself, i.e. the
	// band just outside the silhouette. The dilation is a max over a ring of offsets, which yields a
	// uniform-width stroke without any intermediate blur target to get wrong.
	float core = SampleMask(uv);

	float ring = 0;
	ring = max(ring, SampleMask(uv + float2( texel.x, 0)));
	ring = max(ring, SampleMask(uv + float2(-texel.x, 0)));
	ring = max(ring, SampleMask(uv + float2(0,  texel.y)));
	ring = max(ring, SampleMask(uv + float2(0, -texel.y)));
	ring = max(ring, SampleMask(uv + float2( texel.x,  texel.y)));
	ring = max(ring, SampleMask(uv + float2(-texel.x,  texel.y)));
	ring = max(ring, SampleMask(uv + float2( texel.x, -texel.y)));
	ring = max(ring, SampleMask(uv + float2(-texel.x, -texel.y)));

	// Inside the silhouette ring and core cancel, leaving only the outward band.
	float outline = saturate((ring - core) * _OutlineStrength);

	float4 scene = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv);

	return lerp(scene, _OutlineColor, outline);
}

#endif
