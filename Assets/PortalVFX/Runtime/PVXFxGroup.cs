using UnityEngine;

namespace PortalVFX
{
    public enum PVXFxRole { Open, Loop, Teleport, Close }

    /// <summary>A set of particle systems the portal plays when the matching state starts.</summary>
    [AddComponentMenu("Portal VFX/Fx Group")]
    public class PVXFxGroup : MonoBehaviour
    {
        public PVXFxRole role = PVXFxRole.Loop;

        ParticleSystem[] systems;

        ParticleSystem[] Systems
        {
            get
            {
                if (systems == null) systems = GetComponentsInChildren<ParticleSystem>(true);
                return systems;
            }
        }

        public void Play()
        {
            foreach (var ps in Systems)
            {
                // One-shot groups restart from scratch; loops keep running if already alive.
                if (role != PVXFxRole.Loop) ps.Clear(false);
                ps.Play(false);
            }
        }

        public void Stop(bool clear)
        {
            foreach (var ps in Systems)
                ps.Stop(false, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
