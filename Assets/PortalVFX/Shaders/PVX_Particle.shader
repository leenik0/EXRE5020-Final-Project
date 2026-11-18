// Portal VFX — particles, trails and line renderers.
// Unlit, no LightMode tag: renders in Built-in, URP and HDRP.
// Premultiplied output: Dst One = additive glow, Dst OneMinusSrcAlpha = solid (smoke, ink, debris).
Shader "PortalVFX/Particle"
{
    Properties
    {
        _MainTex ("Sprite (alpha = shape)", 2D) = "white" {}
        [HDR] _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 20)) = 2
        [Toggle] _ErodeByAlpha ("Vertex Alpha Erodes", Float) = 0
        _ErodeSoft ("Erode Softness", Range(0.01, 1)) = 0.15
        [NoScaleOffset] _NoiseTex ("Erode Noise", 2D) = "gray" {}
        _NoiseScale ("Erode Noise Scale", Float) = 1
        _PulseBoost ("Teleport Flash Boost", Range(0, 10)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend (One = additive)", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle(PVX_SOFT)] _SoftParticles ("Soft Particles", Float) = 0
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
            #pragma multi_compile_instancing
            #pragma shader_feature_local PVX_SOFT
            #if defined(PVX_SOFT)
                #define PVX_USE_SOFT
            #endif
            #include "PVX_Particle.cginc"
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
            #pragma multi_compile_instancing
            #include "PVX_Particle.cginc"
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
            #pragma multi_compile_instancing
            #pragma multi_compile _ SOFTPARTICLES_ON
            #pragma shader_feature_local PVX_SOFT
            #if defined(PVX_SOFT) && defined(SOFTPARTICLES_ON)
                #define PVX_USE_SOFT
            #endif
            #include "PVX_Particle.cginc"
            ENDCG
        }
    }
    Fallback Off
}
