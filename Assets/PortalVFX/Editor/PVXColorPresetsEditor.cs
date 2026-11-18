using UnityEditor;
using UnityEngine;

namespace PortalVFX.Editor
{
    /// <summary>One-click buttons for the colour presets, applied live in the editor and in Play mode.</summary>
    [CustomEditor(typeof(PVXColorPresets))]
    public class PVXColorPresetsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var t = (PVXColorPresets)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Apply", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < t.presets.Count; i++)
                {
                    if (!GUILayout.Button(string.IsNullOrEmpty(t.presets[i].name) ? i.ToString() : t.presets[i].name)) continue;
                    Undo.RecordObjects(new Object[] { t, t.GetComponent<PVXPortal>() }, "Apply Colour Preset");
                    t.Apply(i);
                    EditorUtility.SetDirty(t);
                    EditorUtility.SetDirty(t.GetComponent<PVXPortal>());
                }
            }
        }
    }
}
