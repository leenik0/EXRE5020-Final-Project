using System.Collections;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Demo browser: shows one portal prefab at a time and walks it through open, teleport and close.
    /// Left/Right = previous/next, Space = teleport, O = open/close, C = next colour preset, A = toggle auto play.
    /// </summary>
    [AddComponentMenu("Portal VFX/Demo Controller")]
    public class PVXDemoController : MonoBehaviour
    {
        public GameObject[] portals;
        public Transform spawnPoint;
        public bool autoPlay = true;
        public float idleTime = 2.5f;
        public int startIndex;

        int index;
        PVXPortal current;
        Coroutine cycle;
        GUIStyle titleStyle, hintStyle;

        void Start()
        {
            index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, portals.Length - 1));
            Show(index);
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.RightArrow)) Show(index + 1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Show(index - 1);
            if (current == null) return;
            if (Input.GetKeyDown(KeyCode.Space)) current.Teleport();
            if (Input.GetKeyDown(KeyCode.O)) current.Toggle();
            if (Input.GetKeyDown(KeyCode.C))
            {
                var presets = current.GetComponent<PVXColorPresets>();
                if (presets != null) presets.Next();
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                autoPlay = !autoPlay;
                Restart();
            }
#endif
        }

        public void Show(int i)
        {
            if (portals == null || portals.Length == 0) return;
            index = (i % portals.Length + portals.Length) % portals.Length;
            if (current != null) Destroy(current.gameObject);
            Transform at = spawnPoint != null ? spawnPoint : transform;
            var go = Instantiate(portals[index], at.position, at.rotation);
            current = go.GetComponent<PVXPortal>();
            Restart();
        }

        void Restart()
        {
            if (cycle != null) StopCoroutine(cycle);
            cycle = null;
            if (autoPlay && current != null) cycle = StartCoroutine(Cycle(current));
        }

        IEnumerator Cycle(PVXPortal p)
        {
            while (p != null)
            {
                p.Open();
                yield return new WaitForSeconds(p.openDuration + idleTime);
                p.Teleport();
                yield return new WaitForSeconds(p.teleportDuration + idleTime);
                p.Close();
                yield return new WaitForSeconds(p.closeDuration + 0.8f);
                Show(index + 1);
                yield break;
            }
        }

        void OnGUI()
        {
            if (portals == null || portals.Length == 0) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                titleStyle.normal.textColor = Color.white;
                hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            }
            string label = portals[index] != null ? portals[index].name.Replace("PVX_", "").Replace("_", " ") : "";
            GUI.Label(new Rect(24, 18, 900, 40), $"{index + 1:00} / {portals.Length:00}   {label}", titleStyle);
            GUI.Label(new Rect(24, 56, 900, 24), "← → switch    Space teleport    O open/close    C colour    A auto play: " + (autoPlay ? "on" : "off"), hintStyle);
        }
    }
}
