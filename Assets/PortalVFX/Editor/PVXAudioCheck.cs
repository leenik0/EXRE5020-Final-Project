using UnityEditor;
using UnityEngine;

namespace PortalVFX.Editor
{
    /// <summary>On entering Play, warns when portal sounds would be silent: Game view "Mute Audio" on, or no AudioListener.</summary>
    [InitializeOnLoad]
    static class PVXAudioCheck
    {
        static PVXAudioCheck() => EditorApplication.playModeStateChanged += OnPlayMode;

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || Object.FindObjectOfType<PVXPortalAudio>() == null) return;
            if (EditorUtility.audioMasterMute)
                Debug.LogWarning("[Portal VFX] Audio is muted: turn off \"Mute Audio\" in the Game view toolbar to hear the portals.");
            if (Object.FindObjectOfType<AudioListener>() == null)
                Debug.LogWarning("[Portal VFX] No AudioListener in the scene: add one to the camera to hear the portals.");
        }
    }
}
