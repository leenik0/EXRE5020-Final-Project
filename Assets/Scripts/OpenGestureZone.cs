using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class OpenGestureZone : MonoBehaviour
{
    public static readonly List<OpenGestureZone> ActiveZones = new List<OpenGestureZone>();

    [Header("Zone Settings")]
    [Tooltip("Disable this zone automatically after the gesture completes once.")]
    public bool singleUse = true;

    [Header("Events")]
    public UnityEvent onGestureCompleted;

    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        if (!ActiveZones.Contains(this))
            ActiveZones.Add(this);
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        if (zoneCollider == null || !zoneCollider.enabled)
            return false;

        Vector3 closest = zoneCollider.ClosestPoint(worldPoint);
        return (closest - worldPoint).sqrMagnitude < 0.0001f;
    }

    public void TriggerGesture()
    {
        onGestureCompleted?.Invoke();

        if (singleUse)
        {
            gameObject.SetActive(false);
        }
    }
}
