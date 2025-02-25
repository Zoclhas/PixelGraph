#include "lib/common_structs.hlsl"
#include "lib/common_funcs.hlsl"
#include "lib/parallax.hlsl"
#include "lib/pbr_material.hlsl"

#pragma pack_matrix(row_major)


float4 main(const ps_input input) : SV_TARGET
{
	const float3 normal = normalize(input.nor);
    const float3 tangent = normalize(input.tan);
    const float3 bitangent = normalize(input.bin);
	const float3 view = normalize(input.eye);

    const float surface_NoV = saturate(dot(normal, view));

	float tex_depth = 0;
    float3 shadow_tex = 0;
	const float2 tex = get_parallax_texcoord(input.tex, input.vTS, surface_NoV, shadow_tex, tex_depth);

	const pbr_material mat = get_pbr_material(tex);

    if (BlendMode == BLEND_CUTOUT)
		clip(mat.alpha - CUTOUT_THRESHOLD);
    else if (BlendMode == BLEND_TRANSPARENT)
		clip(mat.alpha - EPSILON);

    float3 tex_normal = calc_tex_normal(tex, normal, tangent, bitangent);

    if (EnableSlopeNormals && !EnableLinearSampling && tex_depth - shadow_tex.z > 0.002f) {
	    const float3 slope = apply_slope_normal(tex, input.vTS, shadow_tex.z);

		const float3x3 matTBN = float3x3(tangent, bitangent, normal);
	    tex_normal = mul(slope, matTBN);
    }

    return float4(tex_normal * 0.5f + 0.5f, 1.0f);
}
