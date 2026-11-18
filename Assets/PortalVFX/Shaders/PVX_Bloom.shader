// Portal VFX — lightweight bloom for the Built-in pipeline (URP/HDRP use their own Bloom volume).
// Pass 0: bright-pass prefilter, 1: 13-tap downsample, 2: tent upsample (additive), 3: combine + tonemap.
Shader "Hidden/PortalVFX/Bloom"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    float _Threshold, _Knee, _BloomIntensity, _Scatter, _Exposure, _Vignette, _Contrast, _Gain;

    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    float3 Down(float2 uv)
    {
        float2 d = _MainTex_TexelSize.xy;
        float3 a = tex2D(_MainTex, uv + d * float2(-1, -1)).rgb;
        float3 b = tex2D(_MainTex, uv + d * float2( 1, -1)).rgb;
        float3 c = tex2D(_MainTex, uv + d * float2(-1,  1)).rgb;
        float3 e = tex2D(_MainTex, uv + d * float2( 1,  1)).rgb;
        float3 m = tex2D(_MainTex, uv).rgb;
        return m * 0.5 + (a + b + c + e) * 0.125;
    }

    float4 fragPrefilter(v2f i) : SV_Target
    {
        float3 c = Down(i.uv);
        float br = max(c.r, max(c.g, c.b));
        float soft = clamp(br - _Threshold + _Knee, 0.0, 2.0 * _Knee);
        soft = soft * soft / (4.0 * _Knee + 1e-4);
        float contrib = max(soft, br - _Threshold) / max(br, 1e-4);
        return float4(min(c * contrib, 60.0), 1.0);
    }

    float4 fragDown(v2f i) : SV_Target { return float4(Down(i.uv), 1.0); }

    float4 fragUp(v2f i) : SV_Target
    {
        float2 d = _MainTex_TexelSize.xy;
        float3 s = tex2D(_MainTex, i.uv + d * float2(-1, -1)).rgb + tex2D(_MainTex, i.uv + d * float2(1, -1)).rgb
                 + tex2D(_MainTex, i.uv + d * float2(-1,  1)).rgb + tex2D(_MainTex, i.uv + d * float2(1,  1)).rgb;
        s = s * 0.25;
        return float4(s, _Scatter);
    }

    float4 fragCombine(v2f i) : SV_Target
    {
        float3 c = tex2D(_MainTex, i.uv).rgb + tex2D(_BloomTex, i.uv).rgb * _BloomIntensity;
        c *= _Exposure;
        // ACES-like filmic curve keeps hot portal cores from clipping to flat white.
        c = saturate((c * (2.51 * c + 0.03)) / (c * (2.43 * c + 0.59) + 0.14));
        // Contrast curve fitted to URP's ACES output, so Built-in demos look the same (deep blacks, rich cores).
        c = _Gain * pow(c, _Contrast);
        float2 v = i.uv - 0.5;
        c *= 1.0 - dot(v, v) * _Vignette;
        return float4(c, 1.0);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragPrefilter
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDown
            ENDCG }
        // Same as URP: each upsample lerps toward the blurrier level by Scatter, so energy stays normalised.
        Pass { Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragUp
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragCombine
            ENDCG }
    }
    Fallback Off
}
