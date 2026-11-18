using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Demo guide for a scene split into zones: shows the title and a short explanation of the zone the
    /// player is in, and number keys 1-9 teleport the player straight to a zone (dissolve and rebuild).
    /// </summary>
    [AddComponentMenu("Portal VFX/Demo/Zones Guide")]
    public class PVXDemoZones : MonoBehaviour
    {
        [System.Serializable]
        public struct Zone
        {
            public string title;
            [TextArea] public string description;
            public Vector3 center;
            public float radius;
            [Tooltip("Where number-key travel drops the player.")]
            public Vector3 spawn;
            public float spawnYaw;
        }

        public PVXTeleportable player;
        public Zone[] zones = new Zone[0];

        int current = -1;
        float alpha;
        GUIStyle title, body, keys;

        void Update()
        {
            if (player == null) return;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < zones.Length; i++)
            {
                Vector3 d = player.transform.position - zones[i].center;
                d.y = 0f;
                float m = d.magnitude;
                if (m < zones[i].radius && m < bestD) { best = i; bestD = m; }
            }
            if (best != current) { alpha = 0f; current = best; }
            alpha = Mathf.MoveTowards(alpha, current >= 0 ? 1f : 0f, Time.deltaTime * 2f);

            for (int i = 0; i < zones.Length && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) && player.CanTeleport)
                    player.TeleportTo(zones[i].spawn, Quaternion.Euler(0f, zones[i].spawnYaw, 0f));
        }

        void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, wordWrap = true };
                body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
                keys = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                keys.normal.textColor = new Color(1f, 1f, 1f, 0.65f);
            }
            float x = 24f, w = Mathf.Min(560f, Screen.width - 48f);
            var travel = new System.Text.StringBuilder("Travel: ");
            for (int i = 0; i < zones.Length && i < 9; i++) travel.Append($"{i + 1} {zones[i].title}    ");
            GUI.Label(new Rect(x, 70f, Screen.width - 48f, 22f), travel.ToString(), keys);
            if (current < 0 || alpha <= 0.01f) return;
            var z = zones[current];
            float y = Screen.height - 150f;
            GUI.color = new Color(0f, 0f, 0f, 0.45f * alpha);
            GUI.DrawTexture(new Rect(x - 12f, y - 10f, w + 24f, 130f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(x, y, w, 36f), z.title, title);
            GUI.Label(new Rect(x, y + 38f, w, 80f), z.description, body);
            GUI.color = Color.white;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.5f);
            foreach (var z in zones) Gizmos.DrawWireSphere(z.center, z.radius);
        }
    }
}
