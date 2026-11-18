// Portal VFX — refraction behind and around portals.
// Built-in: uses a shared GrabPass. URP: samples the camera Opaque Texture — enable "Opaque Texture" on
// the URP asset or the effect shows nothing. HDRP: not supported (draws nothing).
Shader "PortalVFX/Distortion"
{
    Properties
    {
        _MainTex ("Mask (alpha)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Noise (RG)", 2D) = "gray" {}
        [Toggle] _UseMask ("Circular Quad Mask", Float) = 1
        _Radius ("Mask Radius", Range(0.1, 1)) = 0.95
        _EdgeSoft ("Mask Softness", Range(0.01, 1)) = 0.5
        _Strength ("Shimmer Strength", Range(0, 0.2)) = 0.03
        _Lens ("Lens Pull", Range(-0.2, 0.2)) = 0.04
        _Swirl ("Swirl Speed", Float) = 0.05
        _NoiseScale ("Noise Scale", Float) = 1.5
        _NoiseSpeed ("Noise Speed", Float) = 0.4
        _Tint ("Tint (alpha = amount)", Color) = (1, 1, 1, 0)

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
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-20" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "PVX_Distortion.cginc"
            ENDCG
        }
    }

    // ── HDRP: no opaque texture access from this shader, so nothing is drawn ──
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            ColorMask 0
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            float4 frag() : SV_Target { return 0; }
            ENDCG
        }
    }

    // ── Built-in ──
    SubShader
    {
        Tags { "Queue" = "Transparent-20" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        GrabPass { "_PVXGrabTexture" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #define PVX_GRAB
            #include "PVX_Distortion.cginc"
            ENDCG
        }
    }
    Fallback Off
}
