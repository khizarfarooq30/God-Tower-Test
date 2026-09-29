using UnityEngine;

public class TowerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform tower;
    [SerializeField] private Transform cameraTransform;

    [Header("Framing")]
    [SerializeField] private float viewYaw;
    [SerializeField, Min(0f)] private float distance = 38f;
    [SerializeField, Range(-30f, 30f)] private float pitch;
    [SerializeField] private float verticalOffset = 2.5f;

    [Header("Follow")]
    [SerializeField, Min(0f)] private float followSmoothTime = 0.2f;
    [SerializeField, Min(0f)] private float verticalDeadZone = 0.4f;
    [SerializeField, Min(0f)] private float downwardFollowThreshold = 3f;

    [Header("Bounds")]
    [SerializeField] private bool clampToStartHeight = true;
    [SerializeField] private float minimumCameraY;

    private float trackedY;
    private float minY;
    private float velocity;

    private void Awake()
    {
        if (target == null || cameraTransform == null)
        {
            Debug.LogError($"{nameof(TowerCameraController)} on '{name}' is missing target or camera reference.", this);
            enabled = false;
            return;
        }

        if (cameraTransform == transform || !cameraTransform.IsChildOf(transform))
        {
            Debug.LogError($"{nameof(TowerCameraController)} must be on a rig object that is a parent of the camera, not on the camera itself.", this);
            enabled = false;
            return;
        }

        ApplyFraming();
        ResetCamera();
    }

    public void SetTower(Transform newTower)
    {
        tower = newTower;
    }

    public void ResetCamera()
    {
        if (!enabled || tower == null)
            return;

        trackedY = target.position.y;
        velocity = 0f;
        float startY = DesiredY();
        minY = clampToStartHeight ? Mathf.Max(minimumCameraY, startY) : minimumCameraY;
        SetRigPosition(Mathf.Max(minY, startY));
    }

    private void OnValidate()
    {
        if (cameraTransform != null)
            ApplyFraming();
    }

    private void LateUpdate()
    {
        if (tower == null)
            return;

        UpdateTracking(target.position.y);
        float desiredY = Mathf.Max(minY, DesiredY());
        SetRigPosition(Mathf.SmoothDamp(transform.position.y, desiredY, ref velocity, followSmoothTime));
    }

    private void UpdateTracking(float targetY)
    {
        if (targetY > trackedY + verticalDeadZone)
            trackedY = targetY - verticalDeadZone;
        else if (downwardFollowThreshold > 0f && targetY < trackedY - downwardFollowThreshold)
            trackedY = targetY + downwardFollowThreshold;
    }

    private float DesiredY() => trackedY + verticalDeadZone + verticalOffset;

    private void SetRigPosition(float y)
    {
        Vector3 towerPosition = tower.position;
        transform.position = new Vector3(towerPosition.x, y, towerPosition.z);
    }

    private void ApplyFraming()
    {
        transform.rotation = Quaternion.Euler(0f, viewYaw, 0f);
        cameraTransform.localPosition = new Vector3(0f, 0f, -distance);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || tower == null)
            return;

        Vector3 center = tower.position;
        DrawHeightLine(center, trackedY - verticalDeadZone, Color.yellow);
        DrawHeightLine(center, trackedY + verticalDeadZone, Color.yellow);
        DrawHeightLine(center, DesiredY(), Color.cyan);
    }

    private static void DrawHeightLine(Vector3 center, float y, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(center.x - 4f, y, center.z), new Vector3(center.x + 4f, y, center.z));
    }
}
