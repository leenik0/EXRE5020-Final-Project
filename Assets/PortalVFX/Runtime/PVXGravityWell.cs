using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Pulls nearby rigidbodies into an open portal: they spiral in, shrink and vanish at the centre.
    /// With respawn on (handy for demos) swallowed objects reappear where they started after a delay.
    /// </summary>
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Gravity Well")]
    public class PVXGravityWell : MonoBehaviour
    {
        public float centerHeight = 2.2f;
        public float radius = 9f;
        public float strength = 30f;
        [Range(0f, 1f)] public float swirl = 0.45f;
        public float swallowRadius = 1.2f;
        public LayerMask layers = ~0;
        public bool respawn = true;
        public float respawnDelay = 2.5f;

        PVXPortal portal;
        readonly Collider[] hits = new Collider[64];
        readonly HashSet<Rigidbody> swallowing = new HashSet<Rigidbody>();
        readonly Dictionary<Rigidbody, (Vector3 pos, Quaternion rot, Vector3 scale)> origins = new Dictionary<Rigidbody, (Vector3, Quaternion, Vector3)>();

        Vector3 Center => transform.position + transform.up * centerHeight;

        void Awake() => portal = GetComponent<PVXPortal>();

        void FixedUpdate()
        {
            if (portal.State != PVXState.Open && portal.State != PVXState.Opening) return;
            float power = portal.OpenAmount;
            int n = Physics.OverlapSphereNonAlloc(Center, radius, hits, layers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var rb = hits[i].attachedRigidbody;
                if (rb == null || rb.isKinematic || swallowing.Contains(rb)) continue;
                if (!origins.ContainsKey(rb)) origins[rb] = (rb.position, rb.rotation, rb.transform.localScale);
                Vector3 to = Center - rb.worldCenterOfMass;
                float d = to.magnitude;
                if (d < swallowRadius) { StartCoroutine(Swallow(rb)); continue; }
                Vector3 dir = to / d;
                float falloff = 1f - Mathf.Clamp01(d / radius);
                Vector3 tangent = Vector3.Cross(transform.forward, dir).normalized;
                Vector3 force = (dir * (1f - swirl) + tangent * swirl) * strength * (0.35f + 0.65f * falloff) * power;
                // Lift so things leave the ground and fly up into the portal.
                force += Vector3.up * -Physics.gravity.y * (0.7f + 0.6f * falloff) * power;
                // Drag so objects spiral in instead of orbiting forever.
                force -= rb.linearVelocity * (0.8f + 1.5f * falloff);
                rb.AddForce(force, ForceMode.Acceleration);
                rb.AddTorque(Random.insideUnitSphere * strength * 0.2f * falloff, ForceMode.Acceleration);
            }
        }

        IEnumerator Swallow(Rigidbody rb)
        {
            swallowing.Add(rb);
            portal.Teleport();
            rb.isKinematic = true;
            var t = rb.transform;
            Vector3 start = t.position, scale = t.localScale;
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.35f)
            {
                if (t == null) yield break;
                t.position = Vector3.Lerp(start, Center, k * k);
                t.localScale = scale * (1f - k);
                t.Rotate(0f, 0f, 720f * Time.deltaTime, Space.Self);
                yield return null;
            }
            t.gameObject.SetActive(false);
            if (respawn && origins.TryGetValue(rb, out var o))
            {
                yield return new WaitForSeconds(respawnDelay);
                if (t == null) yield break;
                t.SetPositionAndRotation(o.pos, o.rot);
                t.localScale = o.scale;
                t.gameObject.SetActive(true);
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            swallowing.Remove(rb);
        }
    }
}
