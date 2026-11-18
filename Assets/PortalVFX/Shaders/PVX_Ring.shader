// Portal VFX — procedural ring (energy bands, rune circles, clock dials, polygons, ground sigils).
// Unlit, no LightMode tag: renders in Built-in, URP and HDRP. Use on a square quad (PVX_Quad mesh).
Shader "PortalVFX/Ring"
{
    Properties
    {
        [Header(Band)]
        _Inner ("Inner Radius", Range(0, 1)) = 0.8
        _Outer ("Outer Radius", Range(0, 1)) = 0.95
        _Softness ("Softness", Range(0.001, 0.5)) = 0.15
        _Sides ("Polygon Sides (0 = circle)", Float) = 0
        _RotSpeed ("Rotation (turns/s)", Range(-2, 2)) = 0.05
        [HDR] _ColorA ("Inner Color", Color) = (0.2, 0.5, 2, 1)
        [HDR] _ColorB ("Outer Color", Color) = (1, 1.5, 3, 1)
        _ProfileAmount ("Center Glow Profile", Range(0, 1)) = 1
        _CoreSharp ("Profile Sharpness", Range(0.1, 16)) = 1.5
        _EdgeLine ("Edge Lines", Range(0, 4)) = 0
        _EdgeLineWidth ("Edge Line Width", Range(0.005, 0.5)) = 0.06
        _Segments ("Segments (0 = solid)", Float) = 0
        _Gap ("Segment Gap", Range(0, 1)) = 0.2
        _SegSoft ("Segment Edge Softness", Range(0, 0.5)) = 0.02
        _RainbowAmount ("Rainbow", Range(0, 1)) = 0
        _RainbowSpeed ("Rainbow Speed", Float) = 0.1

        [Header(Noise)]
        [NoScaleOffset] _NoiseTex ("Noise (R fbm)", 2D) = "gray" {}
        _NoiseTiling ("Noise Tiling (x angle whole number, y radial)", Vector) = (4, 1, 0, 0)
        _NoiseSpeed ("Noise Speed", Float) = 0.3
        _NoiseAmount ("Noise Amount", Range(0, 1)) = 0.3
        _EdgeNoise ("Edge Noise", Range(0, 2)) = 0
        _Wobble ("Wobble", Range(0, 0.3)) = 0
        _WobbleFreq ("Wobble Lobes (whole number)", Float) = 6
        _WobbleSpeed ("Wobble Speed", Float) = 4

        [Header(Runes)]
        [NoScaleOffset] _RuneTex ("Rune Atlas 4x4", 2D) = "black" {}
        [HDR] _RuneColor ("Rune Color", Color) = (1, 0.8, 0.3, 1)
        _RuneAmount ("Rune Amount", Range(0, 4)) = 0
        _RuneCount ("Rune Count", Float) = 16
        _RuneBand ("Rune Band (x min, y max across band)", Vector) = (0.1, 0.9, 0, 0)
        _RuneSeed ("Rune Seed", Float) = 1
        _RuneFlicker ("Rune Flicker", Range(0, 1)) = 0.3

        [Header(Ticks)]
        _Ticks ("Tick Count", Float) = 0
        _TickAmount ("Tick Amount", Range(0, 4)) = 0
        _TickLength ("Tick Length (band fraction)", Range(0, 1)) = 0.5
        _TickWidth ("Tick Width", Range(0.001, 0.5)) = 0.08

        [Header(Travelling Pulses)]
        [HDR] _PulseColor ("Pulse Color", Color) = (1, 1, 1, 1)
        _PulseAmount ("Pulse Amount", Range(0, 10)) = 0
        _PulseCount ("Pulse Count (whole number)", Float) = 2
        _PulseSpeed ("Pulse Speed", Float) = 0.5
        _PulseSharp ("Pulse Tail Sharpness", Range(1, 64)) = 12

        [Header(Reveal)]
        _FillStart ("Fill Start (turns)", Range(0, 1)) = 0.25
        _FillSoft ("Fill Edge Softness", Range(0.001, 0.5)) = 0.05
        _OpenGrow ("Grow From Center While Opening", Range(0, 1)) = 0

        [Header(Output)]
        _Intensity ("Intensity", Range(0, 20)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 1
        _PulseBoost ("Teleport Flash Boost", Range(0, 10)) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend (One = additive)", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0

        [HideInInspector] _Open ("Open", Float) = 1
        [HideInInspector] _Fill ("Fill", Float) = 1
        [HideInInspector] _Pulse ("Pulse", Float) = 0
        [HideInInspector] _HueShift ("Hue Shift", Float) = 0
        [HideInInspector] _GlobalIntensity ("Global Intensity", Float) = 1
        [HideInInspector] _GlobalTint ("Global Tint", Color) = (1, 1, 1, 1)
    }

    // ── URP ──
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "PVX_Ring.cginc"
            ENDCG
        }
    }

    // ── HDRP ──
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "PVX_Ring.cginc"
            ENDCG
        }
    }

    // ── Built-in ──
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "PVX_Ring.cginc"
            ENDCG
        }
    }
    Fallback Off
}
