using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Demo walker for the teleport scene: walks forward, turns around after a few steps and walks back,
    /// so it keeps passing through a linked portal pair and shows the dissolve both ways.
    /// </summary>
    [RequireComponent(typeof(PVXTeleportable))]
    [AddComponentMenu("Portal VFX/Demo Traveler")]
    public class PVXDemoTraveler : MonoBehaviour
    {
        public float speed = 1.6f;
        [Tooltip("Distance walked away from a portal before turning back toward it.")]
        public float walkAway = 2.6f;
        public float turnSpeed = 220f;

        PVXTeleportable teleportable;
        Vector3 lastPos;
        float walked;
        float turnLeft;

        void Awake()
        {
            teleportable = GetComponent<PVXTeleportable>();
            lastPos = transform.position;
        }

        void Update()
        {
            if (teleportable.IsTeleporting)
            {
                lastPos = transform.position;
                walked = 0f;
                return;
            }
            // Just arrived from a portal: walk away from it, then turn back toward it.
            if ((transform.position - lastPos).sqrMagnitude > 1f) walked = 0f;

            if (turnLeft > 0f)
            {
                float step = Mathf.Min(turnLeft, turnSpeed * Time.deltaTime);
                transform.Rotate(0f, step, 0f);
                turnLeft -= step;
            }
            else
            {
                transform.position += transform.forward * speed * Time.deltaTime;
                walked += speed * Time.deltaTime;
                if (walked >= walkAway && Time.time - teleportable.LastTeleportTime < 30f)
                {
                    turnLeft = 180f;
                    walked = -100f; // walk all the way back into the portal
                }
            }
            lastPos = transform.position;
        }
    }
}
