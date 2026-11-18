// Portal VFX — solid portal frames: stone, crystal, bone, metal, paper.
// Unlit with a fixed fake key light (no LightMode tag) so it renders the same in Built-in, URP and HDRP.
// Lit by nearby portals through PVXPortalLight; "Wet Floor" adds puddles that mirror the portals.
// The frame dissolves in and out with the portal's Open value when "Dissolve With Portal" is on.
Shader "PortalVFX/Mesh"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.35, 0.35, 0.4, 1)
        _ShadowColor ("Shadow Tint", Color) = (0.25, 0.25, 0.45, 1)
        _LightDir ("Fake Light Direction", Vector) = (0.4, 0.8, 0.5, 0)

        [Header(Hand Painted)]
        _PaintAmount ("Hand-Painted Amount", Range(0, 1)) = 1
        _PaintScale ("Brush Scale", Float) = 1.6
        _ToonSteps ("Light Bands", Range(1, 6)) = 3
        _ToonSoft ("Band Softness", Range(0.01, 1)) = 0.35
        _TopColor ("Top Light Tint", Color) = (1, 0.92, 0.75, 1)

        [Header(Emissive Veins)]
        [NoScaleOffset] _NoiseTex ("Noise (R)", 2D) = "gray" {}
        [HDR] _EmissionColor ("Vein Color", Color) = (0.3, 0.7, 3, 1)
        _VeinAmount ("Vein Amount", Range(0, 10)) = 1
        _VeinScale ("Vein Scale", Float) = 0.6
        _VeinWidth ("Vein Width", Range(0.005, 0.5)) = 0.04
        _VeinSpeed ("Vein Flow Speed", Float) = 0.05
        _VeinPulse ("Vein Pulse", Range(0, 1)) = 0.3

        [Header(Rim)]
        [HDR] _RimColor ("Rim Color", Color) = (0.4, 0.8, 2, 1)
        _RimAmount ("Rim Amount", Range(0, 10)) = 0.6
        _RimPower ("Rim Power", Range(0.5, 16)) = 3
        _Iridescence ("Iridescence", Range(0, 1)) = 0

        [Header(Dissolve)]
        [Toggle] _DissolveByOpen ("Dissolve With Portal", Float) = 1
        [HDR] _DissolveColor ("Dissolve Edge Color", Color) = (1, 1.5, 4, 1)
        _DissolveEdge ("Dissolve Edge Width", Range(0.01, 0.3)) = 0.08

        [Header(Portal Light)]
        _PortalLight ("Portal Light Response", Range(0, 3)) = 1
        [Toggle(PVX_WET)] _Wet ("Wet Floor (puddles + reflections)", Float) = 0
        _WetScale ("Puddle Scale", Float) = 0.12
        _WetCoverage ("Puddle Coverage", Range(0, 1)) = 0.5
        _WetGloss ("Wet Gloss", Range(0, 1)) = 0.85
        _ReflectAmount ("Reflection Strength", Range(0, 2)) = 1

        [Header(Output)]
        _Intensity ("Emission Intensity", Range(0, 20)) = 1
        _PulseBoost ("Teleport Flash Boost", Range(0, 10)) = 1.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

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
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
        Pass
        {
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma shader_feature_local PVX_WET
            #include "PVX_Mesh.cginc"
            ENDCG
        }
    }

    // ── HDRP ──
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
        Pass
        {
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma shader_feature_local PVX_WET
            #include "PVX_Mesh.cginc"
            ENDCG
        }
    }

    // ── Built-in ──
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
        Pass
        {
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma shader_feature_local PVX_WET
            #include "PVX_Mesh.cginc"
            ENDCG
        }
    }
    Fallback Off
}
