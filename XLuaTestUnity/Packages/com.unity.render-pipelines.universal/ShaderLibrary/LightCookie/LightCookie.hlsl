// Textures
TEXTURE2D(_MainLightCookieTexture);

// Samplers
SAMPLER(sampler_MainLightCookieTexture);

// Buffers
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef SHADER_API_GLES3
CBUFFER_START(LightCookies)
#endif
    float4x4 _MainLightWorldToLight;
#ifndef SHADER_API_GLES3
CBUFFER_END
#endif

real4 SampleMainLightCookieTexture(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv);
}


float2 ComputeLightCookieUVDirectional(float4x4 worldToLight, float3 samplePositionWS, float4 atlasUVRect)
{
    // Translate and rotate 'positionWS' into the light space.
    // Project point to light "view" plane, i.e. discard Z.
    float2 positionLS = mul(worldToLight, float4(samplePositionWS, 1)).xy;

    // Remap [-1, 1] to [0, 1]
    // (implies the transform has ortho projection mapping world space box to [-1, 1])
    float2 positionUV = positionLS * 0.5 + 0.5;

    // Tile texture for cookie in repeat mode
    positionUV.x = frac(positionUV.x);
    positionUV.y = frac(positionUV.y);
    // positionUV.x = saturate(positionUV.x);
    // positionUV.y = saturate(positionUV.y);

    // Remap to atlas texture
    float2 positionAtlasUV = atlasUVRect.xy * float2(positionUV) + atlasUVRect.zw;

    return positionAtlasUV;
}

real3 SampleMainLightCookie(float3 samplePositionWS)
{
    float2 uv = ComputeLightCookieUVDirectional(_MainLightWorldToLight, samplePositionWS, float4(1, 1, 0, 0));
    real4 color = SampleMainLightCookieTexture(uv);

    return color.rgb;
}