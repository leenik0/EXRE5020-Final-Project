// Portal VFX — character teleport dissolve. Used by PVXTeleportable while a character passes through a portal.
// Unlit with a fixed fake key light (no LightMode tag) so it renders the same in Built-in, URP and HDRP.
Shader "PortalVFX/Dissolve"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Tint", Color) = (0.35, 0.35, 0.5, 1)
        _LightDir ("Fake Light Direction", Vector) = (0.4, 0.8, 0.5, 0)
        [NoScaleOffset] _NoiseTex ("Noise (R)", 2D) = "gray" {}
        _NoiseScale ("Noise Scale", Float) = 1.5
        [HDR] _EdgeColor ("Edge Glow", Color) = (0.5, 1.2, 3, 1)
        _EdgeWidth ("Edge Width", Range(0.01, 0.3)) = 0.08
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _SweepAmount ("Bottom-Up Sweep", Range(0, 1)) = 0.7
        _HeightMin ("Sweep Bottom (world Y)", Float) = 0
        _HeightMax ("Sweep Top (world Y)", Float) = 2
        _GlowTint ("Body Glow", Range(0, 4)) = 1
    }

    // ── URP ──
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "PVX_Dissolve.cginc"
            ENDCG
        }
    }

    // ── HDRP ──
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "PVX_Dissolve.cginc"
            ENDCG
        }
    }

    // ── Built-in ──
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "PVX_Dissolve.cginc"
            ENDCG
        }
    }
    Fallback Off
}
