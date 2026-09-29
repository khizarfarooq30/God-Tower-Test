using UnityEngine;

public class PopOutTowerObstacle : MonoBehaviour, ILevelResettable
{
    public enum PopOutState { Retracted, Extending, Extended, Retracting, Warning }

    [SerializeField] private Transform block;

    [Header("Motion")]
    [SerializeField, Min(0f)] private float extensionDistance = 1.2f;
    [SerializeField] private AnimationCurve extensionCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Telegraph")]
    [SerializeField, Min(0f)] private float warningDuration = 0.15f;
    [SerializeField, Range(0f, 0.5f)] private float anticipationFraction = 0.07f;
    [SerializeField, Min(0f)] private float audibleVerticalRange = 8f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float initialDelay = 0.5f;
    [SerializeField, Min(0.01f)] private float extensionDuration = 0.18f;
    [SerializeField, Min(0f)] private float extendedHoldDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float retractionDuration = 0.25f;
    [SerializeField, Min(0f)] private float retractedHoldDuration = 1.2f;

    private Vector3 retractedLocalPosition;
    private Vector3 outwardLocal;
    private float elapsed;

    public PopOutState State { get; private set; }

    private float CycleDuration => extensionDuration + extendedHoldDuration + retractionDuration + retractedHoldDuration;

    private void Awake()
    {
        if (block == null)
        {
            Debug.LogError($"{nameof(PopOutTowerObstacle)} on '{name}' has no block assigned.", this);
            enabled = false;
            return;
        }

        retractedLocalPosition = block.localPosition;
        Vector3 radial = new Vector3(retractedLocalPosition.x, 0f, retractedLocalPosition.z);

        if (radial.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning($"{nameof(PopOutTowerObstacle)} on '{name}': block sits on the root axis; extending along local -Z.", this);
            radial = Vector3.back;
        }

        outwardLocal = radial.normalized;
    }

    public void Configure(float delay, float extension, float extendedHold, float retraction, float retractedHold)
    {
        initialDelay = Mathf.Max(0f, delay);
        extensionDuration = Mathf.Max(0.01f, extension);
        extendedHoldDuration = Mathf.Max(0f, extendedHold);
        retractionDuration = Mathf.Max(0.01f, retraction);
        retractedHoldDuration = Mathf.Max(0f, retractedHold);
    }

    public void ResetForLevel()
    {
        if (block == null)
            return;

        elapsed = 0f;
        Apply();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        Apply();
    }

    private void Apply()
    {
        PopOutState previous = State;
        float amount = Evaluate(elapsed, out PopOutState state);
        State = state;

        if (state != previous)
        {
            if (state == PopOutState.Warning)
                GameAudio.PlayNear(SoundId.PopOutWarning, block.position, audibleVerticalRange);
            else if (state == PopOutState.Extending)
                GameAudio.PlayNear(SoundId.PopOut, block.position, audibleVerticalRange);
        }

        block.localPosition = retractedLocalPosition + outwardLocal * (extensionDistance * amount);
    }

    private float Evaluate(float time, out PopOutState state)
    {
        state = PopOutState.Retracted;
        if (time < initialDelay)
            return WarningAmount(initialDelay - time, ref state);

        float t = (time - initialDelay) % CycleDuration;

        if (t < extensionDuration)
        {
            state = PopOutState.Extending;
            return extensionCurve.Evaluate(t / extensionDuration);
        }

        t -= extensionDuration;
        if (t < extendedHoldDuration)
        {
            state = PopOutState.Extended;
            return 1f;
        }

        t -= extendedHoldDuration;
        if (t < retractionDuration)
        {
            state = PopOutState.Retracting;
            return 1f - Mathf.SmoothStep(0f, 1f, t / retractionDuration);
        }

        t -= retractionDuration;
        return WarningAmount(retractedHoldDuration - t, ref state);
    }

    private float WarningAmount(float timeUntilExtension, ref PopOutState state)
    {
        if (warningDuration <= 0f || timeUntilExtension > warningDuration)
            return 0f;

        state = PopOutState.Warning;
        float progress = 1f - timeUntilExtension / warningDuration;
        return -anticipationFraction * Mathf.Sin(progress * Mathf.PI);
    }
}
