// Portal VFX — portal surface (vortex / tunnel / liquid / void / grid / destination window).
// Unlit, no LightMode tag: renders in Built-in, URP and HDRP. Use on a quad (PVX_Quad mesh).
// Blending: Src One + Dst OneMinusSrcAlpha = solid surface (voids, mirrors); Dst One = additive glow.
Shader "PortalVFX/Surface"
{
    Properties
    {
        [KeywordEnum(Circle, Arch, Rect)] _Shape ("Shape", Float) = 0
        _Aspect ("Aspect (quad height / width)", Float) = 1
        _Roundness ("Rect Roundness (higher = sharper)", Range(2, 16)) = 6
        _Radius ("Radius", Range(0.1, 1)) = 0.95
        _EdgeSoft ("Edge Softness", Range(0.001, 1)) = 0.12
        _EdgeNoise ("Edge Noise", Range(0, 1)) = 0.15

        [Header(Colors)]
        [HDR] _ColorCenter ("Center", Color) = (0.05, 0.1, 0.5, 1)
        [HDR] _ColorMid ("Middle", Color) = (0.2, 0.5, 2, 1)
        [HDR] _ColorEdge ("Edge", Color) = (0.6, 1.2, 3, 1)
        [HDR] _CoreColor ("Core Glow", Color) = (1, 1, 1, 1)
        _CoreAmount ("Core Amount", Range(0, 10)) = 1
        _CorePower ("Core Falloff", Range(0.5, 32)) = 6
        [HDR] _RimColor ("Rim Glow", Color) = (1, 1, 1, 1)
        _RimAmount ("Rim Amount", Range(0, 10)) = 1
        _RimPower ("Rim Falloff", Range(0.5, 32)) = 8
        _VoidAmount ("Dark Center Amount", Range(0, 1)) = 0
        _VoidSize ("Dark Center Size", Range(0.01, 1.5)) = 0.4
        _RainbowAmount ("Rainbow", Range(0, 1)) = 0
        _RainbowSpeed ("Rainbow Speed", Float) = 0.1

        [Header(Flow)]
        [NoScaleOffset] _NoiseTex ("Noise (R fbm, G cells)", 2D) = "gray" {}
        _NoiseTiling ("Noise Tiling (A angle, A depth, B angle, B depth) — angle values must be whole numbers", Vector) = (2, 1, 3, 1.5)
        _NoiseAmount ("Noise Amount", Range(0, 1)) = 0.8
        _Noise2Amount ("Second Noise Amount", Range(0, 1)) = 0.5
        _Twist ("Twist", Range(-8, 8)) = 2
        _SwirlSpeed ("Swirl Speed (turns/s)", Range(-2, 2)) = 0.1
        _FlowSpeed ("Depth Flow Speed", Range(-4, 4)) = 0.4
        _Tunnel ("Tunnel Perspective", Range(0, 1)) = 0.5
        _DepthPower ("Depth Power", Range(0.1, 4)) = 1
        _Arms ("Spiral Arms (whole number)", Float) = 3
        _ArmTwist ("Arm Twist", Range(-8, 8)) = 1
        _ArmsAmount ("Arms Amount", Range(0, 1)) = 0
        _ArmSharp ("Arms Sharpness", Range(0.2, 16)) = 1
        _RingFreq ("Rings Frequency", Range(0, 40)) = 6
        _RingSpeed ("Rings Speed", Range(-8, 8)) = 1
        _RingsAmount ("Rings Amount", Range(0, 1)) = 0
        _Wobble ("Outline Wobble", Range(0, 0.5)) = 0
        _WobbleFreq ("Wobble Lobes (whole number)", Float) = 5
        _WobbleSpeed ("Wobble Speed", Float) = 3

        [Header(Destination Window)]
        _InnerTex ("Destination Texture", 2D) = "black" {}
        [HDR] _InnerColor ("Destination Tint", Color) = (1, 1, 1, 1)
        _InnerAmount ("Destination Amount", Range(0, 1)) = 0
        _InnerRadius ("Destination Radius", Range(0, 1.5)) = 0.8
        _InnerZoom ("Destination Zoom", Range(0.1, 3)) = 0.8
        _InnerParallax ("Destination Parallax", Range(-2, 2)) = 0.4
        _InnerWarp ("Destination Warp", Range(0, 1)) = 0.05
        _InnerScroll ("Destination Scroll (xy)", Vector) = (0, 0, 0, 0)

        [Header(Grid and Pixels)]
        [HDR] _GridColor ("Grid Color", Color) = (0, 1, 1, 1)
        _GridAmount ("Grid Amount", Range(0, 4)) = 0
        _GridDensity ("Grid Density", Float) = 12
        _GridWidth ("Grid Line Width", Range(0.001, 0.5)) = 0.04
        _GridSpeed ("Grid Scroll", Float) = 0
        _Pixelate ("Pixelate (0 = off)", Float) = 0

        [Header(Output)]
        _Intensity ("Intensity", Range(0, 20)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 1
        _PulseBoost ("Teleport Flash Boost", Range(0, 10)) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend (One = additive)", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle(PVX_SOFT)] _SoftParticles ("Soft Intersection", Float) = 0
        _SoftFade ("Soft Fade Distance", Range(0.01, 3)) = 0.3

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
            #pragma shader_feature_local _SHAPE_CIRCLE _SHAPE_ARCH _SHAPE_RECT
            #pragma shader_feature_local PVX_SOFT
            #if defined(PVX_SOFT)
                #define PVX_USE_SOFT
            #endif
            #include "PVX_Surface.cginc"
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
            #pragma shader_feature_local _SHAPE_CIRCLE _SHAPE_ARCH _SHAPE_RECT
            #include "PVX_Surface.cginc"
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
            #pragma multi_compile _ SOFTPARTICLES_ON
            #pragma shader_feature_local _SHAPE_CIRCLE _SHAPE_ARCH _SHAPE_RECT
            #pragma shader_feature_local PVX_SOFT
            #if defined(PVX_SOFT) && defined(SOFTPARTICLES_ON)
                #define PVX_USE_SOFT
            #endif
            #include "PVX_Surface.cginc"
            ENDCG
        }
    }
    Fallback Off
}
