using System;
using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Camera tour for the features demo: glides between stations and captions each one.
    /// Left/Right or 1–9 = jump to a station, A = toggle auto tour.
    /// </summary>
    [AddComponentMenu("Portal VFX/Demo Feature Tour")]
    public class PVXFeatureTour : MonoBehaviour
    {
        [Serializable]
        public struct Station
        {
            public string title;
            [TextArea] public string description;
            public Vector3 cameraPosition;
            public Vector3 lookAt;
        }

        public Camera cam;
        public Station[] stations = new Station[0];
        public bool autoTour = true;
        [Min(1f)] public float stationTime = 12f;
        [Min(0.1f)] public float moveTime = 1.6f;

        int index;
        float timer;
        Vector3 fromPos, fromLook;
        float move = 1f;
        GUIStyle titleStyle, textStyle;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            if (stations.Length > 0) Jump(0, true);
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.RightArrow)) Jump(index + 1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Jump(index - 1);
            for (int i = 0; i < Mathf.Min(9, stations.Length); i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Jump(i);
            if (Input.GetKeyDown(KeyCode.A)) autoTour = !autoTour;
#endif
            if (stations.Length == 0 || cam == null) return;
            if (PVXFlyCamera.Active) return;
            timer += Time.deltaTime;
            if (autoTour && timer > stationTime) Jump(index + 1);

            var s = stations[index];
            move = Mathf.MoveTowards(move, 1f, Time.deltaTime / moveTime);
            float k = move * move * (3f - 2f * move);
            Vector3 pos = Vector3.Lerp(fromPos, s.cameraPosition, k);
            Vector3 look = Vector3.Lerp(fromLook, s.lookAt, k);
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
        }

        void Jump(int i, bool instant = false)
        {
            PVXFlyCamera.Release();
            index = (i % stations.Length + stations.Length) % stations.Length;
            timer = 0f;
            if (instant || cam == null)
            {
                fromPos = stations[index].cameraPosition;
                fromLook = stations[index].lookAt;
                move = 1f;
                return;
            }
            fromPos = cam.transform.position;
            fromLook = cam.transform.position + cam.transform.forward * 10f;
            move = 0f;
        }

        void OnGUI()
        {
            if (stations.Length == 0) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
                titleStyle.normal.textColor = Color.white;
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
                textStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            }
            var s = stations[index];
            GUI.Label(new Rect(24, 18, 1200, 36), $"{index + 1}/{stations.Length}   {s.title}", titleStyle);
            GUI.Label(new Rect(24, 58, 760, 120), s.description, textStyle);
            GUI.Label(new Rect(24, Screen.height - 34, 1200, 24), "← → / 1–" + stations.Length + " station    A auto tour: " + (autoTour ? "on" : "off"), textStyle);
        }
    }
}
