// Portal VFX — procedural ring: energy bands, rune circles, clock dials, polygons, sound waves.
// Drawn on a square quad; the ring lives between _Inner and _Outer (1 = quad edge).
#include "PVX_Common.cginc"

sampler2D _NoiseTex;
sampler2D _RuneTex;

float4 _ColorA, _ColorB, _RuneColor, _PulseColor;
float  _Inner, _Outer, _Softness, _CoreSharp, _ProfileAmount;
float  _EdgeLine, _EdgeLineWidth, _Sides, _RotSpeed;
float  _RuneAmount, _RuneCount, _RuneSeed, _RuneFlicker;
float4 _RuneBand;
float  _Segments, _Gap, _SegSoft;
float  _Ticks, _TickAmount, _TickLength, _TickWidth;
float  _PulseCount, _PulseSpeed, _PulseSharp, _PulseAmount;
float  _FillStart, _FillSoft, _OpenGrow;
float  _EdgeNoise, _NoiseSpeed, _NoiseAmount;
float4 _NoiseTiling;
float  _Wobble, _WobbleFreq, _WobbleSpeed;
float  _RainbowAmount, _RainbowSpeed;
float  _Intensity, _Opacity, _PulseBoost;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}

float4 frag(v2f i) : SV_Target
{
    float t = _Time.y;
    float2 q = (i.uv - 0.5) * 2.0;
    float theta = atan2(q.y, q.x);
    float rawA = theta / PVX_TAU + 0.5;
    float a = frac(rawA + t * _RotSpeed);
    float r = length(q);

    if (_Sides >= 3.0)
    {
        float s = PVX_TAU / _Sides;
        float th = theta + t * _RotSpeed * PVX_TAU;
        r = r * cos(floor(0.5 + th / s) * s - th) / cos(PVX_PI / _Sides);
    }

    r /= max(lerp(1.0, saturate(_Open), _OpenGrow), 1e-3);
    r += sin(a * PVX_TAU * _WobbleFreq + t * _WobbleSpeed) * _Wobble;

    float n = tex2Dlod(_NoiseTex, float4(a * _NoiseTiling.x, r * _NoiseTiling.y + t * _NoiseSpeed, 0, 0)).r;
    float v = (r - _Inner) / max(_Outer - _Inner, 1e-4) + (n - 0.5) * _EdgeNoise;

    float band = smoothstep(0.0, _Softness, v) * smoothstep(1.0, 1.0 - _Softness, v);
    if (_Segments > 0.0)
    {
        // Soft segment ends: fade in after the gap and out before the next one, so arcs never start with a hard cut.
        float sf = frac(a * _Segments);
        float soft = max(_SegSoft, 0.02);
        band *= smoothstep(_Gap, _Gap + soft, sf) * smoothstep(1.0, 1.0 - soft, sf);
    }

    float profile = pow(saturate(1.0 - abs(v * 2.0 - 1.0)), _CoreSharp);
    float3 col = lerp(_ColorA.rgb, _ColorB.rgb, saturate(v)) * lerp(1.0, profile, _ProfileAmount);
    col *= lerp(1.0, n * 1.6, _NoiseAmount);
    col = lerp(col, PVX_Rainbow(a + t * _RainbowSpeed) * dot(col, 0.4) * 2.0, _RainbowAmount);

    float lines = (1.0 - smoothstep(0.0, _EdgeLineWidth, abs(v))) + (1.0 - smoothstep(0.0, _EdgeLineWidth, abs(v - 1.0)));
    col += _ColorB.rgb * lines * _EdgeLine;

    float pf = frac(a * _PulseCount - t * _PulseSpeed);
    // Soft head: fade the last 6% so the comet front never ends in a hard line.
    col += _PulseColor.rgb * pow(pf, _PulseSharp) * smoothstep(1.0, 0.94, pf) * _PulseAmount * profile;

    float coverage = band;

    float tf = frac(a * _Ticks);
    float tick = (1.0 - smoothstep(0.0, _TickWidth, min(tf, 1.0 - tf))) * step(1.0 - _TickLength, v) * step(v, 1.0);
    float3 tickCol = _ColorB.rgb * tick * _TickAmount;
    coverage = max(coverage, tick * saturate(_TickAmount));

    // Runes: one glyph per angular cell, picked from a 4x4 atlas by hash.
    float3 runeCol = 0;
    if (_RuneAmount > 0.0)
    {
        float idx = floor(a * _RuneCount);
        float rv = (v - _RuneBand.x) / max(_RuneBand.y - _RuneBand.x, 1e-4);
        float cell = floor(PVX_Hash11(idx + _RuneSeed * 17.0) * 16.0);
        float2 ruv = (float2(fmod(cell, 4.0), floor(cell / 4.0)) + float2(frac(a * _RuneCount), saturate(rv))) / 4.0;
        float inside = step(0.0, rv) * step(rv, 1.0);
        float glyph = tex2Dlod(_RuneTex, float4(ruv, 0, 0)).r * inside;
        glyph *= lerp(1.0, 0.45 + 0.55 * sin(t * 3.0 + idx * 1.7), _RuneFlicker);
        runeCol = _RuneColor.rgb * glyph * _RuneAmount;
        coverage = max(coverage, glyph);
    }

    float fillA = frac(a - _FillStart);
    float fillMask = _Fill >= 0.999 ? 1.0 : 1.0 - smoothstep(_Fill - _FillSoft, _Fill, fillA);
    float alpha = saturate(coverage * _Opacity) * fillMask * smoothstep(0.0, 0.1, _Open);

    col = (col * band + tickCol + runeCol) * _Intensity * (1.0 + _Pulse * _PulseBoost);
    col = PVX_Final(col) * fillMask * smoothstep(0.0, 0.1, _Open);
    return float4(col, alpha);
}
