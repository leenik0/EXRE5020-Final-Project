using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Portal sounds: an opening swell, a looping hum while open, a whoosh on teleport and a closing implosion.
    /// Follows the portal's state on its own; 3D by default so it fades with distance.
    /// </summary>
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Portal Audio")]
    public class PVXPortalAudio : MonoBehaviour
    {
        public AudioClip open;
        public AudioClip idle;
        public AudioClip teleport;
        public AudioClip close;
        [Range(0f, 1f)] public float volume = 0.6f;
        [Range(0f, 1f)] public float idleVolume = 0.2f;
        [Range(0f, 1f)] public float spatialBlend = 1f;
        public float minDistance = 2f;
        [Tooltip("Silent beyond this distance; the hum stops so far portals free their voice for near ones.")]
        public float maxDistance = 22f;
        [Tooltip("Height of the sound source above the pivot.")]
        public float centerHeight = 2f;

        PVXPortal portal;
        AudioSource oneShot, loop;
        PVXState last = PVXState.Closed;
        float loopTarget;

        void Awake()
        {
            portal = GetComponent<PVXPortal>();
            var holder = new GameObject("Audio").transform;
            holder.SetParent(transform, false);
            holder.localPosition = Vector3.up * centerHeight;
            oneShot = Make(holder.gameObject, false);
            loop = Make(holder.gameObject, true);
            loop.clip = idle;
            loop.volume = 0f;
        }

        AudioSource Make(GameObject go, bool looping)
        {
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = looping;
            s.spatialBlend = spatialBlend;
            s.minDistance = minDistance;
            // The hum is only for portals right next to you, so a room full of them does not drone all the time.
            s.maxDistance = looping ? maxDistance * 0.4f : maxDistance;
            // Linear rolloff reaches silence at maxDistance, so dozens of portals do not stack up into clipping.
            s.rolloffMode = AudioRolloffMode.Linear;
            // One-shots (open, teleport, close) win over the idle hum when Unity runs out of voices.
            s.priority = looping ? 200 : 64;
            s.dopplerLevel = 0f;
            return s;
        }

        void OnEnable()
        {
            if (portal == null) portal = GetComponent<PVXPortal>();
            portal.onTeleport.AddListener(PlayTeleport);
        }

        void OnDisable()
        {
            if (portal != null) portal.onTeleport.RemoveListener(PlayTeleport);
            if (loop != null) loop.Stop();
        }

        void PlayTeleport()
        {
            if (teleport != null && oneShot != null && InRange()) oneShot.PlayOneShot(teleport, volume);
        }

        static AudioListener listener;

        bool InRange()
        {
            if (listener == null || !listener.isActiveAndEnabled) listener = FindObjectOfType<AudioListener>();
            // A camera made from code has no listener and then nothing is heard at all: give the main camera one.
            if (listener == null && Camera.main != null) listener = Camera.main.gameObject.AddComponent<AudioListener>();
            if (listener == null || spatialBlend < 0.5f) return true;
            return (listener.transform.position - loop.transform.position).sqrMagnitude < maxDistance * maxDistance;
        }

        void Update()
        {
            var s = portal.State;
            bool near = InRange();
            bool hum = near && (listener == null || spatialBlend < 0.5f || (listener.transform.position - loop.transform.position).sqrMagnitude < loop.maxDistance * loop.maxDistance);
            if (s != last && near)
            {
                if (s == PVXState.Opening && open != null) oneShot.PlayOneShot(open, volume);
                if (s == PVXState.Closing && close != null) oneShot.PlayOneShot(close, volume);
                last = s;
            }
            if (s != last) last = s;
            loopTarget = hum && (s == PVXState.Opening || s == PVXState.Open) ? idleVolume * volume * Mathf.Clamp01(portal.OpenAmount * 1.5f) : 0f;
            if (idle != null)
            {
                if (loopTarget > 0.001f && !loop.isPlaying) { loop.time = Random.Range(0f, idle.length * 0.9f); loop.Play(); }
                loop.volume = Mathf.MoveTowards(loop.volume, loopTarget, Time.deltaTime * 0.8f);
                if (loop.volume <= 0.001f && loopTarget <= 0.001f && loop.isPlaying) loop.Stop();
            }
        }
    }
}
