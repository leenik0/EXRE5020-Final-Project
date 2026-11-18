using UnityEngine;
using UnityEngine.Events;

public class OpenGesture : MonoBehaviour
{
    [Header("Tracking References")]
    [SerializeField] private Transform headTransform; // CenterEyeAnchor
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;

    [Header("Gesture Thresholds")]
    [Tooltip("Max horizontal distance (meters) between hands to start the gesture.")]
    [SerializeField] private float startMaxDistance = 0.28f;

    [Tooltip("Min horizontal distance (meters) between hands to complete the gesture.")]
    [SerializeField] private float endMinDistance = 0.70f;

    [Tooltip("Minimum outward speed (m/s) required to keep the gesture alive.")]
    [SerializeField] private float minOutwardSpeed = 0.25f;

    [Tooltip("Max time (seconds) allowed to complete the motion.")]
    [SerializeField] private float maxGestureDuration = 1.5f;

    [Tooltip("Require index pinch during the motion.")]
    [SerializeField] private bool requirePinch = false;

    [Tooltip("If true, hands only need to start inside the zone and can finish outside it.")]
    [SerializeField] private bool allowExitAfterStart = true;
    

    [Header("Global Events")]
    public UnityEvent<OpenGestureZone> onAnyZoneOpened;

    private OpenGestureZone currentZone;
    private bool isTrackingGesture;
    private float gestureTimer;
    private float previousHorizontalDist;

    private void Update()
    {
        if (!AreHandsValid())
        {
            ResetGesture();
            return;
        }

        Vector3 leftPos = leftHand.transform.position;
        Vector3 rightPos = rightHand.transform.position;

        if (!isTrackingGesture)
        {
            OpenGestureZone matchedZone = FindZoneContainingHands(leftPos, rightPos);
            if (matchedZone == null)
                return;

            float horizontalDist = GetHorizontalDistance(leftPos, rightPos);
            if (horizontalDist > 0.02f && horizontalDist <= startMaxDistance)
            {
                currentZone = matchedZone;
                isTrackingGesture = true;
                gestureTimer = 0f;
                previousHorizontalDist = horizontalDist;
            }
        }
        else
        {
            if (currentZone == null || !currentZone.isActiveAndEnabled)
            {
                ResetGesture();
                return;
            }

            if (!allowExitAfterStart && (!currentZone.ContainsPoint(leftPos) || !currentZone.ContainsPoint(rightPos)))
            {
                ResetGesture();
                return;
            }

            gestureTimer += Time.deltaTime;
            float horizontalDist = GetHorizontalDistance(leftPos, rightPos);
            float outwardSpeed = (horizontalDist - previousHorizontalDist) / Time.deltaTime;
            previousHorizontalDist = horizontalDist;

            if (gestureTimer > maxGestureDuration || outwardSpeed < minOutwardSpeed)
            {
                ResetGesture();
                return;
            }

            if (horizontalDist >= endMinDistance)
            {
                OpenGestureZone completedZone = currentZone;
                completedZone.TriggerGesture();
                onAnyZoneOpened?.Invoke(completedZone);
                ResetGesture();
            }
        }
    }

    private OpenGestureZone FindZoneContainingHands(Vector3 leftPos, Vector3 rightPos)
    {
        for (int i = 0; i < OpenGestureZone.ActiveZones.Count; i++)
        {
            OpenGestureZone zone = OpenGestureZone.ActiveZones[i];
            if (zone != null && zone.ContainsPoint(leftPos) && zone.ContainsPoint(rightPos))
            {
                return zone;
            }
        }
        return null;
    }

    private float GetHorizontalDistance(Vector3 leftPos, Vector3 rightPos)
    {
        Vector3 flatRight = Vector3.ProjectOnPlane(headTransform.right, Vector3.up).normalized;
        return Vector3.Dot(rightPos - leftPos, flatRight);
    }

    private bool AreHandsValid()
    {
        if (leftHand == null || rightHand == null || headTransform == null)
            return false;

        if (!leftHand.IsTracked || !rightHand.IsTracked)
            return false;

        if (requirePinch)
        {
            return leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        }

        return true;
    }

    private void ResetGesture()
    {
        currentZone = null;
        isTrackingGesture = false;
        gestureTimer = 0f;
    }
}
