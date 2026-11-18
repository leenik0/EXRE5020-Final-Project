using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Free-fly camera for the demo scenes in Play mode.
    /// Hold right mouse to look around; WASD move, Q/E down/up, Shift faster, mouse wheel changes speed.
    /// While you fly, guided cameras (PVXFeatureTour) step aside until you pick a station again.
    /// </summary>
    [AddComponentMenu("Portal VFX/Demo Fly Camera")]
    public class PVXFlyCamera : MonoBehaviour
    {
        public float speed = 5f;
        public float fastMultiplier = 3f;
        public float lookSensitivity = 2.5f;

        /// <summary>True once the user has taken control, until a script calls Release().</summary>
        public static bool Active { get; private set; }

        float yaw, pitch;

        public static void Release() => Active = false;

        void OnEnable()
        {
            Active = false;
            SyncAngles();
        }

        void SyncAngles()
        {
            var e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            bool look = Input.GetMouseButton(1);
            Vector3 move = new Vector3(
                (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) && look ? 1f : 0f),
                (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f),
                (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
            // A doubles as the demo "auto play" key, so strafing left only counts while looking with the mouse.
            float wheel = Input.mouseScrollDelta.y;
            if (look || move != Vector3.zero || wheel != 0f)
            {
                if (!Active) SyncAngles();
                Active = true;
            }
            if (!Active) return;
            if (wheel != 0f) speed = Mathf.Clamp(speed * (wheel > 0f ? 1.2f : 1f / 1.2f), 0.5f, 60f);
            if (look)
            {
                yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSensitivity, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            float s = speed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? fastMultiplier : 1f);
            transform.position += transform.TransformDirection(move) * s * Time.deltaTime;
#endif
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.LowerRight };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(Screen.width - 524, Screen.height - 30, 500, 22), "Fly: hold RMB + WASD, Q/E down/up, Shift fast, wheel speed", style);
        }
    }
}
