using UnityEngine;

namespace PortalVFX
{
    public enum PVXMountMode { Floor, Wall, Ceiling }

    /// <summary>
    /// Lets a floor-standing portal hang on a wall or ceiling: hides the parts that belong on the ground
    /// (ground glow, shockwaves, dust, runes) and, with Snap, lines the portal centre up with a surface point.
    /// Use Place() from code, or Portal Browser "Place on surface" in the editor.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Portal VFX/Mount (Floor / Wall / Ceiling)")]
    public class PVXMount : MonoBehaviour
    {
        public PVXMountMode mode = PVXMountMode.Floor;
        [Tooltip("Height of the portal centre above its pivot.")]
        public float centerHeight = 2f;
        [Tooltip("Parts shown only when standing on the floor (filled in by the build).")]
        public GameObject[] groundParts = new GameObject[0];

        void Awake() => Apply();

#if UNITY_EDITOR
        // SetActive is not allowed inside OnValidate, so defer it a tick.
        void OnValidate() => UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(); };
#endif

        public void Apply()
        {
            bool floor = mode == PVXMountMode.Floor;
            foreach (var g in groundParts)
                if (g != null && g.activeSelf != floor) g.SetActive(floor);
        }

        /// <summary>
        /// Puts the portal on a surface: floor (normal up) stands on it, wall faces out of it with its centre
        /// on the point, ceiling hangs face-down with its centre on the point. hint picks the facing on floors
        /// and the "up" direction on ceilings (e.g. the camera forward).
        /// </summary>
        public static PVXMountMode Place(Transform portal, Vector3 point, Vector3 normal, Vector3 hint)
        {
            var mount = portal.GetComponent<PVXMount>();
            float c = (mount != null ? mount.centerHeight : 2f) * portal.lossyScale.y;
            normal.Normalize();
            PVXMountMode mode;
            Vector3 flatHint = Vector3.ProjectOnPlane(hint, normal);
            if (flatHint.sqrMagnitude < 1e-4f) flatHint = Vector3.ProjectOnPlane(Vector3.forward, normal);
            if (normal.y > 0.7f)
            {
                mode = PVXMountMode.Floor;
                // Front is -forward: face against the hint so the viewer sees the front.
                portal.rotation = Quaternion.LookRotation(flatHint.normalized, Vector3.up);
                portal.position = point;
            }
            else if (normal.y < -0.7f)
            {
                mode = PVXMountMode.Ceiling;
                portal.rotation = Quaternion.LookRotation(-normal, flatHint.normalized);
                portal.position = point + normal * 0.05f - portal.up * c;
            }
            else
            {
                mode = PVXMountMode.Wall;
                portal.rotation = Quaternion.LookRotation(-normal, Vector3.up);
                portal.position = point + normal * 0.05f - portal.up * c;
            }
            if (mount != null)
            {
                mount.mode = mode;
                mount.Apply();
            }
            return mode;
        }
    }
}
