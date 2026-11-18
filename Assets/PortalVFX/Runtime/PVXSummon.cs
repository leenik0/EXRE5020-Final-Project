using System.Collections;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Summons a creature out of a portal: the portal opens, the creature (any object with PVXTeleportable)
    /// rises out of it materialising, and on Dismiss() dissolves away before the portal closes.
    /// Call Summon() / Dismiss() from gameplay, or tick "loop" for a demo cycle.
    /// </summary>
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Summon")]
    public class PVXSummon : MonoBehaviour
    {
        public PVXTeleportable creature;
        [Tooltip("Where the creature stands, relative to the portal.")]
        public Vector3 standOffset = Vector3.zero;
        [Tooltip("How far below its stand point the creature starts rising.")]
        public float riseDepth = 1.6f;
        public bool loop;
        public float holdTime = 3f;
        public float closedTime = 1.5f;

        PVXPortal portal;

        void Awake() => portal = GetComponent<PVXPortal>();

        IEnumerator Start()
        {
            if (creature == null) yield break;
            yield return null;
            creature.Hide();
            while (loop)
            {
                yield return SummonRoutine();
                yield return new WaitForSeconds(holdTime);
                yield return DismissRoutine();
                yield return new WaitForSeconds(closedTime);
            }
        }

        public void Summon() { if (creature != null) StartCoroutine(SummonRoutine()); }
        public void Dismiss() { if (creature != null) StartCoroutine(DismissRoutine()); }

        IEnumerator SummonRoutine()
        {
            if (portal.State == PVXState.Closed || portal.State == PVXState.Closing) portal.Open();
            yield return new WaitForSeconds(portal.openDuration * 0.6f);
            Vector3 stand = transform.TransformPoint(standOffset);
            creature.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(-transform.forward, Vector3.up));
            portal.Teleport();
            creature.Materialize(stand - Vector3.up * riseDepth);
            while (creature.IsTeleporting) yield return null;
        }

        IEnumerator DismissRoutine()
        {
            portal.Teleport();
            creature.Dematerialize();
            while (creature.IsTeleporting) yield return null;
            portal.Close();
        }
    }
}
