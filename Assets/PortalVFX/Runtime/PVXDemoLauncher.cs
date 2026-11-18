using UnityEngine;

namespace PortalVFX
{
    /// <summary>Demo helper: lobs balls at a target every few seconds (shows rigidbodies passing through linked portals).</summary>
    [AddComponentMenu("Portal VFX/Demo Launcher")]
    public class PVXDemoLauncher : MonoBehaviour
    {
        public Transform target;
        public Material ballMaterial;
        public float interval = 1.6f;
        public float flightTime = 1.1f;
        public float ballSize = 0.35f;
        public float lifetime = 8f;

        float timer;

        void Update()
        {
            if (target == null) return;
            timer += Time.deltaTime;
            if (timer < interval) return;
            timer = 0f;
            Launch(transform.position, target.position, flightTime, ballSize, ballMaterial, lifetime);
        }

        /// <summary>Spawns a physics ball that lands on the target after the given flight time.</summary>
        public static Rigidbody Launch(Vector3 from, Vector3 to, float flight, float size, Material mat, float lifetime)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.transform.position = from;
            ball.transform.localScale = Vector3.one * size;
            if (mat != null) ball.GetComponent<Renderer>().sharedMaterial = mat;
            var rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Vector3 d = to - from;
            rb.linearVelocity = d / flight - 0.5f * Physics.gravity * flight;
            Object.Destroy(ball, lifetime);
            return rb;
        }
    }
}
