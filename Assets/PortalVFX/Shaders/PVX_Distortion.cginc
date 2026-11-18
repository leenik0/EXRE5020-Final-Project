// Portal VFX — screen-space refraction: bends whatever is behind the portal.
// Built-in: GrabPass texture. URP: _CameraOpaqueTexture (enable "Opaque Texture" on the URP asset).
// On a quad the circular mask, swirl and lens pull are procedural; on particles the sprite alpha
// and vertex alpha scale the strength.
#include "PVX_Common.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _NoiseTex;
#if defined(PVX_GRAB)
    sampler2D _PVXGrabTexture;
    #define PVX_SCENE_TEX _PVXGrabTexture
#else
    sampler2D _CameraOpaqueTexture;
    #define PVX_SCENE_TEX _CameraOpaqueTexture
#endif

float _Strength, _Lens, _Swirl, _NoiseScale, _NoiseSpeed, _Radius, _EdgeSoft, _UseMask;
float4 _Tint;

struct appdata
{
    float4 vertex : POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    float4 screen : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.color = v.color;
#if defined(PVX_GRAB)
    o.screen = ComputeGrabScreenPos(o.pos);
#else
    o.screen = ComputeScreenPos(o.pos);
#endif
    return o;
}

float4 frag(v2f i) : SV_Target
{
    float t = _Time.y;
    float2 q = (i.uv - 0.5) * 2.0;
    float r = length(q) / max(_Radius * saturate(_Open), 1e-3);
    float ang = atan2(q.y, q.x) / PVX_TAU;

    float mask = _UseMask > 0.5 ? (1.0 - smoothstep(1.0 - _EdgeSoft, 1.0, r)) : 1.0;
    mask *= tex2D(_MainTex, i.uv).a * i.color.a * smoothstep(0.0, 0.05, _Open);

    float2 n = tex2D(_NoiseTex, float2(ang * 2.0 + t * _Swirl, r * _NoiseScale - t * _NoiseSpeed)).rg - 0.5;
    float2 dir = r > 1e-4 ? q / length(q) : 0;
    // Lens: pull the background toward the centre, strongest near the rim, plus a noisy shimmer.
    float lens = sin(saturate(r) * PVX_PI) * _Lens;
    float2 offset = (-dir * lens + n * _Strength) * mask * (1.0 + _Pulse * 2.0);

    float2 suv = i.screen.xy / i.screen.w + offset;
    float3 scene = tex2D(PVX_SCENE_TEX, suv).rgb;
    scene = lerp(scene, scene * _Tint.rgb, mask * _Tint.a);
    return float4(scene, smoothstep(0.0, 1.0, mask));
}
