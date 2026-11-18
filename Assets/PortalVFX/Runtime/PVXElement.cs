using UnityEngine;

namespace PortalVFX
{
    public enum PVXEase { Linear, OutCubic, OutBack, OutElastic, InOutSine, InCubic }

    [System.Flags]
    public enum PVXScaleAxes { None = 0, X = 1, Y = 2, Z = 4, XY = 3, All = 7 }

    /// <summary>
    /// One animated part of a portal. Its local progress is the portal's open progress remapped to
    /// [delay, delay + span] and eased; from that it scales, moves, rotates and feeds _Open / _Fill
    /// to its renderers. Closing plays the same curve backwards, so parts that appeared last vanish first.
    /// </summary>
    [AddComponentMenu("Portal VFX/Portal Element")]
    public class PVXElement : MonoBehaviour
    {
        [Header("Timing (fractions of the open duration)")]
        [Range(0f, 1f)] public float delay;
        [Range(0.01f, 1f)] public float span = 1f;
        public PVXEase ease = PVXEase.OutCubic;

        [Header("Appear")]
        public PVXScaleAxes scaleAxes = PVXScaleAxes.None;
        public Vector3 moveFrom;
        public Vector3 rotateFrom;
        [Tooltip("Feed eased progress to the material _Open property (surfaces grow, frames dissolve in).")]
        public bool driveOpen = true;
        [Tooltip("Feed eased progress to the material _Fill property (rings draw themselves around).")]
        public bool driveFill;

        [Header("Idle motion")]
        [Tooltip("Degrees per second, local axes.")]
        public Vector3 spin;
        [Tooltip("Spin speed follows open progress, so orbiting debris accelerates as the portal opens.")]
        public bool spinFollowsOpen;
        public float bobAmplitude;
        public float bobFrequency = 1f;
        [Tooltip("Breathing scale: amplitude of a slow sine on the scale.")]
        public float breathe;
        public float breatheFrequency = 1f;

        [Header("Teleport reaction")]
        public float pulseScale = 0.08f;
        public float pulseSpin = 2f;

        [Header("Light (optional)")]
        public float lightFlicker = 0.1f;

        public float ShaderOpen { get; private set; } = 1f;
        public float ShaderFill { get; private set; } = 1f;

        Vector3 baseScale, basePos;
        Quaternion baseRot;
        Vector3 spinAngles;
        Light lightSource;
        float baseLight, phase;
        bool captured;

        public void Capture()
        {
            if (captured) return;
            captured = true;
            baseScale = transform.localScale;
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            lightSource = GetComponent<Light>();
            if (lightSource != null) baseLight = lightSource.intensity;
            phase = Mathf.Abs(transform.GetSiblingIndex() * 1.618f + name.Length * 0.37f);
        }

        public void Evaluate(float portalOpen, bool closing, float pulse, float dt)
        {
            float p = Mathf.Clamp01((portalOpen - delay) / Mathf.Max(span, 0.0001f));
            float e = Ease(ease, p);
            float t = Time.time;

            ShaderOpen = driveOpen ? Mathf.Clamp01(e) : (portalOpen > 0f ? 1f : 0f);
            ShaderFill = driveFill ? Mathf.Clamp01(e) : 1f;

            float s = Mathf.Max(e, 0f);
            Vector3 scale = baseScale;
            if ((scaleAxes & PVXScaleAxes.X) != 0) scale.x *= s;
            if ((scaleAxes & PVXScaleAxes.Y) != 0) scale.y *= s;
            if ((scaleAxes & PVXScaleAxes.Z) != 0) scale.z *= s;
            float breath = 1f + Mathf.Sin(t * breatheFrequency * Mathf.PI * 2f + phase) * breathe;
            scale *= breath * (1f + pulse * pulseScale);
            transform.localScale = scale;

            float spinRate = (spinFollowsOpen ? Mathf.Clamp01(portalOpen) : 1f) * (1f + pulse * pulseSpin);
            spinAngles += spin * spinRate * dt;
            spinAngles.x %= 360f; spinAngles.y %= 360f; spinAngles.z %= 360f;

            float inv = 1f - e;
            Vector3 pos = basePos + moveFrom * inv;
            if (bobAmplitude != 0f) pos.y += Mathf.Sin(t * bobFrequency * Mathf.PI * 2f + phase) * bobAmplitude;
            transform.localPosition = pos;
            transform.localRotation = baseRot * Quaternion.Euler(rotateFrom * inv) * Quaternion.Euler(spinAngles);

            if (lightSource != null)
            {
                float flicker = 1f + (Mathf.PerlinNoise(t * 8f, phase) - 0.5f) * 2f * lightFlicker;
                lightSource.intensity = baseLight * Mathf.Clamp01(e) * flicker * (1f + pulse * 2f);
            }
        }

        public static float Ease(PVXEase ease, float x)
        {
            switch (ease)
            {
                case PVXEase.OutCubic: return 1f - Mathf.Pow(1f - x, 3f);
                case PVXEase.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
                }
                case PVXEase.OutElastic:
                    if (x <= 0f) return 0f;
                    if (x >= 1f) return 1f;
                    return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case PVXEase.InOutSine: return -(Mathf.Cos(Mathf.PI * x) - 1f) / 2f;
                case PVXEase.InCubic: return x * x * x;
                default: return x;
            }
        }
    }
}
