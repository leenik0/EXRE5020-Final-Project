using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Procedural animation for the demo kitty: a bouncy hop-walk (legs, arms, squash and stretch), idle breathing
    /// and head sway, blinking, ear twitches, a swaying tail and scarf ends that trail behind, and a happy jump with
    /// "^^" eyes right after it rebuilds from a teleport. Looks for children by name (Visual, LegL/R, ArmL/R,
    /// HeadPivot, EyeL/R, HappyL/R, EarL/R, Tail, ScarfL/R); missing parts are skipped.
    /// </summary>
    [AddComponentMenu("Portal VFX/Demo Walk Anim")]
    public class PVXDemoWalkAnim : MonoBehaviour
    {
        public float stride = 4.5f;
        public float legSwing = 38f;
        public float armSwing = 32f;
        public float bob = 0.035f;
        [Tooltip("Height of the happy jump after a teleport.")]
        public float cheerJump = 0.22f;

        Transform visual, legL, legR, armL, armR, head, eyeL, eyeR, happyL, happyR, earL, earR, tail, scarfL, scarfR;
        Quaternion armLRest, armRRest, earLRest, earRRest, tailRest, scarfLRest, scarfRRest;
        Vector3 eyeScale = Vector3.one, lastPos;
        float speed, phase, nextBlink, blink = -1f, cheer = -100f, earTwitch = -1f, nextTwitch, twitchSide, swing, swingVel;
        PVXTeleportable teleportable;

        void Awake()
        {
            visual = Find("Visual");
            legL = Find("LegL"); legR = Find("LegR");
            armL = Find("ArmL"); armR = Find("ArmR");
            head = Find("HeadPivot");
            eyeL = Find("EyeL"); eyeR = Find("EyeR");
            happyL = Find("HappyL"); happyR = Find("HappyR");
            earL = Find("EarL"); earR = Find("EarR");
            tail = Find("Tail");
            scarfL = Find("ScarfL"); scarfR = Find("ScarfR");
            Rest(armL, ref armLRest); Rest(armR, ref armRRest);
            Rest(earL, ref earLRest); Rest(earR, ref earRRest);
            Rest(tail, ref tailRest);
            Rest(scarfL, ref scarfLRest); Rest(scarfR, ref scarfRRest);
            if (eyeL != null) eyeScale = eyeL.localScale;
            lastPos = transform.position;
            nextBlink = Time.time + Random.Range(1.5f, 4f);
            nextTwitch = Time.time + Random.Range(2f, 5f);
            teleportable = GetComponent<PVXTeleportable>();
        }

        static void Rest(Transform t, ref Quaternion q) { if (t != null) q = t.localRotation; }

        void OnEnable() { if (teleportable != null) teleportable.Phase += OnPhase; }
        void OnDisable() { if (teleportable != null) teleportable.Phase -= OnPhase; }

        // Cheer once the rebuild has finished.
        void OnPhase(PVXTeleportable t, bool dissolving) { if (!dissolving) cheer = -t.dissolveTime; }

        Transform Find(string n)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t;
            return null;
        }

        void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), time = Time.time;
            Vector3 delta = transform.position - lastPos;
            lastPos = transform.position;
            // Ignore teleport jumps.
            float v = delta.magnitude < 1f ? delta.magnitude / dt : 0f;
            speed = Mathf.Lerp(speed, v, 1f - Mathf.Exp(-10f * dt));
            float amount = Mathf.Clamp01(speed / 2f);
            phase += dt * (2f + speed * stride * 0.5f);

            float s = Mathf.Sin(phase);
            if (legL != null) legL.localRotation = Quaternion.Euler(s * legSwing * amount, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(-s * legSwing * amount, 0f, 0f);

            // Cheer: a squashy hop with arms up and happy eyes.
            float c = 0f;
            if (cheer > -10f)
            {
                cheer += dt;
                if (cheer >= 0f)
                {
                    float k = cheer / 0.7f;
                    c = k < 1f ? Mathf.Sin(k * Mathf.PI) : 0f;
                    if (k >= 1f) cheer = -100f;
                }
            }
            bool happy = c > 0.01f;
            float armsUp = c * 120f;
            if (armL != null) armL.localRotation = armLRest * Quaternion.Euler(-s * armSwing * amount, 0f, -armsUp);
            if (armR != null) armR.localRotation = armRRest * Quaternion.Euler(s * armSwing * amount, 0f, armsUp);

            if (visual != null)
            {
                float step = Mathf.Abs(s) * bob * amount;
                float breathe = Mathf.Sin(time * 2.2f) * 0.015f * (1f - amount);
                float jump = c * cheerJump;
                visual.localPosition = new Vector3(0f, step + jump, 0f);
                // Squash on landing, stretch at the top of each hop.
                float squash = 1f + breathe + (Mathf.Abs(s) - 0.5f) * 0.04f * amount + (c > 0f ? (c - 0.5f) * 0.1f : 0f);
                visual.localScale = new Vector3(1f / Mathf.Sqrt(squash), squash, 1f / Mathf.Sqrt(squash));
            }

            // Head: curious sway when idle, bobs with the steps, tilts back while cheering.
            if (head != null)
                head.localRotation = Quaternion.Euler(Mathf.Sin(phase * 2f) * 4f * amount - c * 12f,
                    Mathf.Sin(time * 0.7f) * 10f * (1f - amount), Mathf.Sin(time * 0.9f + 1f) * 7f * (1f - amount));

            // Blink: quick squash of both eyes every few seconds, sometimes a double blink.
            if (time >= nextBlink && blink < 0f) { blink = 0f; nextBlink = time + (Random.value < 0.2f ? 0.35f : Random.Range(2f, 5f)); }
            float lid = 1f;
            if (blink >= 0f)
            {
                blink += dt / 0.16f;
                lid = 1f - Mathf.Sin(Mathf.Clamp01(blink) * Mathf.PI) * 0.92f;
                if (blink >= 1f) blink = -1f;
            }
            Vector3 es = new Vector3(eyeScale.x, eyeScale.y * lid, eyeScale.z);
            if (eyeL != null) { eyeL.localScale = es; eyeL.gameObject.SetActive(!happy); }
            if (eyeR != null) { eyeR.localScale = es; eyeR.gameObject.SetActive(!happy); }
            if (happyL != null) happyL.gameObject.SetActive(happy);
            if (happyR != null) happyR.gameObject.SetActive(happy);

            // Ears: flop back a little when running, perk up when cheering, random twitch of one ear.
            if (time >= nextTwitch && earTwitch < 0f) { earTwitch = 0f; twitchSide = Random.value < 0.5f ? -1f : 1f; nextTwitch = time + Random.Range(2.5f, 6f); }
            float tw = 0f;
            if (earTwitch >= 0f)
            {
                earTwitch += dt / 0.25f;
                tw = Mathf.Sin(Mathf.Clamp01(earTwitch) * Mathf.PI * 2f) * 18f;
                if (earTwitch >= 1f) earTwitch = -1f;
            }
            float flop = -amount * 14f + c * 12f;
            if (earL != null) earL.localRotation = earLRest * Quaternion.Euler(flop, 0f, twitchSide < 0f ? tw : 0f);
            if (earR != null) earR.localRotation = earRRest * Quaternion.Euler(flop, 0f, twitchSide > 0f ? -tw : 0f);

            // Tail: lazy sway idle, faster wag when walking, straight up when happy.
            if (tail != null)
                tail.localRotation = tailRest * Quaternion.Euler(-c * 25f + Mathf.Sin(time * 3f) * 4f, Mathf.Sin(time * (2f + amount * 6f)) * (18f + amount * 10f + c * 20f), 0f);

            // Scarf ends: a springy swing that lags behind the motion.
            float target = amount * 40f + c * 30f;
            swingVel += ((target - swing) * 60f - swingVel * 8f) * dt;
            swing += swingVel * dt;
            float flutter = Mathf.Sin(time * 9f) * 4f * amount;
            if (scarfL != null) scarfL.localRotation = scarfLRest * Quaternion.Euler(swing + flutter, 0f, 0f);
            if (scarfR != null) scarfR.localRotation = scarfRRest * Quaternion.Euler(swing * 0.85f - flutter, 0f, 0f);
        }
    }
}
