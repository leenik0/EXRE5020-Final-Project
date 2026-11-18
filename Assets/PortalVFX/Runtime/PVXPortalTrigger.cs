using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PortalVFX
{
    /// <summary>Keys collected in play, checked by PVXPortalTrigger ("needs key"). Add from gameplay or PVXKeyPickup.</summary>
    public static class PVXKeyring
    {
        static readonly HashSet<string> keys = new HashSet<string>();
        public static event System.Action<string> Changed;

        public static bool Has(string key) => string.IsNullOrEmpty(key) || keys.Contains(key);
        public static void Add(string key) { if (!string.IsNullOrEmpty(key) && keys.Add(key)) Changed?.Invoke(key); }
        public static void Remove(string key) { if (keys.Remove(key)) Changed?.Invoke(key); }
        public static void Clear() => keys.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() { keys.Clear(); Changed = null; }
    }

    /// <summary>
    /// Opens and closes a portal by rule, no code needed: when a target comes near (optionally only if it
    /// carries a key), after a delay, on a timer, or for a limited time. Events fire for locked / opened / closed.
    /// </summary>
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Portal Trigger")]
    public class PVXPortalTrigger : MonoBehaviour
    {
        public enum Mode { WhenNear, AfterDelay, Repeating, Manual }

        public Mode mode = Mode.WhenNear;
        [Header("When Near")]
        [Tooltip("Who opens it. Empty: any object tagged Player, else the main camera.")]
        public Transform target;
        public float openRadius = 6f;
        [Tooltip("Close again once the target is this far (0 = stay open).")]
        public float closeRadius = 9f;
        [Tooltip("Key the target needs (PVXKeyring). Empty = no key.")]
        public string requiredKey = "";
        [Header("Timing")]
        [Tooltip("Wait this long before opening once the rule is met.")]
        public float openDelay = 0f;
        [Tooltip("Close automatically after this many seconds open (0 = never).")]
        public float autoCloseAfter = 0f;
        [Tooltip("Repeating: seconds closed between openings.")]
        public float repeatInterval = 4f;

        [Header("Events")]
        [Tooltip("Target came near without the key.")]
        public UnityEvent onLocked = new UnityEvent();
        public UnityEvent onUnlocked = new UnityEvent();

        PVXPortal portal;
        float pending = -1f, openedAt = -1f, closedAt;
        bool wasLockedNear, unlocked;

        void Awake()
        {
            portal = GetComponent<PVXPortal>();
            if (mode != Mode.Manual) portal.openOnEnable = false;
        }

        void Start()
        {
            if (mode != Mode.Manual && portal.State != PVXState.Closed) portal.SetClosedInstant();
            if (mode == Mode.AfterDelay || mode == Mode.Repeating) pending = Time.time + openDelay;
            closedAt = Time.time;
        }

        /// <summary>Open now (respecting the delay), from a button, cutscene or UnityEvent.</summary>
        public void Trigger() => pending = Time.time + openDelay;

        Transform Target
        {
            get
            {
                if (target != null) return target;
                GameObject p = null;
                try { p = GameObject.FindWithTag("Player"); } catch (UnityException) { }
                if (p != null) return target = p.transform;
                return Camera.main != null ? Camera.main.transform : null;
            }
        }

        void Update()
        {
            bool open = portal.State == PVXState.Open || portal.State == PVXState.Opening;
            if (open && openedAt < 0f) openedAt = Time.time;
            if (!open && openedAt >= 0f) { openedAt = -1f; closedAt = Time.time; }

            switch (mode)
            {
                case Mode.WhenNear:
                {
                    var t = Target;
                    if (t == null) break;
                    float d = Vector3.Distance(t.position, transform.position);
                    bool near = d < openRadius;
                    bool hasKey = PVXKeyring.Has(requiredKey);
                    if (near && !hasKey && !wasLockedNear) onLocked.Invoke();
                    wasLockedNear = near && !hasKey;
                    if (hasKey && !unlocked && !string.IsNullOrEmpty(requiredKey)) { unlocked = true; onUnlocked.Invoke(); }
                    if (!open && near && hasKey && pending < 0f) pending = Time.time + openDelay;
                    if (!near) pending = -1f;
                    if (open && closeRadius > 0f && d > closeRadius) portal.Close();
                    break;
                }
                case Mode.Repeating:
                    if (!open && pending < 0f && Time.time - closedAt >= repeatInterval) pending = Time.time;
                    break;
            }

            if (pending >= 0f && Time.time >= pending)
            {
                pending = -1f;
                portal.Open();
            }
            if (open && autoCloseAfter > 0f && openedAt >= 0f && Time.time - openedAt >= autoCloseAfter + portal.openDuration)
                portal.Close();
        }

        void OnDrawGizmosSelected()
        {
            if (mode != Mode.WhenNear) return;
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, openRadius);
            if (closeRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0.3f, 0.4f);
                Gizmos.DrawWireSphere(transform.position, closeRadius);
            }
        }
    }
}
