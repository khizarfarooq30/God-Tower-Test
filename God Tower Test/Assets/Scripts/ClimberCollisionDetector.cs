using UnityEngine;

public class ClimberCollisionDetector : MonoBehaviour
{
    private const float MinSweepDistance = 0.0001f;

    [SerializeField] private CapsuleCollider hitbox;
    [SerializeField] private LayerMask obstacleMask;

    private void Awake()
    {
        if (hitbox == null || !hitbox.transform.IsChildOf(transform))
        {
            Debug.LogError($"{nameof(ClimberCollisionDetector)} on '{name}' needs a CapsuleCollider hitbox on itself or a child.", this);
            enabled = false;
            return;
        }

        if (hitbox.direction != 1)
            Debug.LogWarning($"{nameof(ClimberCollisionDetector)} expects the hitbox capsule direction to be Y-Axis.", this);

        if (obstacleMask.value == 0)
            Debug.LogWarning($"{nameof(ClimberCollisionDetector)} on '{name}' has an empty obstacle mask.", this);
    }

    public bool IsBlocked(Vector3 fromRoot, Vector3 toRoot)
    {
        if (!enabled)
            return false;

        GetCapsuleOffsets(out Vector3 offsetA, out Vector3 offsetB, out float radius);

        Vector3 delta = toRoot - fromRoot;
        float distance = delta.magnitude;

        if (distance > MinSweepDistance &&
            Physics.CapsuleCast(fromRoot + offsetA, fromRoot + offsetB, radius, delta / distance, distance, obstacleMask, QueryTriggerInteraction.Collide))
            return true;

        return Physics.CheckCapsule(toRoot + offsetA, toRoot + offsetB, radius, obstacleMask, QueryTriggerInteraction.Collide);
    }

    public bool IsOverlapping(Vector3 root)
    {
        if (!enabled)
            return false;

        GetCapsuleOffsets(out Vector3 offsetA, out Vector3 offsetB, out float radius);
        return Physics.CheckCapsule(root + offsetA, root + offsetB, radius, obstacleMask, QueryTriggerInteraction.Collide);
    }

    private void GetCapsuleOffsets(out Vector3 offsetA, out Vector3 offsetB, out float radius)
    {
        Transform hitboxTransform = hitbox.transform;
        Vector3 scale = hitboxTransform.lossyScale;
        radius = hitbox.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfSegment = Mathf.Max(hitbox.height * Mathf.Abs(scale.y) * 0.5f - radius, 0f);

        Vector3 center = hitboxTransform.TransformPoint(hitbox.center) - transform.position;
        Vector3 segment = hitboxTransform.up * halfSegment;
        offsetA = center + segment;
        offsetB = center - segment;
    }
}
