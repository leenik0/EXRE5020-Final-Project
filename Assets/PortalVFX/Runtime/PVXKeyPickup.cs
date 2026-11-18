using UnityEngine;
using UnityEngine.Events;

namespace PortalVFX
{
    /// <summary>
    /// A floating key: bobs and spins until the target touches it, then adds its key to PVXKeyring and
    /// shrinks away. Pairs with PVXPortalTrigger "required key".
    /// </summary>
    [AddComponentMenu("Portal VFX/Key Pickup")]
    public class PVXKeyPickup : MonoBehaviour
    {
        public string key = "RuneKey";
        [Tooltip("Who can pick it up. Empty: anything with PVXTeleportable or tagged Player.")]
        public Transform target;
        public float pickupRadius = 1.2f;
        public UnityEvent onPicked = new UnityEvent();

        Vector3 basePos, baseScale;
        float picked = -1f;

        void Start()
        {
            basePos = transform.position;
            baseScale = transform.localScale;
            if (target == null)
            {
                GameObject tagged = null;
                try { tagged = GameObject.FindWithTag("Player"); } catch (UnityException) { }
                var t = tagged != null ? tagged.GetComponent<PVXTeleportable>() : FindObjectOfType<PVXTeleportable>();
                if (t != null) target = t.transform;
            }
        }

        void Update()
        {
            if (picked >= 0f)
            {
                picked += Time.deltaTime / 0.35f;
                transform.localScale = baseScale * Mathf.Max(0f, 1f - picked) * (1f + picked * 0.6f);
                transform.position += Vector3.up * Time.deltaTime * 3f;
                if (picked >= 1f) gameObject.SetActive(false);
                return;
            }
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.15f;
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            if (target != null && Vector3.Distance(target.position + Vector3.up * 0.8f, transform.position) < pickupRadius)
            {
                picked = 0f;
                PVXKeyring.Add(key);
                onPicked.Invoke();
            }
        }
    }
}
