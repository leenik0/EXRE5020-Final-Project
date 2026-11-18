using UnityEngine;
using Oculus.Interaction;

public class SnapRotationFilter : MonoBehaviour, IGameObjectFilter
{
    [Tooltip("Leave empty to use this GameObject's transform")]
    [SerializeField] private Transform _targetPose;

    [SerializeField] private float _maxAngleDifference = 45f;

    [SerializeField] private bool _logAngles = true;

    public bool Filter(GameObject go)
    {
        if (go == null) return false;

        Transform target = _targetPose != null ? _targetPose : transform;

        // Grab the SnapInteractor's SnapPose (the exact transform Meta aligns to the socket)
        SnapInteractor interactor = go.GetComponentInChildren<SnapInteractor>();
        if (interactor == null) interactor = go.GetComponentInParent<SnapInteractor>();

        Quaternion currentRot = interactor != null ? interactor.SnapPose.rotation : go.transform.rotation;
        float angle = Quaternion.Angle(currentRot, target.rotation);

        if (_logAngles)
        {
            Debug.Log($"[SnapFilter] Object: {go.name} | Angle off: {angle:F1}° (Allowed: {_maxAngleDifference}°)");
        }

        return angle <= _maxAngleDifference;
    }
}