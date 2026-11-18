using System.Collections;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Put on a character (or any object) to dissolve it out at one point and rebuild it at another.
    /// While teleporting, every renderer temporarily uses a copy of the dissolve material that keeps the
    /// original albedo texture and colour; sparks fly off the dissolving edge. Portals flash on the way.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Portal VFX/Teleportable")]
    public class PVXTeleportable : MonoBehaviour
    {
        [Tooltip("Template using the PortalVFX/Dissolve shader. Assigned on the demo prefab; falls back to Shader.Find.")]
        public Material dissolveMaterial;
        [Tooltip("Material for the sparks that fly off the edge (PortalVFX/Particle).")]
        public Material sparkMaterial;
        [ColorUsage(false, true)] public Color edgeColor = new Color(0.5f, 1.2f, 3f);
        [Tooltip("Take the dissolve edge, sparks and screen warp colour from the portal being passed (lava glows orange, ice blue).")]
        public bool usePortalColor = true;
        [Min(0.05f)] public float dissolveTime = 0.55f;
        [Min(0f)] public float hiddenTime = 0.15f;
        [Tooltip("Seconds before the same object can teleport again.")]
        [Min(0f)] public float cooldown = 1f;

        [Header("Pull Into Portal")]
        [Tooltip("Suck the object into the portal centre (stretch, shrink, spin) and spin it back out on arrival.")]
        public bool pullIn = true;
        [Min(1f)] public float pullStretch = 1.8f;
        public float pullSpin = 300f;
        [Tooltip("Height of the object's middle above its pivot, so its body (not its feet) meets the portal centre.")]
        public float pullCenterOffset = 0.8f;

        public bool IsTeleporting { get; private set; }
        /// <summary>Raised when the object starts dissolving (true) and when it starts rebuilding (false).</summary>
        public event System.Action<PVXTeleportable, bool> Phase;
        public float LastTeleportTime { get; private set; } = -100f;
        /// <summary>Edge colour of the teleport in progress: the portal's own colour, or edgeColor.</summary>
        public Color CurrentEdge => currentEdge ?? edgeColor;
        Color? currentEdge;

        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly int EdgeId = Shader.PropertyToID("_EdgeColor");
        static readonly int MinId = Shader.PropertyToID("_HeightMin");
        static readonly int MaxId = Shader.PropertyToID("_HeightMax");

        Renderer[] renderers;
        Material[][] originals;
        Material[][] dissolving;
        ParticleSystem sparks;

        public bool CanTeleport => !IsTeleporting && Time.time - LastTeleportTime > cooldown;

        /// <summary>Dissolve here, move to the target pose, rebuild there. Portals are optional and flash when passed.</summary>
        public void TeleportTo(Vector3 position, Quaternion rotation, PVXPortal from = null, PVXPortal to = null)
        {
            TeleportTo(position, rotation, from, to, null, null);
        }

        /// <summary>
        /// As above, but with portal centres: with pullIn on, the object is sucked into <paramref name="pullPoint"/>
        /// (stretching, shrinking and spinning while it dissolves) and spun back out of <paramref name="emergePoint"/>.
        /// </summary>
        public void TeleportTo(Vector3 position, Quaternion rotation, PVXPortal from, PVXPortal to, Vector3? pullPoint, Vector3? emergePoint)
        {
            if (!CanTeleport) return;
            StartCoroutine(Run(position, rotation, from, to, pullPoint, emergePoint));
        }

        IEnumerator Run(Vector3 position, Quaternion rotation, PVXPortal from, PVXPortal to, Vector3? pullPoint, Vector3? emergePoint)
        {
            IsTeleporting = true;
            LastTeleportTime = Time.time;
            Prepare();
            Swap(true);
            if (from != null) from.Teleport();
            UseColor(from);
            Phase?.Invoke(this, true);
            Vector3 baseScale = transform.localScale;

            // Sucked in: accelerate toward the portal centre, stretch along the pull, shrink and spin.
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            yield return Animate(0f, 1f, k =>
            {
                if (!pullIn || !pullPoint.HasValue) return;
                float e = k * k * k;
                Vector3 target = pullPoint.Value - Vector3.up * pullCenterOffset * (1f - e);
                transform.position = Vector3.Lerp(startPos, target, e);
                transform.rotation = startRot * Quaternion.Euler(0f, pullSpin * e, 0f);
                float thin = Mathf.Lerp(1f, 0.15f, e);
                transform.localScale = Vector3.Scale(baseScale, new Vector3(thin, Mathf.Lerp(1f, pullStretch, Mathf.Sin(e * Mathf.PI * 0.5f)) * Mathf.Lerp(1f, 0.3f, e * e), thin));
            });
            SetVisible(false);
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = baseScale;
            if (cc != null) cc.enabled = true;
            yield return new WaitForSeconds(hiddenTime);
            if (to != null) to.Teleport();
            UseColor(to != null ? to : from);
            Phase?.Invoke(this, false);
            SetVisible(true);

            // Spun back out: start as a thin streak in the portal centre and unwind into place.
            yield return Animate(1f, 0f, k =>
            {
                if (!pullIn || !emergePoint.HasValue) return;
                float e = 1f - k; // k runs 1 → 0 while rebuilding
                float ease = 1f - Mathf.Pow(1f - e, 3f);
                Vector3 from3 = emergePoint.Value - Vector3.up * pullCenterOffset;
                transform.position = Vector3.Lerp(from3, position, ease);
                transform.rotation = rotation * Quaternion.Euler(0f, -pullSpin * (1f - ease), 0f);
                float thin = Mathf.Lerp(0.15f, 1f, ease);
                transform.localScale = Vector3.Scale(baseScale, new Vector3(thin, Mathf.Lerp(pullStretch * 0.3f, 1f, ease), thin));
            });
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = baseScale;

            Swap(false);
            LastTeleportTime = Time.time;
            IsTeleporting = false;
        }

        /// <summary>Rise into place from <paramref name="from"/> (e.g. below a summoning circle) while materialising.</summary>
        public void Materialize(Vector3 from)
        {
            if (IsTeleporting) return;
            StartCoroutine(MaterializeRun(from));
        }

        /// <summary>Dissolve away while drifting up and thinning out; hidden afterwards.</summary>
        public void Dematerialize()
        {
            if (IsTeleporting) return;
            StartCoroutine(DematerializeRun());
        }

        IEnumerator MaterializeRun(Vector3 from)
        {
            IsTeleporting = true;
            Prepare();
            Swap(true);
            Set(1f);
            SetVisible(true);
            Phase?.Invoke(this, false);
            Vector3 target = transform.position, baseScale = transform.localScale;
            Quaternion rot = transform.rotation;
            yield return Animate(1f, 0f, k =>
            {
                float ease = 1f - Mathf.Pow(k, 3f); // k runs 1 → 0
                transform.position = Vector3.Lerp(from, target, ease);
                transform.rotation = rot * Quaternion.Euler(0f, -pullSpin * (1f - ease), 0f);
                float thin = Mathf.Lerp(0.3f, 1f, ease);
                transform.localScale = Vector3.Scale(baseScale, new Vector3(thin, Mathf.Lerp(1.4f, 1f, ease), thin));
            });
            transform.SetPositionAndRotation(target, rot);
            transform.localScale = baseScale;
            Swap(false);
            IsTeleporting = false;
        }

        IEnumerator DematerializeRun()
        {
            IsTeleporting = true;
            Prepare();
            Swap(true);
            Phase?.Invoke(this, true);
            Vector3 start = transform.position, baseScale = transform.localScale;
            Quaternion rot = transform.rotation;
            yield return Animate(0f, 1f, k =>
            {
                transform.position = start + Vector3.up * k * k * 0.8f;
                transform.rotation = rot * Quaternion.Euler(0f, pullSpin * k * k, 0f);
                float thin = Mathf.Lerp(1f, 0.25f, k * k);
                transform.localScale = Vector3.Scale(baseScale, new Vector3(thin, Mathf.Lerp(1f, 1.5f, k), thin));
            });
            SetVisible(false);
            transform.SetPositionAndRotation(start, rot);
            transform.localScale = baseScale;
            Swap(false);
            IsTeleporting = false;
        }

        public void Hide()
        {
            Prepare();
            SetVisible(false);
        }

        IEnumerator Animate(float from, float to, System.Action<float> step = null)
        {
            if (sparks != null) sparks.Play();
            for (float t = 0f; t < 1f; t += Time.deltaTime / dissolveTime)
            {
                float k = Mathf.Lerp(from, to, t);
                step?.Invoke(k);
                Set(k);
                yield return null;
            }
            step?.Invoke(to);
            Set(to);
            if (sparks != null) sparks.Stop();
        }

        void Set(float amount)
        {
            var b = Bounds();
            foreach (var mats in dissolving)
            foreach (var m in mats)
            {
                m.SetFloat(DissolveId, amount);
                m.SetColor(EdgeId, CurrentEdge);
                m.SetFloat(MinId, b.min.y);
                m.SetFloat(MaxId, b.max.y);
            }
            if (sparks != null)
            {
                var sh = sparks.shape;
                sh.position = sparks.transform.InverseTransformPoint(new Vector3(b.center.x, Mathf.Lerp(b.min.y, b.max.y, amount), b.center.z));
                sh.scale = new Vector3(b.size.x, 0.05f, b.size.z);
            }
        }

        Bounds Bounds()
        {
            var b = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        void Prepare()
        {
            if (renderers != null) return;
            if (dissolveMaterial == null)
            {
                var shader = Shader.Find("PortalVFX/Dissolve");
                if (shader != null) dissolveMaterial = new Material(shader);
            }
            var all = GetComponentsInChildren<Renderer>();
            int count = 0;
            foreach (var r in all) if (r is MeshRenderer || r is SkinnedMeshRenderer) count++;
            renderers = new Renderer[count];
            originals = new Material[count][];
            dissolving = new Material[count][];
            int i = 0;
            foreach (var r in all)
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                renderers[i] = r;
                originals[i] = r.sharedMaterials;
                dissolving[i] = new Material[originals[i].Length];
                for (int k = 0; k < originals[i].Length; k++)
                {
                    var m = new Material(dissolveMaterial);
                    var src = originals[i][k];
                    if (src != null)
                    {
                        if (src.HasProperty("_BaseMap") && src.GetTexture("_BaseMap") != null) m.SetTexture("_MainTex", src.GetTexture("_BaseMap"));
                        else if (src.HasProperty("_MainTex") && src.GetTexture("_MainTex") != null) m.SetTexture("_MainTex", src.GetTexture("_MainTex"));
                        if (src.HasProperty("_BaseColor")) m.SetColor("_Color", src.GetColor("_BaseColor"));
                        else if (src.HasProperty("_Color")) m.SetColor("_Color", src.GetColor("_Color"));
                    }
                    dissolving[i][k] = m;
                }
                i++;
            }
            BuildSparks();
        }

        /// <summary>Switch the edge, spark (and, through Phase, screen warp) colour to the portal's theme.</summary>
        void UseColor(PVXPortal portal)
        {
            if (usePortalColor && portal != null && PVXPortalLight.TryTheme(portal, out var theme))
            {
                // Normalised hue at the brightness of edgeColor, so every portal flashes equally hot.
                float m = Mathf.Max(theme.r, Mathf.Max(theme.g, theme.b), 1e-3f);
                float hot = Mathf.Max(edgeColor.r, Mathf.Max(edgeColor.g, edgeColor.b));
                currentEdge = new Color(theme.r / m * hot, theme.g / m * hot, theme.b / m * hot, 1f);
            }
            else currentEdge = null;
            if (sparks != null)
            {
                var main = sparks.main;
                main.startColor = CurrentEdge;
            }
        }

        void BuildSparks()
        {
            if (sparkMaterial == null) return;
            var go = new GameObject("TeleportSparks");
            go.transform.SetParent(transform, false);
            sparks = go.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = edgeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.15f;
            var em = sparks.emission;
            em.rateOverTime = 120f;
            var sh = sparks.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            var col = sparks.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = sparkMaterial;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 2f;
        }

        void Swap(bool toDissolve)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].sharedMaterials = toDissolve ? dissolving[i] : originals[i];
        }

        void SetVisible(bool visible)
        {
            foreach (var r in renderers) if (r != null) r.enabled = visible;
        }

        void OnDestroy()
        {
            if (dissolving == null) return;
            foreach (var mats in dissolving) foreach (var m in mats) if (m != null) Destroy(m);
        }
    }
}
