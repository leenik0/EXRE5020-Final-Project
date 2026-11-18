using System.Collections;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Trailer camera: glides from portal to portal, opens each one as it arrives, slowly orbits it, fires a
    /// teleport flash, closes it and moves on. Made for recording store videos.
    /// Space pause, ← → previous / next, H hide titles, B cinematic bars.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Portal VFX/Showcase Camera")]
    public class PVXShowcase : MonoBehaviour
    {
        [System.Serializable]
        public struct Shot
        {
            public PVXPortal portal;
            public string title;
        }

        public Shot[] shots = new Shot[0];
        public float travelTime = 2.4f;
        public float holdTime = 6.5f;
        public float distance = 8f;
        public float height = 2.4f;
        public float lookHeight = 2f;
        [Tooltip("Degrees the camera sweeps around each portal while holding.")]
        public float orbitDegrees = 40f;
        public bool showTitles = true;
        public bool cinematicBars = true;

        public int Current { get; private set; } = -1;

        bool paused;
        int jump;
        float titleAlpha;
        GUIStyle titleStyle, subStyle;

        IEnumerator Start()
        {
            foreach (var s in shots) if (s.portal != null) s.portal.SetClosedInstant();
            if (shots.Length == 0) yield break;
            int i = 0;
            while (true)
            {
                yield return Visit(i);
                i = (i + (jump != 0 ? jump : 1) + shots.Length) % shots.Length;
                jump = 0;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.RightArrow)) jump = 1;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) jump = -1;
            if (Input.GetKeyDown(KeyCode.H)) showTitles = !showTitles;
            if (Input.GetKeyDown(KeyCode.B)) cinematicBars = !cinematicBars;
        }

        Vector3 OrbitPoint(PVXPortal p, float angle)
        {
            float s = p.transform.lossyScale.y;
            // Front of a portal is -forward.
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * -p.transform.forward;
            return p.transform.position + dir * distance * s + Vector3.up * height * s;
        }

        Vector3 Focus(PVXPortal p) => p.transform.position + Vector3.up * lookHeight * p.transform.lossyScale.y;

        IEnumerator Visit(int i)
        {
            var p = shots[i].portal;
            if (p == null) yield break;
            Current = i;
            float a0 = -orbitDegrees * 0.5f, a1 = orbitDegrees * 0.5f;

            // Glide in on a gentle arc.
            Vector3 fromPos = transform.position;
            Quaternion fromRot = transform.rotation;
            Vector3 toPos = OrbitPoint(p, a0);
            Quaternion toRot = Quaternion.LookRotation(Focus(p) - toPos);
            bool opened = false;
            for (float k = 0f; k < 1f; k += Time.deltaTime / travelTime)
            {
                if (jump != 0) break;
                float e = Mathf.SmoothStep(0f, 1f, k);
                Vector3 pos = Vector3.Lerp(fromPos, toPos, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 1.5f;
                transform.SetPositionAndRotation(pos, Quaternion.Slerp(fromRot, toRot, e));
                if (!opened && k > 0.6f) { p.Open(); opened = true; }
                titleAlpha = Mathf.MoveTowards(titleAlpha, 0f, Time.deltaTime * 3f);
                yield return null;
            }
            if (!opened) p.Open();

            // Slow orbit while the portal idles; one teleport flash in the middle.
            bool flashed = false;
            for (float t = 0f; t < holdTime; t += paused ? 0f : Time.deltaTime)
            {
                if (jump != 0) break;
                float e = Mathf.SmoothStep(0f, 1f, t / holdTime);
                Vector3 pos = OrbitPoint(p, Mathf.Lerp(a0, a1, e));
                transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Focus(p) - pos));
                if (!flashed && t > holdTime * 0.55f) { p.Teleport(); flashed = true; }
                titleAlpha = Mathf.MoveTowards(titleAlpha, t < holdTime - 0.8f ? 1f : 0f, Time.deltaTime * 2f);
                yield return null;
            }
            p.Close();
        }

        void OnGUI()
        {
            float h = Screen.height;
            if (cinematicBars)
            {
                float bar = h * 0.09f;
                var black = Texture2D.blackTexture;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, bar), black);
                GUI.DrawTexture(new Rect(0f, h - bar, Screen.width, bar), black);
            }
            if (!showTitles || Current < 0 || titleAlpha <= 0.01f) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(h * 0.045f), fontStyle = FontStyle.Bold };
                subStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(h * 0.022f) };
            }
            var shot = shots[Current];
            string title = string.IsNullOrEmpty(shot.title) && shot.portal != null ? shot.portal.name : shot.title;
            GUI.color = new Color(1f, 1f, 1f, titleAlpha);
            GUI.Label(new Rect(h * 0.08f, h * 0.72f, Screen.width, h * 0.07f), title, titleStyle);
            GUI.color = new Color(1f, 1f, 1f, titleAlpha * 0.7f);
            GUI.Label(new Rect(h * 0.08f, h * 0.79f, Screen.width, h * 0.05f), $"{Current + 1} / {shots.Length}   ·   Space pause   ← → switch   H titles   B bars", subStyle);
            GUI.color = Color.white;
        }
    }
}
