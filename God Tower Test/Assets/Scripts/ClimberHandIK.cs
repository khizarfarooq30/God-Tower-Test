using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Animator))]
public class ClimberHandIK : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerClimber climber;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Placement")]
    [SerializeField, FormerlySerializedAs("handHeightOffset")] private float highHandOffset = 0.7f;
    [SerializeField] private float lowHandOffset = 0.2f;
    [SerializeField, Min(0f)] private float handSpacing = 0.5f;
    [SerializeField] private float handSurfaceOffset = 0.05f;
    [SerializeField] private Vector3 leftHandRotationOffset = new Vector3(0f, 90f, -90f);
    [SerializeField] private Vector3 rightHandRotationOffset = new Vector3(0f, -90f, 90f);

    [Header("Hand Over Hand")]
    [SerializeField, Range(0.05f, 1f)] private float leadHandEnd = 0.6f;
    [SerializeField, Range(0f, 0.95f)] private float trailHandStart = 0.35f;
    [SerializeField, Min(0f)] private float handReleaseDistance = 0.15f;
    [SerializeField, Min(0f)] private float reachHeight = 0.5f;
    [SerializeField] private AnimationCurve reachCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(0.85f, 0f),
        new Keyframe(1f, 0f));

    [Header("Weights")]
    [SerializeField, Range(0f, 1f)] private float positionWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float rotationWeight = 0.8f;
    [SerializeField, Range(0f, 1f)] private float jumpingWeight = 1f;
    [SerializeField, Min(0f)] private float ikBlendSpeed = 5f;
    [SerializeField, Min(0f)] private float ikReleaseSpeed = 12f;

    private Animator animator;
    private float stateWeight;
    private TowerClimber.ClimbState lastState;
    private bool leftIsHigh = true;
    private float leftFromY;
    private float leftToY;
    private float rightFromY;
    private float rightToY;

    public bool IsLeftHandHigh => leftIsHigh;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (climber == null)
            climber = GetComponentInParent<TowerClimber>();

        if (climber == null || leftHandTarget == null || rightHandTarget == null)
        {
            Debug.LogError($"{nameof(ClimberHandIK)} on '{name}' is missing climber or hand targets.", this);
            enabled = false;
            return;
        }

        if (!animator.isHuman)
            Debug.LogError($"{nameof(ClimberHandIK)} on '{name}' requires a Humanoid avatar.", this);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!enabled || layerIndex != 0 || !climber.HasTower)
            return;

        UpdateStateWeight(Time.deltaTime);
        TrackJumpStart();

        float leftY = HandHeight(true, out float leftRelease);
        float rightY = HandHeight(false, out float rightRelease);

        PlaceTarget(leftHandTarget, -1f, leftY, leftRelease, leftHandRotationOffset);
        PlaceTarget(rightHandTarget, 1f, rightY, rightRelease, rightHandRotationOffset);

        ApplyGoal(AvatarIKGoal.LeftHand, leftHandTarget);
        ApplyGoal(AvatarIKGoal.RightHand, rightHandTarget);
    }

    private void UpdateStateWeight(float deltaTime)
    {
        float target = TargetStateWeight();
        float speed = target < stateWeight ? ikReleaseSpeed : ikBlendSpeed;
        stateWeight = Mathf.MoveTowards(stateWeight, target, speed * deltaTime);
    }

    private float TargetStateWeight()
    {
        switch (climber.State)
        {
            case TowerClimber.ClimbState.Attached:
            case TowerClimber.ClimbState.Recovering:
                return 1f;
            case TowerClimber.ClimbState.Jumping:
                return jumpingWeight;
            default:
                return 0f;
        }
    }

    private void TrackJumpStart()
    {
        TowerClimber.ClimbState state = climber.State;
        if (state == TowerClimber.ClimbState.Jumping && lastState != TowerClimber.ClimbState.Jumping)
            BeginHandSwap();

        lastState = state;
    }

    private void BeginHandSwap()
    {
        float startY = climber.TowerPosition.y + climber.JumpStartHeight;
        float landY = startY + climber.JumpHeight;

        leftFromY = startY + RestOffset(leftIsHigh);
        rightFromY = startY + RestOffset(!leftIsHigh);

        leftIsHigh = !leftIsHigh;

        leftToY = landY + RestOffset(leftIsHigh);
        rightToY = landY + RestOffset(!leftIsHigh);
    }

    private float HandHeight(bool isLeft, out float release)
    {
        bool isHigh = isLeft == leftIsHigh;
        release = 0f;

        if (climber.State != TowerClimber.ClimbState.Jumping)
            return climber.transform.position.y + RestOffset(isHigh);

        float progress = climber.JumpProgress;
        float t = isHigh
            ? Mathf.InverseLerp(0f, leadHandEnd, progress)
            : Mathf.InverseLerp(trailHandStart, 1f, progress);

        release = Mathf.Sin(t * Mathf.PI) * handReleaseDistance;

        float from = isLeft ? leftFromY : rightFromY;
        float to = isLeft ? leftToY : rightToY;
        float overshoot = isHigh ? reachCurve.Evaluate(progress) * reachHeight : 0f;
        return Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)) + overshoot;
    }

    private float RestOffset(bool isHigh) => isHigh ? highHandOffset : lowHandOffset;

    private void PlaceTarget(Transform target, float side, float y, float release, Vector3 rotationOffset)
    {
        Vector3 radial = climber.RadialDirection;
        Vector3 right = Vector3.Cross(Vector3.up, -radial);
        float surfaceRadius = climber.TowerRadius + handSurfaceOffset;
        float arc = surfaceRadius > 0f ? handSpacing * 0.5f / surfaceRadius : 0f;

        Vector3 direction = radial * Mathf.Cos(arc) + right * (side * Mathf.Sin(arc));
        Vector3 axis = climber.TowerPosition;
        Vector3 position = new Vector3(axis.x, y, axis.z) + direction * (surfaceRadius + release);
        Quaternion rotation = Quaternion.LookRotation(-direction, Vector3.up) * Quaternion.Euler(rotationOffset);

        target.SetPositionAndRotation(position, rotation);
    }

    private void ApplyGoal(AvatarIKGoal goal, Transform target)
    {
        animator.SetIKPositionWeight(goal, positionWeight * stateWeight);
        animator.SetIKRotationWeight(goal, rotationWeight * stateWeight);
        animator.SetIKPosition(goal, target.position);
        animator.SetIKRotation(goal, target.rotation);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (leftHandTarget != null)
            Gizmos.DrawWireSphere(leftHandTarget.position, 0.06f);

        Gizmos.color = Color.magenta;
        if (rightHandTarget != null)
            Gizmos.DrawWireSphere(rightHandTarget.position, 0.06f);
    }
}
