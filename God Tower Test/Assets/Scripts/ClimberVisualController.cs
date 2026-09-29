using UnityEngine;

public class ClimberVisualController : MonoBehaviour
{
    [SerializeField] private TowerClimber climber;
    [SerializeField] private Transform leanPivot;
    [SerializeField] private ClimberHandIK handIK;

    [Header("Lean")]
    [SerializeField] private float jumpLeanAngle = 8f;
    [SerializeField] private float steeringLeanAngle = 6f;
    [SerializeField] private float reachRollAngle = 12f;
    [SerializeField] private float hitVisualTilt = 15f;
    [SerializeField] private float fallVisualTilt = 8f;
    [SerializeField, Min(0f)] private float leanSmoothSpeed = 10f;

    private void Awake()
    {
        if (climber == null)
            climber = GetComponentInParent<TowerClimber>();

        if (handIK == null)
            handIK = GetComponentInChildren<ClimberHandIK>();

        if (climber == null || leanPivot == null)
        {
            Debug.LogError($"{nameof(ClimberVisualController)} on '{name}' is missing climber or lean pivot.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Vector3 towardTower = -climber.RadialDirection;
        Vector3 right = Vector3.Cross(Vector3.up, towardTower);

        Quaternion leanWorld =
            Quaternion.AngleAxis(SideRoll(), towardTower) *
            Quaternion.AngleAxis(-AwayTilt(), right);

        Transform parent = leanPivot.parent;
        Quaternion parentRotation = parent != null ? parent.rotation : Quaternion.identity;
        Quaternion desiredLocal = Quaternion.Inverse(parentRotation) * leanWorld * parentRotation;

        float blend = 1f - Mathf.Exp(-leanSmoothSpeed * Time.deltaTime);
        leanPivot.localRotation = Quaternion.Slerp(leanPivot.localRotation, desiredLocal, blend);
    }

    private float SideRoll()
    {
        float roll = -climber.SteeringInput * steeringLeanAngle;

        if (climber.State == TowerClimber.ClimbState.Jumping && handIK != null)
        {
            float side = handIK.IsLeftHandHigh ? 1f : -1f;
            roll += side * reachRollAngle * Mathf.Sin(climber.JumpProgress * Mathf.PI);
        }

        return roll;
    }

    private float AwayTilt()
    {
        switch (climber.State)
        {
            case TowerClimber.ClimbState.Jumping:
                return Mathf.Sin(climber.JumpProgress * Mathf.PI) * jumpLeanAngle;
            case TowerClimber.ClimbState.Hit:
                return hitVisualTilt;
            case TowerClimber.ClimbState.Falling:
                return fallVisualTilt;
            default:
                return 0f;
        }
    }
}
