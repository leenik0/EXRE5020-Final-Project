using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Play-mode portal builder for the demo. Five picture cards along the bottom (base portal, frame, ground rune,
    /// trail, destination): click a card or its arrows to change that layer. A big RANDOM button, colour swatches and
    /// sliders for size, speed, brightness and hue. The combined portal is rebuilt live.
    /// Save a mix as a prefab in the editor with Tools > Portal VFX > Portal Constructor.
    /// </summary>
    [AddComponentMenu("Portal VFX/Demo/Portal Builder")]
    public class PVXDemoBuilder : MonoBehaviour
    {
        [System.Serializable]
        public class Layer
        {
            public string label;
            public GameObject[] items = new GameObject[0];
            public Texture2D[] thumbs = new Texture2D[0];
            [Tooltip("0 = none, otherwise item index + 1.")]
            public int pick;
        }

        public Layer[] layers = new Layer[0];
        [Tooltip("Where the portal is built.")]
        public Transform spot;

        static readonly Color[] Swatches =
        {
            new Color(1f, 1f, 1f), new Color(1f, 0.45f, 0.1f), new Color(1f, 0.15f, 0.2f), new Color(0.75f, 0.3f, 1f),
            new Color(0.25f, 0.5f, 1f), new Color(0.2f, 0.95f, 1f), new Color(0.3f, 1f, 0.4f), new Color(1f, 0.9f, 0.3f),
        };
        static readonly float[] SwatchHues = { 0f, 25f, -20f, -95f, -150f, 170f, 110f, 55f };

        float hue, size = 1f, speed = 1f, brightness = 1f;
        PVXPortal current;
        GUIStyle title, label, nameStyle, big, small;
        float uiScale = 1f;

        void Start() => Rebuild();

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) Randomize();
            if (Input.GetKeyDown(KeyCode.Space) && current != null) current.Teleport();
        }

        public void Randomize()
        {
            // A base portal most of the time; without one, a frame and a destination make the portal.
            bool hasBase = Random.value < 0.75f;
            Pick(0, hasBase);
            Pick(1, Random.value < (hasBase ? 0.35f : 1f));
            Pick(2, Random.value < 0.7f);
            Pick(3, Random.value < 0.6f);
            Pick(4, !hasBase);
            hue = Random.value < 0.45f ? 0f : Random.Range(-180f, 180f);
            Rebuild();
        }

        void Pick(int layer, bool on)
        {
            if (layer >= layers.Length) return;
            var l = layers[layer];
            l.pick = on && l.items.Length > 0 ? Random.Range(1, l.items.Length + 1) : 0;
        }

        void Step(int layer, int dir)
        {
            var l = layers[layer];
            int n = l.items.Length + 1;
            l.pick = (l.pick + dir + n) % n;
            Rebuild();
        }

        /// <summary>Builds the layers under an inactive holder, strips their own portal logic, then activates one portal.</summary>
        public void Rebuild()
        {
            if (current != null) Destroy(current.gameObject);
            Transform at = spot != null ? spot : transform;
            var holder = new GameObject("PortalBuild_Holder");
            holder.SetActive(false);
            GameObject root;
            var basePrefab = layers.Length > 0 && layers[0].pick > 0 ? layers[0].items[layers[0].pick - 1] : null;
            if (basePrefab != null) root = Instantiate(basePrefab, holder.transform);
            else
            {
                root = new GameObject("CustomPortal");
                root.transform.SetParent(holder.transform, false);
                root.AddComponent<PVXPortal>();
                root.AddComponent<PVXQuality>();
            }
            for (int i = 1; i < layers.Length; i++)
            {
                var l = layers[i];
                if (l.pick == 0 || l.pick > l.items.Length) continue;
                var part = Instantiate(l.items[l.pick - 1], root.transform);
                part.transform.localPosition = Vector3.zero;
                part.transform.localRotation = Quaternion.identity;
                Strip(part);
            }
            if (root.GetComponent<PVXPortalLight>() == null && root.GetComponentInChildren<Light>(true) != null) root.AddComponent<PVXPortalLight>();
            var prox = root.GetComponent<PVXProximity>();
            if (prox != null) DestroyImmediate(prox);
            current = root.GetComponent<PVXPortal>();
            current.openOnEnable = true;
            root.transform.SetParent(null, false);
            root.transform.SetPositionAndRotation(at.position, at.rotation);
            root.name = "Your Portal";
            Destroy(holder);
            ApplyLook();
            root.SetActive(true);
        }

        void ApplyLook()
        {
            if (current == null) return;
            current.hueShift = hue;
            current.speed = speed;
            current.intensity = brightness;
            current.transform.localScale = Vector3.one * size;
        }

        /// <summary>The root portal drives everything, so a layer's own portal-level components go (dependents first).</summary>
        static void Strip(GameObject part)
        {
            for (int pass = 0; pass < 4; pass++)
                foreach (var c in part.GetComponents<MonoBehaviour>())
                {
                    if (c == null || c is PVXElement || c is PVXFxGroup) continue;
                    if (c is PVXPortal && System.Array.Exists(part.GetComponents<MonoBehaviour>(), o => o != c && o != null && !(o is PVXElement) && !(o is PVXFxGroup))) continue;
                    DestroyImmediate(c);
                }
            var audio = part.GetComponent<AudioSource>();
            if (audio != null) DestroyImmediate(audio);
        }

        static string Pretty(GameObject g)
        {
            if (g == null) return "None";
            var parts = g.name.Split('_');
            string n = parts.Length >= 3 ? string.Join(" ", parts, 2, parts.Length - 2) : g.name;
            return System.Text.RegularExpressions.Regex.Replace(n, "(?<=[a-z])(?=[A-Z])", " ");
        }

        void Styles()
        {
            uiScale = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2f);
            int S(float px) => Mathf.RoundToInt(px * uiScale);
            title = new GUIStyle(GUI.skin.label) { fontSize = S(30), fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = S(15), alignment = TextAnchor.MiddleCenter };
            label.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            nameStyle = new GUIStyle(GUI.skin.label) { fontSize = S(16), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
            big = new GUIStyle(GUI.skin.button) { fontSize = S(34), fontStyle = FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { fontSize = S(15) };
            small.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
        }

        void OnGUI()
        {
            if (title == null || !Mathf.Approximately(uiScale, Mathf.Clamp(Screen.height / 1080f, 0.6f, 2f))) Styles();
            float k = uiScale, W = Screen.width, H = Screen.height;

            GUI.Label(new Rect(28 * k, 20 * k, W, 44 * k), "PORTAL BUILDER", title);
            GUI.Label(new Rect(30 * k, 62 * k, W, 26 * k), "Click a card (or its arrows) to change a layer  ·  RANDOM for a surprise  ·  R random, Space flash", small);

            // Bottom dock.
            float dockH = 300 * k, y0 = H - dockH;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, y0, W, dockH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float rightW = 420 * k, cardsW = W - rightW - 30 * k;
            float cardW = Mathf.Min(230 * k, (cardsW - 20 * k) / Mathf.Max(1, layers.Length) - 10 * k);
            float x = 20 * k;
            for (int i = 0; i < layers.Length; i++, x += cardW + 10 * k)
            {
                var l = layers[i];
                float y = y0 + 16 * k;
                GUI.Label(new Rect(x, y, cardW, 22 * k), l.label.ToUpperInvariant(), label);
                y += 26 * k;
                var img = new Rect(x, y, cardW, cardW * 9f / 16f);
                GUI.color = new Color(0.08f, 0.08f, 0.12f);
                GUI.DrawTexture(img, Texture2D.whiteTexture);
                GUI.color = Color.white;
                var tex = l.pick > 0 && l.pick <= l.thumbs.Length ? l.thumbs[l.pick - 1] : null;
                if (tex != null) GUI.DrawTexture(img, tex, ScaleMode.ScaleAndCrop);
                else GUI.Label(img, l.pick == 0 ? "None" : "", nameStyle);
                if (GUI.Button(img, GUIContent.none, GUIStyle.none)) Step(i, 1);
                y = img.yMax + 6 * k;
                GUI.Label(new Rect(x, y, cardW, 26 * k), l.pick == 0 ? "None" : Pretty(l.items[l.pick - 1]), nameStyle);
                y += 30 * k;
                float bw = (cardW - 8 * k) / 3f;
                if (GUI.Button(new Rect(x, y, bw, 34 * k), "◀")) Step(i, -1);
                if (GUI.Button(new Rect(x + bw + 4 * k, y, bw, 34 * k), "None")) { l.pick = 0; Rebuild(); }
                if (GUI.Button(new Rect(x + 2 * (bw + 4 * k), y, bw, 34 * k), "▶")) Step(i, 1);
            }

            // Right: RANDOM, colour swatches and sliders.
            float rx = W - rightW - 10 * k, ry = y0 + 16 * k;
            if (GUI.Button(new Rect(rx, ry, rightW, 74 * k), "RANDOM", big)) Randomize();
            ry += 84 * k;
            float sw = (rightW - 7 * 6 * k) / Swatches.Length;
            for (int i = 0; i < Swatches.Length; i++)
            {
                var r = new Rect(rx + i * (sw + 6 * k), ry, sw, 30 * k);
                GUI.color = Swatches[i];
                if (GUI.Button(r, GUIContent.none)) { hue = SwatchHues[i]; ApplyLook(); }
                GUI.DrawTexture(new Rect(r.x + 3 * k, r.y + 3 * k, r.width - 6 * k, r.height - 6 * k), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            ry += 40 * k;
            hue = Slider(rx, ref ry, rightW, "Hue", hue, -180f, 180f);
            size = Slider(rx, ref ry, rightW, "Size", size, 0.4f, 2f);
            speed = Slider(rx, ref ry, rightW, "Speed", speed, 0.2f, 3f);
            brightness = Slider(rx, ref ry, rightW, "Brightness", brightness, 0.2f, 2.5f);
        }

        float Slider(float x, ref float y, float w, string text, float value, float min, float max)
        {
            float k = uiScale;
            GUI.Label(new Rect(x, y - 4 * k, 110 * k, 26 * k), text, small);
            float v = GUI.HorizontalSlider(new Rect(x + 110 * k, y + 2 * k, w - 110 * k, 20 * k), value, min, max);
            y += 28 * k;
            if (!Mathf.Approximately(v, value)) { value = v; hue = text == "Hue" ? v : hue; }
            if (!Mathf.Approximately(v, value) || GUI.changed) ApplyLookDeferred();
            return v;
        }

        bool lookDirty;
        void ApplyLookDeferred() => lookDirty = true;
        void LateUpdate()
        {
            if (!lookDirty) return;
            lookDirty = false;
            ApplyLook();
        }
    }
}
