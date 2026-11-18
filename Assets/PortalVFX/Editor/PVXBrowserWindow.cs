using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PortalVFX.Editor
{
    /// <summary>
    /// Tools > Portal VFX > Portal Browser: find any portal by category or name and drop it into the scene,
    /// link portals for teleporting, make characters teleportable, set quality and open the demo scenes.
    /// </summary>
    public class PVXBrowserWindow : EditorWindow
    {
        const string Root = "Assets/PortalVFX";

        GameObject[] prefabs = new GameObject[0];
        string[] categories = { "All" };
        int category;
        string search = "";
        Vector2 scroll;
        PVXQualityLevel quality = PVXQualityLevel.High;
        int tab;
        bool onSurface;
        float cardWidth = 180f;
        GameObject selected, pressed;
        Vector2 pressPos;
        readonly Dictionary<string, Texture2D> thumbs = new Dictionary<string, Texture2D>();
        GUIStyle cardLabel;

        const string ThumbDir = Root + "/Demo/Thumbnails";

        Texture Thumb(GameObject p)
        {
            if (!thumbs.TryGetValue(p.name, out var t) || t == null)
            {
                t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ThumbDir}/{p.name}.png");
                if (t != null) thumbs[p.name] = t;
            }
            // Custom portals have no baked thumbnail: fall back to Unity's own preview.
            return t != null ? t : (Texture)AssetPreview.GetAssetPreview(p);
        }

        /// <summary>Thumbnail cards; drag one into the Scene view or Hierarchy, double-click to place it.</summary>
        void CardGrid(GameObject[] shown)
        {
            if (cardLabel == null)
                cardLabel = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter, wordWrap = true, fontSize = 11 };
            float avail = position.width - 24f;
            int cols = Mathf.Max(1, Mathf.FloorToInt(avail / (cardWidth + 6f)));
            float w = (avail - (cols - 1) * 6f) / cols, h = w * 9f / 16f, card = h + 30f;
            int rows = Mathf.CeilToInt(shown.Length / (float)cols);
            Rect area = GUILayoutUtility.GetRect(avail, rows * (card + 6f));
            var e = Event.current;
            bool loading = false;
            for (int i = 0; i < shown.Length; i++)
            {
                var p = shown[i];
                var r = new Rect(area.x + (i % cols) * (w + 6f), area.y + (i / cols) * (card + 6f), w, card);
                // Skip cards scrolled out of view.
                if (r.yMax < scroll.y - 10f || r.y > scroll.y + position.height) continue;
                var img = new Rect(r.x, r.y, w, h);
                if (e.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(r, selected == p ? new Color(0.24f, 0.37f, 0.6f) : new Color(0f, 0f, 0f, 0.25f));
                    var tex = Thumb(p);
                    if (tex != null) GUI.DrawTexture(new Rect(img.x + 2f, img.y + 2f, img.width - 4f, img.height - 4f), tex, ScaleMode.ScaleAndCrop);
                    else { EditorGUI.DrawRect(img, new Color(0.08f, 0.08f, 0.1f)); loading |= AssetPreview.IsLoadingAssetPreview(p.GetInstanceID()); }
                    GUI.Label(new Rect(r.x + 2f, img.yMax + 2f, w - 4f, 28f), Pretty(p.name), cardLabel);
                }
                if (!r.Contains(e.mousePosition)) continue;
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    selected = pressed = p;
                    pressPos = e.mousePosition;
                    if (e.clickCount == 2) { if (!onSurface || !PlaceOnSurface(p)) Place(p); pressed = null; }
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && pressed == p && (e.mousePosition - pressPos).sqrMagnitude > 16f)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { p };
                    DragAndDrop.paths = new[] { AssetDatabase.GetAssetPath(p) };
                    DragAndDrop.StartDrag(p.name);
                    pressed = null;
                    e.Use();
                }
                else if (e.type == EventType.ContextClick)
                {
                    selected = p;
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Place in front of the Scene camera"), false, () => Place(p));
                    menu.AddItem(new GUIContent("Place on surface (floor / wall / ceiling)"), false, () => { if (!PlaceOnSurface(p)) Place(p); });
                    menu.AddItem(new GUIContent("Show asset in Project"), false, () => EditorGUIUtility.PingObject(p));
                    menu.ShowAsContext();
                    e.Use();
                }
            }
            if (e.type == EventType.MouseUp) pressed = null;
            if (loading) Repaint();
        }

        [MenuItem("Tools/Portal VFX/Portal Browser", priority = 0)]
        public static void Open()
        {
            var w = GetWindow<PVXBrowserWindow>("Portal Browser");
            w.minSize = new Vector2(360, 420);
            w.Refresh();
        }

        void OnEnable() => Refresh();
        void OnProjectChange() => Refresh();

        void Refresh()
        {
            var folders = new[] { Root + "/Prefabs", Root + "/Extras", Root + "/Custom" }.Where(AssetDatabase.IsValidFolder).ToArray();
            prefabs = AssetDatabase.FindAssets("t:Prefab", folders)
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(Path.GetFileName)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(g => g != null && g.GetComponent<PVXPortal>() != null).ToArray();
            categories = new[] { "All" }.Concat(prefabs.Select(Category).Distinct().OrderBy(c => c)).ToArray();
            category = Mathf.Clamp(category, 0, categories.Length - 1);
            stats = null;
        }

        static string Category(GameObject g) => Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(g)));

        static string Pretty(string prefabName)
        {
            // PVX_52_ClassicFire -> 52  Classic Fire
            var parts = prefabName.Split('_');
            if (parts.Length < 3) return prefabName;
            string words = System.Text.RegularExpressions.Regex.Replace(string.Join("", parts.Skip(2)), "(?<=[a-z])(?=[A-Z])", " ");
            return parts[1] + "   " + words;
        }

        void OnGUI()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("↻", GUILayout.Width(24))) Refresh();
            }
            tab = GUILayout.Toolbar(tab, new[] { "Portals", "Performance" });
            if (tab == 1) { StatsGUI(); return; }
            category = GUILayout.SelectionGrid(category, categories.Select(c => c.Length > 3 && char.IsDigit(c[0]) ? c.Substring(3) : c).ToArray(), 4);

            var shown = prefabs.Where(p => (category == 0 || Category(p) == categories[category]) &&
                (string.IsNullOrEmpty(search) || p.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{shown.Length} portals", EditorStyles.miniLabel, GUILayout.Width(70));
                onSurface = GUILayout.Toggle(onSurface, new GUIContent("Place on surface", "Aim the Scene view at a floor, wall or ceiling (needs a collider): the portal is mounted there."), EditorStyles.miniButton);
                if (GUILayout.Button(new GUIContent("Constructor…", "Build your own portal from a base, frame, rune, trails and a destination."), EditorStyles.miniButton)) PVXConstructorWindow.Open();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Drag a card into the Scene or Hierarchy · double-click to place · right-click for more", EditorStyles.miniLabel);
                cardWidth = GUILayout.HorizontalSlider(cardWidth, 120f, 320f, GUILayout.Width(90));
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            CardGrid(shown);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Selected in scene", EditorStyles.boldLabel);
            var selPortals = Selection.gameObjects.Select(g => g.GetComponentInParent<PVXPortal>()).Where(p => p != null).Distinct().ToArray();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = selPortals.Length == 2;
                if (GUILayout.Button(new GUIContent("Link 2 portals", "Walking into one teleports you out of the other."))) Link(selPortals[0], selPortals[1]);
                GUI.enabled = selPortals.Length >= 1;
                if (GUILayout.Button(new GUIContent("Self-link", "Walk in and step back out of the same portal."))) foreach (var p in selPortals) Link(p, p);
                GUI.enabled = Selection.activeGameObject != null && selPortals.Length == 0;
                if (GUILayout.Button(new GUIContent("Make teleportable", "Adds PVXTeleportable (dissolve + pull-in) to the selected character."))) MakeTeleportable(Selection.activeGameObject);
                GUI.enabled = true;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                quality = (PVXQualityLevel)EditorGUILayout.EnumPopup(quality);
                if (GUILayout.Button("Apply to all portals in scene")) ApplyQuality(quality);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Demo scenes", EditorStyles.boldLabel);
            var scenes = new[] { ("Main Demo", "PVX_Demo_Main"), ("Browser", "PVX_Demo"), ("Playground", "PVX_Demo_Playground"), ("Features", "PVX_Demo_Features"), ("Teleport", "PVX_Demo_Teleport"),
                ("Daylight", "PVX_Demo_Day"), ("Showcase", "PVX_Demo_Showcase"), ("Dungeon", "PVX_Demo_Dungeon"), ("Builder", "PVX_Demo_Builder") };
            for (int row = 0; row < scenes.Length; row += 4)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int k = row; k < Mathf.Min(row + 4, scenes.Length); k++)
                    {
                        string path = $"{Root}/Demo/{scenes[k].Item2}.unity";
                        GUI.enabled = File.Exists(path);
                        if (GUILayout.Button(scenes[k].Item1) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
                    }
                    GUI.enabled = true;
                }
            }
            EditorGUILayout.Space(4);
        }

        void OnSelectionChange() => Repaint();

        /// <summary>Raycast from the Scene view centre; mount the portal on the floor, wall or ceiling it hits.</summary>
        static bool PlaceOnSurface(GameObject prefab)
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null) return false;
            var cam = view.camera;
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("[Portal VFX] Place on surface: nothing with a collider in the middle of the Scene view, placed normally.");
                return false;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var mode = PVXMount.Place(go.transform, hit.point, hit.normal, hit.normal.y > 0.7f ? Vector3.ProjectOnPlane(ray.direction, Vector3.up) : cam.transform.up);
            var mount = go.GetComponent<PVXMount>();
            if (mount != null) PrefabUtility.RecordPrefabInstancePropertyModifications(mount);
            Undo.RegisterCreatedObjectUndo(go, "Place " + prefab.name + " on " + mode);
            Selection.activeGameObject = go;
            return true;
        }

        // ── Performance ──

        struct Stats
        {
            public GameObject prefab;
            public int systems, maxParticles, typical, renderers, materials, lights, distortion;
        }

        Stats[] stats;
        Vector2 statsScroll;
        int statsSort;

        static Stats Measure(GameObject p)
        {
            var s = new Stats { prefab = p };
            var systems = p.GetComponentsInChildren<ParticleSystem>(true);
            s.systems = systems.Length;
            float typical = 0f;
            foreach (var ps in systems)
            {
                var main = ps.main;
                s.maxParticles += main.maxParticles;
                float life = (main.startLifetime.constantMin + main.startLifetime.constantMax) * 0.5f;
                if (main.startLifetime.mode == ParticleSystemCurveMode.Constant) life = main.startLifetime.constant;
                var em = ps.emission;
                float steady = em.rateOverTime.constantMax * life;
                if (ps.GetComponentInParent<PVXFxGroup>() is PVXFxGroup g && g.role != PVXFxRole.Loop) steady = 0f;
                typical += Mathf.Min(main.maxParticles, steady);
            }
            s.typical = Mathf.RoundToInt(typical);
            var renderers = p.GetComponentsInChildren<Renderer>(true);
            s.renderers = renderers.Length;
            s.materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().Count();
            s.lights = p.GetComponentsInChildren<Light>(true).Length;
            s.distortion = renderers.Count(r => r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "PortalVFX/Distortion");
            return s;
        }

        void StatsGUI()
        {
            if (stats == null || stats.Length != prefabs.Length) stats = prefabs.Select(Measure).ToArray();
            using (new EditorGUILayout.HorizontalScope())
            {
                statsSort = EditorGUILayout.Popup("Sort by", statsSort, new[] { "Name", "Typical particles", "Max particles", "Systems", "Materials" });
                if (GUILayout.Button("Export CSV", GUILayout.Width(90))) ExportCsv();
            }
            EditorGUILayout.HelpBox("Typical = particles alive while the portal idles (High quality). Medium keeps 70 %, Low 40 %, and Distance LOD thins them further when far away.", MessageType.None);
            IEnumerable<Stats> rows = stats;
            switch (statsSort)
            {
                case 1: rows = rows.OrderByDescending(r => r.typical); break;
                case 2: rows = rows.OrderByDescending(r => r.maxParticles); break;
                case 3: rows = rows.OrderByDescending(r => r.systems); break;
                case 4: rows = rows.OrderByDescending(r => r.materials); break;
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Portal", EditorStyles.miniBoldLabel, GUILayout.MinWidth(140));
                foreach (var h in new[] { "Typical", "Max", "Systems", "Mats", "Lights" }) GUILayout.Label(h, EditorStyles.miniBoldLabel, GUILayout.Width(52));
            }
            statsScroll = EditorGUILayout.BeginScrollView(statsScroll);
            foreach (var r in rows)
            {
                if (r.prefab == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Pretty(r.prefab.name), EditorStyles.label, GUILayout.MinWidth(140))) EditorGUIUtility.PingObject(r.prefab);
                    var old = GUI.color;
                    GUI.color = r.typical > 1500 ? new Color(1f, 0.6f, 0.5f) : r.typical > 800 ? new Color(1f, 0.9f, 0.5f) : old;
                    GUILayout.Label(r.typical.ToString(), GUILayout.Width(52));
                    GUI.color = old;
                    GUILayout.Label(r.maxParticles.ToString(), GUILayout.Width(52));
                    GUILayout.Label(r.systems.ToString(), GUILayout.Width(52));
                    GUILayout.Label(r.materials.ToString(), GUILayout.Width(52));
                    GUILayout.Label(r.lights.ToString(), GUILayout.Width(52));
                }
            }
            EditorGUILayout.EndScrollView();
            if (stats.Length > 0)
                EditorGUILayout.LabelField($"Average typical particles: {stats.Average(r => r.typical):0}   ·   heaviest: {stats.Max(r => r.typical)}", EditorStyles.miniLabel);
        }

        void ExportCsv()
        {
            string path = EditorUtility.SaveFilePanel("Export performance report", Application.dataPath, "PVX_PerformanceReport", "csv");
            if (string.IsNullOrEmpty(path)) return;
            var sb = new System.Text.StringBuilder("Portal,TypicalParticles,MaxParticles,ParticleSystems,Renderers,Materials,Lights,DistortionLayers\n");
            foreach (var r in stats.Where(r => r.prefab != null))
                sb.AppendLine($"{r.prefab.name},{r.typical},{r.maxParticles},{r.systems},{r.renderers},{r.materials},{r.lights},{r.distortion}");
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
        }

        static void Place(GameObject prefab)
        {
            var view = SceneView.lastActiveSceneView;
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            if (view != null)
            {
                var cam = view.camera.transform;
                pos = view.pivot;
                pos.y = 0f;
                Vector3 toPortal = pos - cam.position;
                toPortal.y = 0f;
                // A portal's front is -forward, so point forward away from the camera to face it.
                if (toPortal.sqrMagnitude > 0.01f) rot = Quaternion.LookRotation(toPortal.normalized, Vector3.up);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetPositionAndRotation(pos, rot);
            Undo.RegisterCreatedObjectUndo(go, "Place " + prefab.name);
            Selection.activeGameObject = go;
        }

        static void Link(PVXPortal a, PVXPortal b)
        {
            var la = a.GetComponent<PVXPortalLink>() ?? Undo.AddComponent<PVXPortalLink>(a.gameObject);
            var lb = b.GetComponent<PVXPortalLink>() ?? Undo.AddComponent<PVXPortalLink>(b.gameObject);
            Undo.RecordObjects(new Object[] { la, lb }, "Link Portals");
            la.other = lb;
            lb.other = la;
            EditorUtility.SetDirty(la);
            EditorUtility.SetDirty(lb);
        }

        static void MakeTeleportable(GameObject go)
        {
            var t = go.GetComponent<PVXTeleportable>() ?? Undo.AddComponent<PVXTeleportable>(go);
            Undo.RecordObject(t, "Make Teleportable");
            if (t.dissolveMaterial == null) t.dissolveMaterial = FindMaterial("PVX_Dissolve");
            if (t.sparkMaterial == null) t.sparkMaterial = FindMaterial("PVX_P_Spark_Add_4");
            if (go.GetComponent<Collider>() == null)
            {
                var c = Undo.AddComponent<CapsuleCollider>(go);
                c.center = new Vector3(0f, 0.9f, 0f);
                c.height = 1.8f;
                c.radius = 0.3f;
            }
            EditorUtility.SetDirty(t);
        }

        static Material FindMaterial(string name)
        {
            return AssetDatabase.FindAssets(name + " t:Material", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileNameWithoutExtension(p) == name)
                .Select(AssetDatabase.LoadAssetAtPath<Material>).FirstOrDefault();
        }

        static void ApplyQuality(PVXQualityLevel level)
        {
            if (Application.isPlaying) { PVXQuality.SetGlobal(level); return; }
            foreach (var q in Object.FindObjectsOfType<PVXQuality>())
            {
                Undo.RecordObject(q, "Set Portal Quality");
                q.overrideGlobal = true;
                q.level = level;
                PrefabUtility.RecordPrefabInstancePropertyModifications(q);
            }
        }
    }
}
