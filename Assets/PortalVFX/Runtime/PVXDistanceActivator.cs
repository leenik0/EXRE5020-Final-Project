using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Opens portals as a target approaches and closes them once it walks away, so big scenes only pay for
    /// the portals nearby and every portal greets you with its opening effect.
    /// </summary>
    [AddComponentMenu("Portal VFX/Distance Activator")]
    public class PVXDistanceActivator : MonoBehaviour
    {
        public Transform target;
        public PVXPortal[] portals = new PVXPortal[0];
        public float openDistance = 20f;
        public float closeDistance = 26f;

        void Start()
        {
            foreach (var p in portals)
                if (p != null && Far(p, openDistance)) p.SetClosedInstant();
        }

        void Update()
        {
            if (target == null) return;
            foreach (var p in portals)
            {
                if (p == null) continue;
                if (p.State == PVXState.Closed && !Far(p, openDistance)) p.Open();
                else if (p.State == PVXState.Open && Far(p, closeDistance)) p.Close();
            }
        }

        bool Far(PVXPortal p, float d) => target != null && (p.transform.position - target.position).sqrMagnitude > d * d;
    }
}
