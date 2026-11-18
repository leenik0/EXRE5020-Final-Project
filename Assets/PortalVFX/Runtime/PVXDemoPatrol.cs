using UnityEngine;

namespace PortalVFX
{
    /// <summary>Demo helper: walks back and forth between two points (used to show the proximity reaction).</summary>
    [AddComponentMenu("Portal VFX/Demo Patrol")]
    public class PVXDemoPatrol : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 1.4f;
        public float pause = 1f;

        bool toB = true;
        float wait;

        void Update()
        {
            if (wait > 0f) { wait -= Time.deltaTime; return; }
            Vector3 target = toB ? pointB : pointA;
            Vector3 d = target - transform.position;
            d.y = 0f;
            if (d.magnitude < 0.05f)
            {
                toB = !toB;
                wait = pause;
                return;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d, Vector3.up), Time.deltaTime * 6f);
            transform.position += d.normalized * Mathf.Min(speed * Time.deltaTime, d.magnitude);
        }
    }
}
