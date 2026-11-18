using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Mirror camera for wet floors: renders the scene reflected in this object's plane into a texture that
    /// PortalVFX/Mesh materials with "Wet Floor" on show in their puddles. Put it on the floor.
    /// </summary>
    [AddComponentMenu("Portal VFX/Planar Reflection (Wet Floor)")]
    public class PVXPlanarReflection : MonoBehaviour
    {
        [Range(0.1f, 1f)] public float resolutionScale = 0.5f;
        [Range(0f, 2f)] public float strength = 1f;
        public LayerMask layers = ~0;
        [Tooltip("Camera to mirror. Defaults to Camera.main.")]
        public Camera viewer;

        static readonly int TexId = Shader.PropertyToID("_PVXReflectionTex");
        static readonly int StrengthId = Shader.PropertyToID("_PVXReflectStrength");

        Camera mirror;
        RenderTexture target;
        bool rendering;

        void OnEnable()
        {
            var go = new GameObject("PVX_ReflectionCamera") { hideFlags = HideFlags.HideAndDontSave };
            mirror = go.AddComponent<Camera>();
            mirror.enabled = false;
        }

        void OnDisable()
        {
            Shader.SetGlobalFloat(StrengthId, 0f);
            if (mirror != null) Destroy(mirror.gameObject);
            if (target != null) { target.Release(); Destroy(target); }
        }

        // HDRP has its own planar reflection probes; a hand-rendered mirror camera is not supported there.
        static bool Unsupported => UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null &&
            UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline");

        void LateUpdate()
        {
            if (Unsupported) return;
            var cam = viewer != null ? viewer : Camera.main;
            if (cam == null || rendering) return;
            int w = Mathf.Max(64, (int)(cam.pixelWidth * resolutionScale)), h = Mathf.Max(64, (int)(cam.pixelHeight * resolutionScale));
            if (target == null || target.width != w || target.height != h)
            {
                if (target != null) { target.Release(); Destroy(target); }
                target = new RenderTexture(w, h, 16, RenderTextureFormat.DefaultHDR) { name = "PVX_Reflection" };
            }

            Vector3 n = transform.up, p = transform.position;
            float d = -Vector3.Dot(n, p);
            var reflect = Reflection(new Vector4(n.x, n.y, n.z, d));

            mirror.CopyFrom(cam);
            mirror.enabled = false;
            mirror.cullingMask = layers & cam.cullingMask;
            mirror.targetTexture = target;
            mirror.worldToCameraMatrix = cam.worldToCameraMatrix * reflect;
            // Oblique near plane at the floor so nothing below it leaks into the reflection.
            Vector4 clip = CameraSpacePlane(mirror, p, n);
            mirror.projectionMatrix = cam.CalculateObliqueMatrix(clip);
            mirror.transform.position = reflect.MultiplyPoint(cam.transform.position);

            rendering = true;
            // The floor must not sample the texture it is being rendered into.
            Shader.SetGlobalTexture(TexId, Texture2D.blackTexture);
            bool invert = GL.invertCulling;
            GL.invertCulling = !invert;
            mirror.Render();
            GL.invertCulling = invert;
            rendering = false;

            Shader.SetGlobalTexture(TexId, target);
            Shader.SetGlobalFloat(StrengthId, strength);
        }

        static Vector4 CameraSpacePlane(Camera c, Vector3 pos, Vector3 normal)
        {
            Matrix4x4 m = c.worldToCameraMatrix;
            Vector3 cp = m.MultiplyPoint(pos + normal * 0.02f);
            Vector3 cn = m.MultiplyVector(normal).normalized;
            return new Vector4(cn.x, cn.y, cn.z, -Vector3.Dot(cp, cn));
        }

        static Matrix4x4 Reflection(Vector4 plane)
        {
            var m = Matrix4x4.identity;
            m.m00 = 1f - 2f * plane.x * plane.x; m.m01 = -2f * plane.x * plane.y; m.m02 = -2f * plane.x * plane.z; m.m03 = -2f * plane.w * plane.x;
            m.m10 = -2f * plane.y * plane.x; m.m11 = 1f - 2f * plane.y * plane.y; m.m12 = -2f * plane.y * plane.z; m.m13 = -2f * plane.w * plane.y;
            m.m20 = -2f * plane.z * plane.x; m.m21 = -2f * plane.z * plane.y; m.m22 = 1f - 2f * plane.z * plane.z; m.m23 = -2f * plane.w * plane.z;
            return m;
        }
    }
}
