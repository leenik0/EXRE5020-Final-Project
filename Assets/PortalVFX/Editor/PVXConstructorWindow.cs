using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PortalVFX.Editor
{
    /// <summary>
    /// Tools > Portal VFX > Portal Constructor: build your own portal from layers — a base portal, a frame,
    /// a ground rune, up to two trails and a destination reveal — recolour and resize it, preview it in the
    /// scene and save it as a prefab in Assets/PortalVFX/Custom.
    /// </summary>
    public class PVXConstructorWindow : EditorWindow
    {
        const string Root = "Assets/PortalVFX";
        const string PreviewName = "PVX_ConstructorPreview";

        static readonly string[] LayerNames = { "Base portal", "Frame", "Ground rune", "Trail A", "Trail B", "Destination" };
        static readonly string[] LayerFolders = { "Prefabs", "Extras/Frames", "Extras/GroundRunes", "Extras/Trails", "Extras/Trails", "Extras/Reveals" };

        GameObject[][] options;
        string[][] labels;
        readonly int[] pick = new int[6];
        string portalName = "MyPortal";
        Color tint = Color.white;
        float hue, intensity = 1f, size = 1f;
        Vector2 scroll;
        bool livePreview = true;

        static Texture Thumb(GameObject p) => p == null ? null :
            (Texture)AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Editor/Thumbnails/{p.name}.png") ?? AssetPreview.GetAssetPreview(p);

        /// <summary>One layer: its picture and name; click to pick another from a grid of pictures.</summary>
        void LayerRow(int layer)
        {
            var p = Picked(layer);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var r = GUILayoutUtility.GetRect(96f, 54f, GUILayout.Width(96f), GUILayout.Height(54f));
                var tex = Thumb(p);
                if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleAndCrop);
                else EditorGUI.DrawRect(r, new Color(0.1f, 0.1f, 0.12f));
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(LayerNames[layer], EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField(labels[layer][pick[layer]], EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var btn = GUILayoutUtility.GetRect(new GUIContent("Choose…"), EditorStyles.miniButton);
                        if (GUI.Button(btn, "Choose…", EditorStyles.miniButton))
                            PopupWindow.Show(btn, new Picker(options[layer], pick[layer], i => { pick[layer] = i; if (livePreview) Preview(); Repaint(); }));
                        if (GUILayout.Button("◀", EditorStyles.miniButton, GUILayout.Width(24))) { pick[layer] = (pick[layer] + labels[layer].Length - 1) % labels[layer].Length; GUI.changed = true; }
                        if (GUILayout.Button("▶", EditorStyles.miniButton, GUILayout.Width(24))) { pick[layer] = (pick[layer] + 1) % labels[layer].Length; GUI.changed = true; }
                        if (GUILayout.Button("None", EditorStyles.miniButton, GUILayout.Width(44))) { pick[layer] = 0; GUI.changed = true; }
                    }
                }
            }
        }

        /// <summary>Grid of thumbnails to pick a layer from.</summary>
        class Picker : PopupWindowContent
        {
            readonly GameObject[] items;
            readonly int selected;
            readonly System.Action<int> onPick;
            Vector2 scroll;

            public Picker(GameObject[] items, int selected, System.Action<int> onPick) { this.items = items; this.selected = selected; this.onPick = onPick; }

            public override Vector2 GetWindowSize() => new Vector2(4 * 132f + 20f, 420f);

            public override void OnGUI(Rect rect)
            {
                scroll = GUILayout.BeginScrollView(scroll);
                int cols = 4;
                var area = GUILayoutUtility.GetRect(cols * 132f, Mathf.CeilToInt((items.Length + 1) / (float)cols) * 96f);
                for (int i = 0; i <= items.Length; i++)
                {
                    var r = new Rect(area.x + (i % cols) * 132f, area.y + (i / cols) * 96f, 128f, 92f);
                    var img = new Rect(r.x, r.y, 128f, 72f);
                    EditorGUI.DrawRect(r, i == selected ? new Color(0.24f, 0.37f, 0.6f) : new Color(0f, 0f, 0f, 0.3f));
                    var tex = i == 0 ? null : Thumb(items[i - 1]);
                    if (tex != null) GUI.DrawTexture(img, tex, ScaleMode.ScaleAndCrop);
                    string name = i == 0 ? "None" : items[i - 1].name.Replace("PVX_", "").Replace('_', ' ');
                    GUI.Label(new Rect(r.x, img.yMax, 128f, 20f), name, EditorStyles.centeredGreyMiniLabel);
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { onPick(i); editorWindow.Close(); }
                }
                GUILayout.EndScrollView();
            }
        }

        [MenuItem("Tools/Portal VFX/Portal Constructor", priority = 1)]
        public static void Open()
        {
            var w = GetWindow<PVXConstructorWindow>("Portal Constructor");
            w.minSize = new Vector2(340, 380);
            w.Load();
        }

        void OnEnable() => Load();
        void OnProjectChange() => Load();

        void Load()
        {
            options = new GameObject[6][];
            labels = new string[6][];
            for (int i = 0; i < 6; i++)
            {
                options[i] = AssetDatabase.FindAssets("t:Prefab", new[] { $"{Root}/{LayerFolders[i]}" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(Path.GetFileName)
                    .Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(g => g != null && g.GetComponent<PVXPortal>() != null).ToArray();
                labels[i] = new[] { "None" }.Concat(options[i].Select(g => g.name.Replace("PVX_", "").Replace('_', ' '))).ToArray();
                pick[i] = Mathf.Clamp(pick[i], 0, labels[i].Length - 1);
            }
        }

        GameObject Picked(int layer) => pick[layer] > 0 ? options[layer][pick[layer] - 1] : null;

        void OnGUI()
        {
            if (options == null) Load();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("1. Click a layer to choose it from pictures.   2. The portal updates in the scene.   3. Save as prefab.", MessageType.None);
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < 6; i++) LayerRow(i);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Look", EditorStyles.boldLabel);
            tint = EditorGUILayout.ColorField(new GUIContent("Tint"), tint, true, false, true);
            hue = EditorGUILayout.Slider("Hue shift", hue, -180f, 180f);
            intensity = EditorGUILayout.Slider("Brightness", intensity, 0f, 3f);
            size = EditorGUILayout.Slider("Size", size, 0.2f, 3f);
            if (EditorGUI.EndChangeCheck() && livePreview) Preview();

            EditorGUILayout.Space(6);
            portalName = EditorGUILayout.TextField("Name", portalName);
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                livePreview = GUILayout.Toggle(livePreview, new GUIContent("Live preview", "Rebuild the portal in the scene after every change."), EditorStyles.miniButton, GUILayout.Width(90));
                if (GUILayout.Button("Randomize")) Randomize();
                if (GUILayout.Button("Preview in scene")) Preview();
            }
            GUI.enabled = pick.Any(p => p > 0);
            if (GUILayout.Button("Save as prefab", GUILayout.Height(28))) Save();
            GUI.enabled = true;
            EditorGUILayout.HelpBox("Every layer opens, idles and closes together as one portal. Press Play to see it animate; " +
                "saved prefabs go to Assets/PortalVFX/Custom and show up in the Portal Browser.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        void Randomize()
        {
            pick[0] = Random.value < 0.75f ? Random.Range(1, labels[0].Length) : 0;
            pick[1] = Random.value < (pick[0] == 0 ? 0.9f : 0.35f) ? Random.Range(1, labels[1].Length) : 0;
            pick[2] = Random.value < 0.6f ? Random.Range(1, labels[2].Length) : 0;
            pick[3] = Random.value < 0.6f ? Random.Range(1, labels[3].Length) : 0;
            pick[4] = 0;
            pick[5] = pick[0] == 0 ? Random.Range(1, labels[5].Length) : 0;
            hue = Random.value < 0.5f ? 0f : Random.Range(-180f, 180f);
            Preview();
        }

        GameObject Compose()
        {
            GameObject root;
            var basePrefab = Picked(0);
            if (basePrefab != null)
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                root = new GameObject();
                root.AddComponent<PVXPortal>();
                root.AddComponent<PVXQuality>();
                root.AddComponent<PVXColorPresets>().presets = PVXColorPresets.Defaults();
            }
            root.name = "PVX_Custom_" + Sanitize(portalName);

            var mount = root.GetComponent<PVXMount>();
            for (int i = 1; i < 6; i++)
            {
                var prefab = Picked(i);
                if (prefab == null) continue;
                var layer = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                PrefabUtility.UnpackPrefabInstance(layer, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                layer.name = LayerNames[i].Replace(" ", "");
                layer.transform.SetParent(root.transform, false);
                layer.transform.localPosition = Vector3.zero;
                layer.transform.localRotation = Quaternion.identity;
                StripPortalComponents(layer);
                if (i == 2 && mount != null) mount.groundParts = mount.groundParts.Append(layer).ToArray();
            }

            if (root.GetComponent<PVXPortalLight>() == null && root.GetComponentInChildren<Light>(true) != null)
                root.AddComponent<PVXPortalLight>().rangeScale = 1.3f;
            var portal = root.GetComponent<PVXPortal>();
            portal.tint = tint;
            portal.hueShift = hue;
            portal.intensity = intensity;
            root.transform.localScale = Vector3.one * size;
            return root;
        }

        /// <summary>A layer is driven by the root portal, so its own portal-level components go.</summary>
        static void StripPortalComponents(GameObject layer)
        {
            // Several passes: components that [RequireComponent(PVXPortal)] must go before PVXPortal itself.
            for (int pass = 0; pass < 4; pass++)
            {
                foreach (var c in layer.GetComponents<MonoBehaviour>())
                {
                    if (c == null || c is PVXElement || c is PVXFxGroup) continue;
                    if (c.GetType().Namespace != "PortalVFX") continue;
                    if (c is PVXPortal && layer.GetComponents<MonoBehaviour>().Any(o => o != c && o != null && !(o is PVXElement) && !(o is PVXFxGroup) && o.GetType().Namespace == "PortalVFX")) continue;
                    DestroyImmediate(c);
                }
            }
            var audio = layer.GetComponent<AudioSource>();
            if (audio != null) DestroyImmediate(audio);
        }

        void Preview()
        {
            var old = GameObject.Find(PreviewName);
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            if (old != null) { pos = old.transform.position; rot = old.transform.rotation; Undo.DestroyObjectImmediate(old); }
            else if (SceneView.lastActiveSceneView != null)
            {
                var view = SceneView.lastActiveSceneView;
                pos = view.pivot;
                pos.y = 0f;
                Vector3 dir = pos - view.camera.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
            var go = Compose();
            go.name = PreviewName;
            go.transform.SetPositionAndRotation(pos, rot);
            Undo.RegisterCreatedObjectUndo(go, "Portal Constructor Preview");
            Selection.activeGameObject = go;
        }

        void Save()
        {
            string dir = Root + "/Custom";
            Directory.CreateDirectory(dir);
            var go = Compose();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{go.name}.prefab");
            go.transform.localScale = Vector3.one;
            var portal = go.GetComponent<PVXPortal>();
            portal.size = 1f;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
            // Size goes on the prefab through the portal's Size setting, so it stays editable.
            if (!Mathf.Approximately(size, 1f))
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                contents.transform.localScale = Vector3.one * size;
                var so = new SerializedObject(contents.GetComponent<PVXPortal>());
                so.FindProperty("size").floatValue = size;
                so.FindProperty("appliedSize").floatValue = size;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[Portal VFX] Saved custom portal to {path}");
        }

        static string Sanitize(string s)
        {
            var clean = new string((s ?? "").Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
            return string.IsNullOrEmpty(clean) ? "Portal" : clean;
        }
    }
}
