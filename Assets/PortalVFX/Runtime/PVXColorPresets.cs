using System;
using System.Collections.Generic;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>Ready-made colour variants for a portal. Pick one in the inspector or call Apply(index) at runtime.</summary>
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Color Presets")]
    public class PVXColorPresets : MonoBehaviour
    {
        [Serializable]
        public struct Preset
        {
            public string name;
            [Range(-180f, 180f)] public float hueShift;
            [ColorUsage(false, true)] public Color tint;
            [Min(0f)] public float intensity;
        }

        public List<Preset> presets = new List<Preset>();
        [Tooltip("Applied on enable. -1 keeps whatever the portal has.")]
        public int current = -1;

        void OnEnable()
        {
            if (current >= 0) Apply(current);
        }

        public void Apply(int index)
        {
            if (index < 0 || index >= presets.Count) return;
            current = index;
            var p = presets[index];
            var portal = GetComponent<PVXPortal>();
            portal.hueShift = p.hueShift;
            portal.tint = p.tint;
            portal.intensity = p.intensity;
        }

        public void Next() => Apply(presets.Count == 0 ? -1 : (current + 1) % presets.Count);

        /// <summary>Default set: original look plus three hue variants.</summary>
        public static List<Preset> Defaults()
        {
            return new List<Preset>
            {
                new Preset { name = "Original", hueShift = 0f, tint = Color.white, intensity = 1f },
                new Preset { name = "Variant A", hueShift = 120f, tint = Color.white, intensity = 1f },
                new Preset { name = "Variant B", hueShift = -120f, tint = Color.white, intensity = 1f },
                new Preset { name = "Variant C", hueShift = 180f, tint = Color.white, intensity = 1f },
            };
        }
    }
}
