// Portal VFX — solid frames (stone, crystal, bone, metal, wood) with emissive veins, rim glow and
// an Open-driven dissolve. Lighting is a fixed fake key light so it looks identical in every pipeline.
#include "PVX_Common.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _NoiseTex;
float4 _BaseColor, _ShadowColor, _EmissionColor, _RimColor, _DissolveColor, _LightDir;
float  _VeinScale, _VeinSpeed, _VeinWidth, _VeinAmount, _VeinPulse;
float  _RimPower, _RimAmount, _Iridescence;
float  _DissolveByOpen, _DissolveEdge, _Intensity, _PulseBoost;
float4 _TopColor;
float  _PaintAmount, _PaintScale, _ToonSteps, _ToonSoft;
float  _PortalLight, _WetScale, _WetCoverage, _WetGloss, _ReflectAmount;
sampler2D _PVXReflectionTex;  // planar reflection from PVXPlanarReflection
float  _PVXReflectStrength;   // 0 when no reflection camera is running

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 viewWS : TEXCOORD2;
    float3 posOS : TEXCOORD3;
    float3 normalOS : TEXCOORD4;
    float3 posWS : TEXCOORD5;
#if defined(PVX_WET)
    float4 screenPos : TEXCOORD6;
#endif
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normalWS = UnityObjectToWorldNormal(v.normal);
    o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.viewWS = _WorldSpaceCameraPos - o.posWS;
#if defined(PVX_WET)
    o.screenPos = ComputeScreenPos(o.pos);
#endif
    o.posOS = v.vertex.xyz;
    o.normalOS = v.normal;
    return o;
}

float4 Triplanar4(float3 p, float3 n)
{
    float3 w = pow(abs(n), 4.0) + 1e-4;
    w /= w.x + w.y + w.z;
    return tex2D(_NoiseTex, p.yz) * w.x + tex2D(_NoiseTex, p.xz) * w.y + tex2D(_NoiseTex, p.xy) * w.z;
}

float Triplanar(float3 p, float3 n) { return Triplanar4(p, n).r; }

float4 frag(v2f i, float facing : VFACE) : SV_Target
{
    float t = _Time.y;
    float3 N = normalize(i.normalWS) * (facing > 0 ? 1.0 : -1.0);
    float3 V = normalize(i.viewWS);

    float nn = Triplanar(i.posOS * _VeinScale + float3(0.0, t * _VeinSpeed, 0.0), i.normalOS);

    float threshold = (1.0 - saturate(_Open)) * 1.05 * _DissolveByOpen;
    clip(nn - threshold);
    float edge = (1.0 - smoothstep(0.0, _DissolveEdge, nn - threshold)) * step(0.001, threshold);

    // Hand-painted look: brushy two-tone albedo, soft toon bands, warm top light, cool bounce.
    float4 paint = Triplanar4(i.posOS * _PaintScale, i.normalOS);
    float brush = lerp(paint.b, paint.g, 0.35);
    float3 albedo = tex2D(_MainTex, i.uv).rgb * _BaseColor.rgb;
    albedo *= lerp(1.0, 0.72 + brush * 0.6, _PaintAmount);
    float ndl = dot(N, normalize(_LightDir.xyz)) * 0.5 + 0.5;
    float bands = floor(ndl * _ToonSteps) / _ToonSteps;
    float soft = smoothstep(0.0, _ToonSoft, frac(ndl * _ToonSteps)) / _ToonSteps;
    float light = saturate(lerp(ndl * ndl, bands + soft, _PaintAmount));
    float3 col = lerp(_ShadowColor.rgb * albedo, albedo, light);
    col += _TopColor.rgb * albedo * saturate(N.y) * 0.6;
    col += _ShadowColor.rgb * albedo * saturate(-N.y) * 0.25;

    float3 spec;
    float wet = 0.0;
    float specPower = 24.0;
#if defined(PVX_WET)
    // Puddles: two noise octaves on world XZ, darker albedo and a sharp gloss where wet.
    float puddle = tex2D(_NoiseTex, i.posWS.xz * _WetScale).r * 0.65 + tex2D(_NoiseTex, i.posWS.xz * _WetScale * 3.1 + 0.37).r * 0.35;
    wet = smoothstep(1.0 - _WetCoverage, 1.0 - _WetCoverage + 0.08, puddle);
    specPower = lerp(24.0, 400.0, wet * _WetGloss);
    col *= lerp(1.0, 0.55, wet);
#endif
    float3 portalLit = PVX_PortalLight(i.posWS, N, V, specPower, spec) * _PortalLight;
    col += albedo * portalLit + spec * _PortalLight * lerp(0.15, 2.5, wet * _WetGloss);
#if defined(PVX_WET)
    float2 suv = i.screenPos.xy / i.screenPos.w;
    float2 ripple = (tex2D(_NoiseTex, i.posWS.xz * 0.9 + t * 0.03).rg - 0.5) * 0.012;
    float3 refl = tex2D(_PVXReflectionTex, suv + ripple).rgb;
    float fresWet = 0.35 + 0.65 * pow(1.0 - saturate(dot(N, V)), 3.0);
    col += refl * _PVXReflectStrength * _ReflectAmount * lerp(0.12, 1.0, wet) * fresWet;
#endif

    float fres = pow(1.0 - saturate(dot(N, V)), _RimPower);
    // Veins appear in patches only, so surfaces read as painted stone/bone rather than crackle.
    float patchMask = smoothstep(0.45, 0.7, paint.r);
    float veins = (1.0 - smoothstep(0.0, _VeinWidth, abs(nn - 0.5))) * lerp(1.0, patchMask, _PaintAmount);
    veins *= 1.0 + sin(t * 2.0 + nn * 12.0) * _VeinPulse;
    float3 emissive = _EmissionColor.rgb * veins * _VeinAmount + _RimColor.rgb * fres * _RimAmount;
    float facet = dot(N, float3(0.31, 0.53, 0.79)) + dot(V, float3(0.6, 0.2, 0.1));
    emissive = lerp(emissive, PVX_Rainbow(fres * 1.2 + facet * 0.5 + t * 0.08) * (fres * 2.0 + 0.25) * _RimColor.rgb, _Iridescence);
    emissive *= _Intensity * (1.0 + _Pulse * _PulseBoost);

    col = PVX_Final(col + emissive + _DissolveColor.rgb * edge);
    return float4(col, 1.0);
}
