using System;
using UnityEngine;
using UnityEngine.Serialization;

public class TowerClimber : MonoBehaviour
{
    public enum ClimbState { Attached, Jumping, Hit, Falling, Recovering }

    [Header("Tower")]
    [SerializeField] private Transform tower;
    [SerializeField, Min(0f)] private float towerRadius = 2f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.25f;
    [SerializeField, FormerlySerializedAs("angle")] private float startAngle;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float jumpHeight = 2f;
    [SerializeField, Min(0.01f)] private float jumpDuration = 0.5f;
    [SerializeField, Min(0f)] private float outwardDistance = 0.6f;
    [SerializeField] private AnimationCurve heightCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Circumference")]
    [SerializeField, Min(0f)] private float angularSpeed = 120f;
    [SerializeField, Range(0f, 1f)] private float jumpAngularSpeedMultiplier = 0.7f;
    [SerializeField, Min(0f)] private float maxDragAngularSpeed = 720f;

    [Header("Collision")]
    [SerializeField] private ClimberCollisionDetector collisionDetector;

    [Header("Hit")]
    [SerializeField, Min(0.01f)] private float hitDuration = 0.15f;
    [SerializeField, Min(0f)] private float hitOutwardDistance = 0.45f;
    [SerializeField, Min(0f)] private float hitVerticalDrop = 0.2f;

    [Header("Fall")]
    [SerializeField, Min(0f)] private float fallDistance = 4f;
    [SerializeField, Min(0.01f)] private float fallSpeed = 8f;

    [Header("Recovery")]
    [SerializeField, FormerlySerializedAs("hitRecoveryDuration"), Min(0.01f)] private float recoveryDuration = 0.2f;

    [Header("Visual")]
    [SerializeField] private Transform visual;
    [SerializeField] private float visualYawOffset;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges;

    private ClimbState state;
    private float height;
    private float angle;
    private float outward;
    private float horizontalInput;
    private float pendingSteeringDegrees;
    private float steeringAmount;
    private float minimumHeight;
    private bool controlEnabled = true;

    private float stateElapsed;
    private float jumpStartHeight;
    private float hitStartHeight;
    private float hitStartOutward;
    private float fallTargetHeight;
    private float recoveryStartOutward;

    public event Action<ClimbState> StateChanged;

    public ClimbState State => state;
    public bool IsJumping => state == ClimbState.Jumping;
    public float Height => height;
    public float Angle => angle;
    public bool HasTower => tower != null;
    public Vector3 TowerPosition => tower.position;
    public float TowerRadius => towerRadius;
    public Vector3 RadialDirection => DirectionAt(angle);
    public float JumpHeight => jumpHeight;
    public float FallDistance => fallDistance;
    public float JumpStartHeight => jumpStartHeight;
    public float SteeringInput => steeringAmount;
    public bool CanStartClimb => enabled && controlEnabled && tower != null && state == ClimbState.Attached;
    public float JumpProgress => state == ClimbState.Jumping ? Mathf.Clamp01(stateElapsed / jumpDuration) : 0f;

    private void Awake()
    {
        if (visual == null)
            Debug.LogWarning($"{nameof(TowerClimber)} on '{name}' has no visual assigned; facing will not be applied.", this);

        if (collisionDetector == null)
            TryGetComponent(out collisionDetector);

        angle = Mathf.Repeat(startAngle, 360f);

        if (tower == null)
            return;

        height = transform.position.y - tower.position.y;
        minimumHeight = height;
        ApplyPose();
    }

    public void SetTower(Transform newTower)
    {
        if (newTower == null)
        {
            Debug.LogError($"{nameof(TowerClimber)} on '{name}' was given a null tower.", this);
            return;
        }

        tower = newTower;
    }

    public void PlaceAt(Vector3 worldPoint)
    {
        if (tower == null)
            return;

        Vector3 offset = worldPoint - tower.position;
        bool onAxis = offset.x * offset.x + offset.z * offset.z < 0.0001f;
        float placeAngle = onAxis ? startAngle : Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
        SetClimbPosition(offset.y, placeAngle);
    }

    public void SetClimbPosition(float newHeight, float newAngle)
    {
        if (tower == null)
            return;

        height = newHeight;
        minimumHeight = newHeight;
        angle = Mathf.Repeat(newAngle, 360f);
        outward = 0f;
        ClearSteering();
        stateElapsed = 0f;
        SetState(ClimbState.Attached);
        ApplyPose();
    }

    public void SetControlEnabled(bool isEnabled)
    {
        controlEnabled = isEnabled;
        ClearSteering();
    }

    public void SetHorizontalInput(float value)
    {
        horizontalInput = controlEnabled ? Mathf.Clamp(value, -1f, 1f) : 0f;
    }

    public void AddSteeringDegrees(float degrees)
    {
        if (controlEnabled && (state == ClimbState.Attached || state == ClimbState.Jumping))
            pendingSteeringDegrees += degrees;
    }

    public bool ReceiveHit()
    {
        if (!enabled || tower == null || !controlEnabled)
            return false;

        if (state != ClimbState.Attached && state != ClimbState.Jumping)
            return false;

        BeginHit(state == ClimbState.Jumping ? jumpStartHeight : height);
        ApplyPose();
        return true;
    }

    public bool TryJump()
    {
        if (!enabled || !controlEnabled || state != ClimbState.Attached)
            return false;

        jumpStartHeight = height;
        stateElapsed = 0f;
        SetState(ClimbState.Jumping);
        return true;
    }

