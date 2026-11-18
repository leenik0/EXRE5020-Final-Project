// Portal VFX — portal surface: vortex, tunnel, liquid, void, grid, destination window.
// Drawn on a quad. Shape is computed procedurally (circle/ellipse, arch, rounded rect).
#include "PVX_Common.cginc"

sampler2D _NoiseTex;
sampler2D _InnerTex;
float4 _InnerTex_ST;

float4 _ColorCenter, _ColorMid, _ColorEdge, _CoreColor, _RimColor, _InnerColor, _GridColor;
float  _Aspect, _Roundness, _Radius, _EdgeSoft, _EdgeNoise;
float  _Twist, _SwirlSpeed, _FlowSpeed, _Tunnel, _DepthPower;
float4 _NoiseTiling;
float  _NoiseAmount, _Noise2Amount;
float  _Arms, _ArmTwist, _ArmsAmount, _ArmSharp;
float  _RingFreq, _RingSpeed, _RingsAmount;
float  _CorePower, _CoreAmount, _VoidAmount, _VoidSize;
float  _RimPower, _RimAmount;
float  _InnerAmount, _InnerZoom, _InnerParallax, _InnerRadius, _InnerWarp;
float4 _InnerScroll;
float  _GridAmount, _GridDensity, _GridWidth, _GridSpeed;
float  _Pixelate, _Wobble, _WobbleFreq, _WobbleSpeed;
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
    PVX_SOFT_COORDS(1)
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    PVX_TRANSFER_SOFT(o, o.pos)
    return o;
}

// Normalised distance to the shape outline: 0 at centre, 1 on the edge.
// q.x spans -1..1, q.y spans -_Aspect.._Aspect (aspect = quad height / width).
float ShapeDistance(float2 q)
{
#if defined(_SHAPE_ARCH)
    float cy = max(_Aspect - 1.0, 0.0);
    return q.y > cy ? length(float2(q.x, q.y - cy)) : max(abs(q.x), -q.y / _Aspect);
#elif defined(_SHAPE_RECT)
    float n = _Roundness;
    return pow(pow(abs(q.x), n) + pow(abs(q.y / _Aspect), n), 1.0 / n);
#else
    return length(float2(q.x, q.y / _Aspect));
#endif
}

float4 frag(v2f i) : SV_Target
{
    float t = _Time.y;
    float2 uv = i.uv;
    if (_Pixelate > 1.0)
        uv = (floor(uv * _Pixelate) + 0.5) / _Pixelate;

    float2 q = (uv - 0.5) * 2.0;
    q.y *= _Aspect;
    float ang = atan2(q.y, q.x) / PVX_TAU + 0.5;
    float d = ShapeDistance(q);
    d *= 1.0 + sin(ang * PVX_TAU * _WobbleFreq + t * _WobbleSpeed) * _Wobble;

    float open = saturate(_Open);
    float rr = d / max(_Radius * open, 1e-4);
    float g = saturate(rr);

    // Polar flow field: twist toward the centre, scroll along depth.
    float depth = pow(lerp(rr, 0.35 / (rr + 0.05), _Tunnel), _DepthPower);
    float swirl = ang + _Twist * (1.0 - g) + t * _SwirlSpeed;
    float n1 = tex2Dlod(_NoiseTex, float4(swirl * _NoiseTiling.x, depth * _NoiseTiling.y + t * _FlowSpeed, 0, 0)).r;
    float n2 = tex2Dlod(_NoiseTex, float4(swirl * _NoiseTiling.z - t * _SwirlSpeed * 0.5, depth * _NoiseTiling.w + t * _FlowSpeed * 1.37, 0, 0)).g;
    float n = lerp(n1, n1 * n2 * 2.0, _Noise2Amount);

    float arms = pow(0.5 + 0.5 * cos((swirl * _Arms + depth * _ArmTwist) * PVX_TAU), _ArmSharp);
    float rings = 0.5 + 0.5 * cos((rr * _RingFreq - t * _RingSpeed) * PVX_TAU);
    float pattern = lerp(1.0, n * 1.6, _NoiseAmount)
                  * lerp(1.0, arms * 1.5, _ArmsAmount)
                  * lerp(1.0, rings * 1.5, _RingsAmount);

    float3 col = g < 0.5 ? lerp(_ColorCenter.rgb, _ColorMid.rgb, g * 2.0)
                         : lerp(_ColorMid.rgb, _ColorEdge.rgb, g * 2.0 - 1.0);
    col *= pattern;
    col = lerp(col, PVX_Rainbow(ang + rr * 0.5 + t * _RainbowSpeed) * dot(col, 0.4) * 2.0, _RainbowAmount);
    col *= lerp(1.0, smoothstep(0.0, _VoidSize, rr), _VoidAmount);
    col += _CoreColor.rgb * pow(saturate(1.0 - rr), _CorePower) * _CoreAmount;
    col += _RimColor.rgb * pow(g, _RimPower) * _RimAmount * (0.6 + n * 0.8);

    // Destination window: a texture seen through the portal with depth parallax.
    float2 iuv = 0.5 + (uv - 0.5) * (_InnerZoom + g * _InnerParallax);
    iuv = iuv * _InnerTex_ST.xy + _InnerTex_ST.zw + _InnerScroll.xy * t + (n - 0.5) * _InnerWarp;
    float3 inner = tex2D(_InnerTex, iuv).rgb * _InnerColor.rgb;
    float innerMask = 1.0 - smoothstep(_InnerRadius - 0.25, _InnerRadius, rr);
    col = lerp(col, inner + col * 0.35, _InnerAmount * innerMask);

    float2 gf = abs(frac(uv * _GridDensity + float2(0.0, t * _GridSpeed)) - 0.5);
    float gridLine = 1.0 - smoothstep(0.0, _GridWidth, 0.5 - max(gf.x, gf.y));
    col += _GridColor.rgb * gridLine * _GridAmount * (0.5 + n);

    float mask = 1.0 - smoothstep(1.0 - _EdgeSoft, 1.0, rr + (n1 - 0.5) * _EdgeNoise);
    mask *= smoothstep(0.0, 0.05, open);
    float alpha = saturate(mask * _Opacity) * PVX_SOFT_FADE(i);

    col *= _Intensity * (1.0 + _Pulse * _PulseBoost);
    col = PVX_Final(col);
    return float4(col * alpha, alpha);
}
