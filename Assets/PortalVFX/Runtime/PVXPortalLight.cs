using System.Collections.Generic;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Lets a portal light its surroundings: floors, walls, frames and props using PortalVFX/Mesh pick up its
    /// colour, flicker, open/close fade and teleport flash, in every render pipeline. It mirrors the portal's
    /// point light (also kept for Standard/Lit materials) and follows tint, hue shift and intensity.
    /// The 8 portals nearest the camera are lit each frame.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Portal Light")]
    public class PVXPortalLight : MonoBehaviour
    {
        public const int MaxLights = 8;

        [Tooltip("Point light to mirror. Found among the children when empty.")]
        public Light source;
        [Min(0f)] public float strength = 0.5f;
        [Tooltip("Range multiplier over the point light's own range.")]
        [Min(0.1f)] public float rangeScale = 1f;
        [Tooltip("Extra flicker on top of the light's own (0 = steady).")]
        [Range(0f, 1f)] public float flicker = 0.15f;

        static readonly List<PVXPortalLight> active = new List<PVXPortalLight>();
        static readonly Vector4[] positions = new Vector4[MaxLights];
        static readonly Vector4[] colors = new Vector4[MaxLights];
        static int pushedFrame = -1;
        static readonly int PosId = Shader.PropertyToID("_PVXLightPos");
        static readonly int ColorId = Shader.PropertyToID("_PVXLightColor");
        static readonly int CountId = Shader.PropertyToID("_PVXLightCount");

        PVXPortal portal;
        float seed;

        void Awake()
        {
            portal = GetComponent<PVXPortal>();
            if (source == null) source = GetComponentInChildren<Light>(true);
            seed = Random.value * 100f;
        }

        void OnEnable()
        {
            if (portal == null) Awake();
            active.Add(this);
            if (!Application.isPlaying) Push();
        }

        void OnDisable()
        {
            active.Remove(this);
            if (active.Count == 0) Shader.SetGlobalFloat(CountId, 0f);
        }

        /// <summary>Current colour x intensity (black while closed).</summary>
        public Color Current
        {
            get
            {
                if (source == null || !source.isActiveAndEnabled || (Application.isPlaying && portal.State == PVXState.Closed)) return Color.black;
                float t = Time.time * 9f + seed;
                float f = 1f + (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f * flicker;
                Color c = source.color * portal.tint;
                if (Mathf.Abs(portal.hueShift) > 0.01f) c = HueShift(c, portal.hueShift);
                return c * (source.intensity * portal.intensity * portal.IntensityScale * strength * f);
            }
        }

        /// <summary>
        /// The portal's theme colour (its light colour with tint and hue shift applied), for effects that should
        /// match it: teleport dissolve, sparks, screen warp. False when the portal has no light.
        /// </summary>
        public static bool TryTheme(PVXPortal portal, out Color theme)
        {
            var pl = portal.GetComponent<PVXPortalLight>();
            var l = pl != null && pl.source != null ? pl.source : portal.GetComponentInChildren<Light>(true);
            if (l == null) { theme = Color.white; return false; }
            Color c = l.color * portal.tint;
            if (Mathf.Abs(portal.hueShift) > 0.01f) c = HueShift(c, portal.hueShift);
            theme = c;
            return c.maxColorComponent > 0.01f;
        }

        public Vector3 Position => source != null ? source.transform.position : transform.position + transform.up * 2f;
        public float Range => (source != null ? source.range : 6f) * rangeScale * transform.lossyScale.x;

        void LateUpdate()
        {
            // In the editor (not playing) push every call so the Scene view lights up without Play.
            if (Application.isPlaying && pushedFrame == Time.frameCount) return;
            pushedFrame = Time.frameCount;
            Push();
        }

        static void Push()
        {
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            if (active.Count > MaxLights) active.Sort((a, b) => (a.Position - eye).sqrMagnitude.CompareTo((b.Position - eye).sqrMagnitude));
            int n = 0;
            for (int i = 0; i < active.Count && n < MaxLights; i++)
            {
                var l = active[i];
                Color c = l.Current;
                if (c.maxColorComponent <= 0.001f) continue;
                Vector3 p = l.Position;
                float r = Mathf.Max(0.1f, l.Range);
                positions[n] = new Vector4(p.x, p.y, p.z, 1f / (r * r));
                colors[n] = new Vector4(c.r, c.g, c.b, 1f);
                n++;
            }
            for (int i = n; i < MaxLights; i++) colors[i] = Vector4.zero;
            Shader.SetGlobalVectorArray(PosId, positions);
            Shader.SetGlobalVectorArray(ColorId, colors);
            Shader.SetGlobalFloat(CountId, n);
        }

        static Color HueShift(Color c, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector3 k = new Vector3(0.57735f, 0.57735f, 0.57735f), v = new Vector3(c.r, c.g, c.b);
            Vector3 r = v * ca + Vector3.Cross(k, v) * sa + k * Vector3.Dot(k, v) * (1f - ca);
            return new Color(Mathf.Max(0f, r.x), Mathf.Max(0f, r.y), Mathf.Max(0f, r.z), c.a);
        }
    }
}