    private void Update()
    {
        if (tower == null)
            return;

        float deltaTime = Time.deltaTime;

        if (state != ClimbState.Attached && state != ClimbState.Jumping)
            ClearSteering();

        switch (state)
        {
            case ClimbState.Hit:
                UpdateHit(deltaTime);
                break;
            case ClimbState.Falling:
                UpdateFall(deltaTime);
                break;
            case ClimbState.Recovering:
                UpdateRecovery(deltaTime);
                break;
            default:
                UpdateClimbing(deltaTime);
                break;
        }

        ApplyPose();
    }

    private void UpdateClimbing(float deltaTime)
    {
        if (!controlEnabled)
        {
            ClearSteering();
            UpdateJump(deltaTime);
            return;
        }

        if (IsOverlapping(CalculatePosition(height, angle, outward)))
        {
            BeginHit(state == ClimbState.Jumping ? jumpStartHeight : height);
            return;
        }

        ClimbState previousState = state;
        float previousHeight = height;
        float previousAngle = angle;
        float previousOutward = outward;

        UpdateAngle(deltaTime);
        UpdateJump(deltaTime);

        Vector3 from = CalculatePosition(previousHeight, previousAngle, previousOutward);
        Vector3 to = CalculatePosition(height, angle, outward);

        if (!IsBlocked(from, to))
            return;

        angle = previousAngle;
        steeringAmount = 0f;

        if (previousState != ClimbState.Jumping)
            return;

        height = previousHeight;
        outward = previousOutward;
        BeginHit(jumpStartHeight);
    }

    private void UpdateAngle(float deltaTime)
    {
        float maxDrag = maxDragAngularSpeed * deltaTime;
        float drag = Mathf.Clamp(pendingSteeringDegrees, -maxDrag, maxDrag);
        pendingSteeringDegrees = 0f;

        float multiplier = state == ClimbState.Jumping ? jumpAngularSpeedMultiplier : 1f;
        float delta = (horizontalInput * angularSpeed * deltaTime + drag) * multiplier;

        float fullSpeedStep = angularSpeed * deltaTime;
        steeringAmount = fullSpeedStep > 0f ? Mathf.Clamp(delta / fullSpeedStep, -1f, 1f) : 0f;

        if (!Mathf.Approximately(delta, 0f))
            angle = Mathf.Repeat(angle - delta, 360f);
    }

    private void ClearSteering()
    {
        horizontalInput = 0f;
        pendingSteeringDegrees = 0f;
        steeringAmount = 0f;
    }

    private void UpdateJump(float deltaTime)
    {
        if (state != ClimbState.Jumping)
            return;

        stateElapsed += deltaTime;
        float t = Mathf.Clamp01(stateElapsed / jumpDuration);

        height = jumpStartHeight + jumpHeight * heightCurve.Evaluate(t);
        outward = Mathf.Sin(t * Mathf.PI) * outwardDistance;

        if (t < 1f)
            return;

        height = jumpStartHeight + jumpHeight;
        outward = 0f;
        SetState(ClimbState.Attached);
    }

    private void BeginHit(float progressHeight)
    {
        hitStartHeight = height;
        hitStartOutward = outward;
        fallTargetHeight = Mathf.Max(progressHeight - fallDistance, minimumHeight);
        stateElapsed = 0f;
        SetState(ClimbState.Hit);
    }

    private void UpdateHit(float deltaTime)
    {
        stateElapsed += deltaTime;
        float t = Mathf.Clamp01(stateElapsed / hitDuration);
        float eased = 1f - (1f - t) * (1f - t);

        height = hitStartHeight - hitVerticalDrop * eased;
        outward = hitStartOutward + hitOutwardDistance * eased;

        if (t >= 1f)
            SetState(ClimbState.Falling);
    }

    private void UpdateFall(float deltaTime)
    {
        height = Mathf.MoveTowards(height, fallTargetHeight, fallSpeed * deltaTime);

        if (!Mathf.Approximately(height, fallTargetHeight))
            return;

        height = fallTargetHeight;
        recoveryStartOutward = outward;
        stateElapsed = 0f;
        SetState(ClimbState.Recovering);
    }

    private void UpdateRecovery(float deltaTime)
    {
        stateElapsed += deltaTime;
        float t = Mathf.Clamp01(stateElapsed / recoveryDuration);
        outward = Mathf.Lerp(recoveryStartOutward, 0f, Mathf.SmoothStep(0f, 1f, t));

        if (t < 1f)
            return;

        height = fallTargetHeight;
        outward = 0f;
        SetState(ClimbState.Attached);
    }

    private void SetState(ClimbState newState)
    {
        if (state == newState)
            return;

        if (logStateChanges)
            Debug.Log($"{name}: {state} → {newState}", this);

        state = newState;
        StateChanged?.Invoke(newState);
    }

    private bool IsBlocked(Vector3 from, Vector3 to)
    {
        return collisionDetector != null && collisionDetector.IsBlocked(from, to);
    }

    private bool IsOverlapping(Vector3 position)
    {
        return collisionDetector != null && collisionDetector.IsOverlapping(position);
    }

    private static Vector3 DirectionAt(float angleDegrees)
    {
        return Quaternion.Euler(0f, angleDegrees, 0f) * Vector3.back;
    }

    private Vector3 CalculatePosition(float atHeight, float atAngle, float atOutward)
    {
        float radialDistance = towerRadius + surfaceOffset + atOutward;
        return tower.position + Vector3.up * atHeight + DirectionAt(atAngle) * radialDistance;
    }

    private void ApplyPose()
    {
        transform.position = CalculatePosition(height, angle, outward);

        if (visual != null)
            visual.rotation = Quaternion.LookRotation(-DirectionAt(angle), Vector3.up) * Quaternion.Euler(0f, visualYawOffset, 0f);
    }
}
