using UnityEngine;
using UnityEngine.Events;

namespace PortalVFX
{
    /// <summary>
    /// Links this portal to another: the portal surface shows a live view out of the other portal, and any
    /// PVXTeleportable that walks into the trigger is dissolved here and rebuilt in front of the other one.
    /// Put one on each portal of a pair and point them at each other, or point a portal at itself to step back out of it.
    /// </summary>
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Portal Link")]
    public class PVXPortalLink : MonoBehaviour
    {
        public PVXPortalLink other;
        [Tooltip("Surface renderer that shows the destination (its material needs _InnerTex, e.g. PortalVFX/Surface).")]
        public Renderer view;
        [Tooltip("Height of the portal centre above the pivot, used for the view camera and the trigger.")]
        public float centerHeight = 2f;
        [Tooltip("Radius of the walk-through trigger.")]
        public float triggerRadius = 1.3f;
        public bool liveView = true;
        [Range(128, 2048)] public int resolution = 512;
        [Tooltip("The live view only renders while the viewer is closer than this (saves a camera render per portal).")]
        public float liveViewDistance = 18f;
        public bool autoTeleport = true;
        [Tooltip("Rigidbodies (thrown objects) that enter fly out of the other portal with matching velocity.")]
        public bool passRigidbodies = true;
        [Tooltip("How far in front of the destination portal the traveller appears.")]
        public float exitDistance = 1.2f;
        [Tooltip("Camera whose position drives the view. Defaults to Camera.main.")]
        public Camera viewer;

        [System.Serializable] public class TravelEvent : UnityEvent<GameObject> { }
        [Tooltip("Fires with the object (character or rigidbody) that passed through this portal.")]
        public TravelEvent onTravel = new TravelEvent();

        public PVXPortal Portal { get; private set; }

        Camera viewCam;
        RenderTexture target;
        Material viewMat;

        void Awake()
        {
            Portal = GetComponent<PVXPortal>();
            if (autoTeleport && GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0f, centerHeight, 0f);
                col.radius = triggerRadius;
                // Kinematic body so trigger events fire even if the traveller has none.
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        void OnEnable()
        {
            // A portal linked to itself sends travellers back out of the same portal: nothing to look through.
            if (!liveView || view == null || other == null || other == this) return;
            target = new RenderTexture(resolution, resolution, 16) { name = name + "_View" };
            var go = new GameObject(name + "_ViewCamera") { hideFlags = HideFlags.DontSave };
            viewCam = go.AddComponent<Camera>();
            viewCam.targetTexture = target;
            viewCam.enabled = true;
            viewMat = view.material;
            viewMat.SetTexture("_InnerTex", target);
            viewMat.SetTextureScale("_InnerTex", Vector2.one);
            viewMat.SetFloat("_InnerAmount", 1f);
            viewMat.SetFloat("_InnerZoom", 1f);
            viewMat.SetFloat("_InnerParallax", 0f);
            viewMat.SetFloat("_InnerRadius", 2f);
            viewMat.SetVector("_InnerScroll", Vector4.zero);
        }

        void OnDisable()
        {
            if (viewCam != null) Destroy(viewCam.gameObject);
            if (target != null) { target.Release(); Destroy(target); }
            if (viewMat != null) Destroy(viewMat);
        }

        void LateUpdate()
        {
            if (viewCam == null || other == null) return;
            var eye = viewer != null ? viewer : Camera.main;
            if (eye == null) return;
            bool near = Vector3.Distance(eye.transform.position, transform.position) < liveViewDistance;
            viewCam.enabled = near;
            if (!near) return;
            // Mirror the viewer's offset from this portal to the far side of the other portal.
            Quaternion flip = Quaternion.Euler(0f, 180f, 0f);
            Vector3 local = transform.InverseTransformPoint(eye.transform.position);
            Quaternion localRot = Quaternion.Inverse(transform.rotation) * eye.transform.rotation;
            viewCam.transform.SetPositionAndRotation(other.transform.TransformPoint(flip * local), other.transform.rotation * flip * localRot);
            viewCam.fieldOfView = eye.fieldOfView;
            viewCam.nearClipPlane = Mathf.Max(0.05f, Vector3.Distance(viewCam.transform.position, other.transform.position + Vector3.up * centerHeight) - 0.5f);
            viewCam.clearFlags = eye.clearFlags;
            viewCam.backgroundColor = eye.backgroundColor;
        }

        void OnTriggerEnter(Collider c)
        {
            if (!autoTeleport || other == null || Portal.State == PVXState.Closed || Portal.State == PVXState.Closing) return;
            var t = c.GetComponentInParent<PVXTeleportable>();
            if (t == null)
            {
                PassRigidbody(c.attachedRigidbody);
                return;
            }
            if (!t.CanTeleport) return;
            Vector3 exit = other.transform.position + other.transform.forward * -exitDistance;
            // Stepping back out of the same portal keeps the traveller's facing, so a follow camera doesn't swing around.
            Quaternion rot = other == this ? t.transform.rotation : Quaternion.LookRotation(-other.transform.forward, Vector3.up);
            t.TeleportTo(exit, rot, Portal, other.Portal, transform.position + Vector3.up * centerHeight, other.transform.position + Vector3.up * other.centerHeight);
            onTravel.Invoke(t.gameObject);
        }

        static readonly System.Collections.Generic.Dictionary<Rigidbody, float> lastPass = new System.Collections.Generic.Dictionary<Rigidbody, float>();

        /// <summary>Thrown objects fly out of the other portal with the same speed, turned to match it.</summary>
        void PassRigidbody(Rigidbody rb)
        {
            if (!passRigidbodies || rb == null || rb.isKinematic) return;
            if (lastPass.TryGetValue(rb, out float last) && Time.time - last < 0.5f) return;
            lastPass[rb] = Time.time;
            Quaternion flip = Quaternion.Euler(0f, 180f, 0f);
            Vector3 localOffset = transform.InverseTransformPoint(rb.position) - Vector3.up * centerHeight;
            localOffset.z = 0f;
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
            Vector3 outVel = other.transform.TransformDirection(flip * localVel);
            Vector3 outPos = other.transform.TransformPoint(flip * localOffset + Vector3.up * other.centerHeight) - other.transform.forward * (other.triggerRadius + 0.4f);
            rb.position = outPos;
            rb.transform.position = outPos;
            rb.linearVelocity = outVel;
            rb.rotation = other.transform.rotation * flip * Quaternion.Inverse(transform.rotation) * rb.rotation;
            lastPass[rb] = Time.time;
            Portal.Teleport();
            onTravel.Invoke(rb.gameObject);
            if (other != this) other.Portal.Teleport();
        }
    }
}
