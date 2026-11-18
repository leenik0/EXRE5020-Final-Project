using System.Collections;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Demo helper: keeps playing portals through their phases (open, idle, teleport, close) or, with
    /// cyclePresets, steps through a portal's colour presets. Used by the features demo scene.
    /// </summary>
    [AddComponentMenu("Portal VFX/Demo Loop")]
    public class PVXDemoLoop : MonoBehaviour
    {
        public PVXPortal[] portals = new PVXPortal[0];
        public bool openClose = true;
        public bool teleport = true;
        public float idleTime = 3f;
        public float closedTime = 1f;
        [Tooltip("Instead of opening and closing, step the colour presets of every portal.")]
        public bool cyclePresets;
        public float presetTime = 2.5f;

        IEnumerator Start()
        {
            yield return null;
            while (true)
            {
                if (cyclePresets)
                {
                    yield return new WaitForSeconds(presetTime);
                    foreach (var p in portals)
                    {
                        var presets = p != null ? p.GetComponent<PVXColorPresets>() : null;
                        if (presets != null) presets.Next();
                    }
                    continue;
                }
                yield return new WaitForSeconds(idleTime * 0.5f);
                if (teleport) foreach (var p in portals) if (p != null) p.Teleport();
                yield return new WaitForSeconds(idleTime * 0.5f);
                if (!openClose) continue;
                foreach (var p in portals) if (p != null) p.Close();
                yield return new WaitForSeconds(closedTime + 1f);
                foreach (var p in portals) if (p != null) p.Open();
                yield return new WaitForSeconds(2f);
            }
        }
    }
}
