// Portal VFX — character teleport dissolve: the model burns away in a sweep with a glowing edge,
// keeping its own albedo. Fixed fake key light so it matches the pack in every pipeline.
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _NoiseTex;
float4 _Color, _EdgeColor, _ShadowColor, _LightDir;
float  _Dissolve, _EdgeWidth, _NoiseScale, _SweepAmount, _HeightMin, _HeightMax, _GlowTint;

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
    float3 posWS : TEXCOORD2;
    float3 posOS : TEXCOORD3;
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
    o.posOS = v.vertex.xyz;
    return o;
}

float4 frag(v2f i) : SV_Target
{
    float3 N = normalize(i.normalWS);
    float3 w = pow(abs(N), 4.0);
    w /= w.x + w.y + w.z;
    float3 p = i.posWS * _NoiseScale;
    float n = tex2D(_NoiseTex, p.yz).r * w.x + tex2D(_NoiseTex, p.xz).r * w.y + tex2D(_NoiseTex, p.xy).r * w.z;

    // Sweep from the feet up: lower parts go first, noise breaks the line up.
    float h = saturate((i.posWS.y - _HeightMin) / max(_HeightMax - _HeightMin, 1e-3));
    float field = lerp(n, h * 0.7 + n * 0.3, _SweepAmount);
    float threshold = _Dissolve * 1.15 - 0.05;
    clip(field - threshold);
    float edge = 1.0 - smoothstep(0.0, _EdgeWidth, field - threshold);
    edge *= step(0.001, _Dissolve);

    float3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
    float ndl = dot(N, normalize(_LightDir.xyz)) * 0.5 + 0.5;
    float3 col = lerp(_ShadowColor.rgb * albedo, albedo, ndl * ndl);
    // The whole body picks up the portal colour while it dissolves.
    col += _EdgeColor.rgb * _GlowTint * saturate(_Dissolve * 3.0) * 0.15;
    col += _EdgeColor.rgb * edge * 3.0;
    return float4(col, 1.0);
}
