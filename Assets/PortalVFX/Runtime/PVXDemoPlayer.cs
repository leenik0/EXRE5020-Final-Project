using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Playable demo character with a modern third-person orbit camera.
    /// Click to capture the mouse (Esc releases): mouse orbits the camera, wheel zooms,
    /// WASD moves relative to the camera and the character turns toward where it walks, Shift runs.
    /// E = replay the nearest portal's opening, C = next colour preset, Space = flash it. Walk into a linked portal to teleport.
    /// </summary>
    [RequireComponent(typeof(PVXTeleportable))]
    [AddComponentMenu("Portal VFX/Demo Player")]
    public class PVXDemoPlayer : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 3.2f;
        public float runMultiplier = 2f;
        public float acceleration = 12f;
        public float turnSmoothing = 12f;

        [Header("Camera")]
        public Camera followCamera;
        public float distance = 5.5f;
        public Vector2 distanceRange = new Vector2(2.5f, 12f);
        public float pivotHeight = 1.3f;
        public float shoulderOffset = 0.35f;
        public Vector2 pitchRange = new Vector2(-15f, 65f);
        public float mouseSensitivity = 2.2f;
        public float cameraSmoothing = 14f;

        [Header("Interaction")]
        [Tooltip("How close a portal must be for C / Space to affect it.")]
        public float interactRange = 7f;
        [Tooltip("F throws a ball with this material (try throwing it into a portal).")]
        public Material throwMaterial;

        /// <summary>Scripted input (x = strafe, y = forward, camera-relative) used by tests and cutscenes.</summary>
        public Vector2 scriptedInput;

        /// <summary>Kept for older scenes; the camera offset is now driven by distance / pivotHeight.</summary>
        [HideInInspector] public Vector3 cameraOffset = new Vector3(0f, 3.4f, -6.5f);

        PVXTeleportable teleportable;
        PVXPortal[] portals;
        GUIStyle hint;
        float yaw, pitch = 18f, zoom;
        Vector3 velocity;
        Vector3 camPos;

        void Awake()
        {
            teleportable = GetComponent<PVXTeleportable>();
            portals = FindObjectsOfType<PVXPortal>();
            yaw = transform.eulerAngles.y;
            zoom = distance;
        }

        void Start()
        {
            if (followCamera != null) camPos = CameraTarget(out _);
        }

        void Update()
        {
            Vector2 input = scriptedInput;
            bool run = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, pitchRange.x, pitchRange.y);
            }
            zoom = Mathf.Clamp(zoom - Input.mouseScrollDelta.y * 0.6f, distanceRange.x, distanceRange.y);
            input.x += (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            input.y += (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.C))
            {
                var p = Nearest();
                var presets = p != null ? p.GetComponent<PVXColorPresets>() : null;
                if (presets != null) presets.Next();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Replay the whole opening: snap shut, then build up again.
                var p = Nearest();
                if (p != null) { p.SetClosedInstant(); p.Open(); }
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                Vector3 aim = Quaternion.Euler(Mathf.Min(pitch, 20f) - 12f, yaw, 0f) * Vector3.forward;
                var ball = PVXDemoLauncher.Launch(transform.position + Vector3.up * 1.2f + transform.forward * 0.5f, transform.position, 1f, 0.3f, throwMaterial, 10f);
                ball.velocity = aim * 13f;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var p = Nearest();
                if (p != null) p.Teleport();
            }
#endif
            if (teleportable.IsTeleporting) { velocity = Vector3.zero; return; }

            // Camera-relative movement; the character turns to face where it walks.
            Quaternion camYaw = Quaternion.Euler(0f, yaw, 0f);
            Vector3 wish = camYaw * new Vector3(input.x, 0f, input.y);
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            Vector3 target = wish * speed * (run ? runMultiplier : 1f);
            velocity = Vector3.MoveTowards(velocity, target, acceleration * Time.deltaTime);
            Vector3 step = velocity * Time.deltaTime;
            // Slide along walls and pillars (solid colliders only; portal triggers and the floor are ignored).
            if (step.sqrMagnitude > 1e-6f && Physics.SphereCast(transform.position + Vector3.up * 0.8f, 0.3f, step.normalized, out var hit, step.magnitude + 0.05f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.attachedRigidbody == null && hit.normal.y < 0.5f)
            {
                Vector3 n = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                step -= n * Vector3.Dot(step, n);
                velocity -= n * Vector3.Dot(velocity, n);
            }
            transform.position += step;
            if (wish.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(wish, Vector3.up), 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));
        }

        Vector3 CameraTarget(out Vector3 pivot)
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            pivot = transform.position + Vector3.up * pivotHeight + rot * Vector3.right * shoulderOffset;
            return pivot - rot * Vector3.forward * zoom;
        }

        void LateUpdate()
        {
            if (followCamera == null) return;
            Vector3 target = CameraTarget(out Vector3 pivot);
            // Glide normally; snap if the character jumped far (e.g. teleported to another place).
            float k = (camPos - target).sqrMagnitude > 400f ? 1f : 1f - Mathf.Exp(-cameraSmoothing * Time.deltaTime);
            camPos = Vector3.Lerp(camPos, target, k);
            if (camPos.y < 0.3f) camPos.y = 0.3f;
            followCamera.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(pivot - camPos, Vector3.up));
        }

        PVXPortal Nearest()
        {
            PVXPortal best = null;
            float bestD = interactRange;
            foreach (var p in portals)
            {
                if (p == null) continue;
                float d = Vector3.Distance(p.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        void OnGUI()
        {
            if (hint == null)
            {
                hint = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                hint.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            }
            var near = Nearest();
            GUI.Label(new Rect(24, 18, 1400, 24),
                "Click: capture mouse (Esc release)   Mouse: look   Wheel: zoom   WASD: move   Shift: run   E: re-open portal   F: throw ball   C: colour   Space: flash   — walk into a portal to teleport", hint);
            if (near != null) GUI.Label(new Rect(24, 44, 900, 24), "Nearest: " + near.name, hint);
        }
    }
}
