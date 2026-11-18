// Portal VFX — shared helpers for every PVX shader.
// Plain UnityCG code with no LightMode tag, so the same passes render in Built-in, URP and HDRP.
#ifndef PVX_COMMON_INCLUDED
#define PVX_COMMON_INCLUDED

#include "UnityCG.cginc"

#define PVX_TAU 6.28318530718
#define PVX_PI  3.14159265359

// Written per renderer by PVXPortal through a MaterialPropertyBlock (defaults live in each shader's Properties).
float  _Open;          // 0 = closed, 1 = fully open
float  _Fill;          // arc reveal for rings
float  _Pulse;         // teleport flash envelope
float  _HueShift;      // degrees
float  _GlobalIntensity;
float4 _GlobalTint;

float PVX_Hash11(float n)
{
    return frac(sin(n * 127.1) * 43758.5453);
}

// Rodrigues rotation around the grey axis: cheap hue shift that keeps HDR values.
float3 PVX_HueShiftRGB(float3 c, float degrees)
{
    float a = radians(degrees);
    const float3 k = float3(0.57735, 0.57735, 0.57735);
    float ca = cos(a);
    return c * ca + cross(k, c) * sin(a) + k * dot(k, c) * (1.0 - ca);
}

float3 PVX_Final(float3 c)
{
    if (abs(_HueShift) > 0.01)
        c = max(PVX_HueShiftRGB(c, _HueShift), 0.0);
    return c * _GlobalTint.rgb * _GlobalIntensity;
}

float3 PVX_Rainbow(float x)
{
    return saturate(abs(frac(x + float3(0.0, 0.6667, 0.3333)) * 6.0 - 3.0) - 1.0);
}

// Portal light: the nearest portals light every PVX surface (floors, frames, props) in every pipeline.
// PVXPortalLight fills these globals each frame.
#define PVX_MAX_LIGHTS 8
float4 _PVXLightPos[PVX_MAX_LIGHTS];   // xyz = world position, w = 1 / range^2
float4 _PVXLightColor[PVX_MAX_LIGHTS]; // rgb = colour x intensity
float  _PVXLightCount;

float3 PVX_PortalLight(float3 posWS, float3 N, float3 V, float specPower, out float3 spec)
{
    float3 diff = 0;
    spec = 0;
    [unroll]
    for (int k = 0; k < PVX_MAX_LIGHTS; k++)
    {
        if (k < (int)_PVXLightCount)
        {
            float3 L = _PVXLightPos[k].xyz - posWS;
            float d2 = dot(L, L);
            float att = saturate(1.0 - d2 * _PVXLightPos[k].w);
            att = att * att / (1.0 + d2 * 0.12);
            L *= rsqrt(max(d2, 1e-4));
            float3 c = _PVXLightColor[k].rgb * att;
            diff += c * saturate(dot(N, L) * 0.75 + 0.25);
            spec += c * pow(saturate(dot(N, normalize(L + V))), specPower);
        }
    }
    return diff;
}

#if defined(PVX_USE_SOFT)
    UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
    float _SoftFade;
    #define PVX_SOFT_COORDS(n) float4 projPos : TEXCOORD##n;
    #define PVX_TRANSFER_SOFT(o, clipPos) o.projPos = ComputeScreenPos(clipPos); COMPUTE_EYEDEPTH(o.projPos.z);
    #define PVX_SOFT_FADE(i) saturate((LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))) - i.projPos.z) / _SoftFade)
#else
    #define PVX_SOFT_COORDS(n)
    #define PVX_TRANSFER_SOFT(o, clipPos)
    #define PVX_SOFT_FADE(i) 1.0
#endif

#endif
