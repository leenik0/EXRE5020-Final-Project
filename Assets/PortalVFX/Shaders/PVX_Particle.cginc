// Portal VFX — particles, trails and line renderers.
// Texture alpha is the shape, vertex color tints it. With "Alpha Erodes" on, vertex alpha burns the
// sprite away against the noise texture instead of a plain fade.
#include "PVX_Common.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _NoiseTex;
float4 _Tint;
float  _Intensity, _PulseBoost, _ErodeByAlpha, _ErodeSoft, _NoiseScale;

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
    PVX_SOFT_COORDS(1)
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
    PVX_TRANSFER_SOFT(o, o.pos)
    return o;
}

float4 frag(v2f i) : SV_Target
{
    float4 tex = tex2D(_MainTex, i.uv);
    float shape = tex.a;
    float alpha;
    if (_ErodeByAlpha > 0.5)
    {
        float n = tex2D(_NoiseTex, i.uv * _NoiseScale).r;
        float threshold = 1.0 - i.color.a;
        alpha = smoothstep(threshold, threshold + _ErodeSoft, shape * (0.55 + n * 0.45));
    }
    else
    {
        alpha = shape * i.color.a;
    }
    alpha *= _Tint.a * saturate(_Open) * PVX_SOFT_FADE(i);

    float3 col = tex.rgb * i.color.rgb * _Tint.rgb * _Intensity * (1.0 + _Pulse * _PulseBoost);
    col = PVX_Final(col);
    return float4(col * alpha, alpha);
}
