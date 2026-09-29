using UnityEngine;

public class ClimberFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerClimber climber;
    [SerializeField] private LevelFlow flow;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private TrailRenderer trail;

    [Header("VFX Prefabs")]
    [SerializeField] private ParticleSystem contactVfx;
    [SerializeField] private ParticleSystem hitVfx;
    [SerializeField] private ParticleSystem celebrationVfx;

    [Header("Placement")]
    [SerializeField] private float contactHeightOffset = 0.7f;
    [SerializeField] private float hitHeightOffset = 0.4f;
    [SerializeField] private float celebrationHeightOffset = 1.2f;
    [SerializeField, Min(1f)] private float finalCelebrationScale = 1.6f;

    [Header("Camera")]
    [SerializeField] private Vector2 landShake = new Vector2(0.08f, 0.03f);
    [SerializeField] private Vector2 hitShake = new Vector2(0.2f, 0.25f);
    [SerializeField] private Vector2 victoryShake = new Vector2(0.25f, 0.08f);

    private ParticleSystem contactInstance;
    private ParticleSystem hitInstance;
    private ParticleSystem celebrationInstance;
    private TowerClimber.ClimbState previousState;

    private void Awake()
    {
        if (climber == null)
            climber = GetComponentInParent<TowerClimber>();

        contactInstance = CreateInstance(contactVfx);
        hitInstance = CreateInstance(hitVfx);
        celebrationInstance = CreateInstance(celebrationVfx);
        SetTrail(false);
    }

    private void OnEnable()
    {
        if (climber != null)
            climber.StateChanged += HandleStateChanged;
        if (flow != null)
            flow.LevelCompleted += HandleLevelCompleted;
    }

    private void OnDisable()
    {
        if (climber != null)
            climber.StateChanged -= HandleStateChanged;
        if (flow != null)
            flow.LevelCompleted -= HandleLevelCompleted;
    }

    private void HandleStateChanged(TowerClimber.ClimbState state)
    {
        switch (state)
        {
            case TowerClimber.ClimbState.Jumping:
                GameAudio.Play(SoundId.Climb);
                SetTrail(true);
                break;

            case TowerClimber.ClimbState.Attached:
                SetTrail(false);
                if (previousState == TowerClimber.ClimbState.Jumping || previousState == TowerClimber.ClimbState.Recovering)
                {
                    GameAudio.Play(SoundId.Grab);
                    PlayAt(contactInstance, ContactPoint(), 1f);
                    Shake(landShake);
                }
                break;

            case TowerClimber.ClimbState.Hit:
                GameAudio.Play(SoundId.Hit);
                PlayAt(hitInstance, climber.transform.position + Vector3.up * hitHeightOffset - climber.RadialDirection * 0.3f, 1f);
                Shake(hitShake);
                SetTrail(false);
                break;

            case TowerClimber.ClimbState.Falling:
                GameAudio.Play(SoundId.Fall);
                SetTrail(true);
                break;

            case TowerClimber.ClimbState.Recovering:
                GameAudio.Play(SoundId.Recover);
                SetTrail(false);
                break;
        }

        previousState = state;
    }

    private void HandleLevelCompleted(int index, bool isLast)
    {
        GameAudio.Play(isLast ? SoundId.FinalVictory : SoundId.Victory);
        PlayAt(celebrationInstance, climber.transform.position + Vector3.up * celebrationHeightOffset, isLast ? finalCelebrationScale : 1f);
        Shake(victoryShake);
    }

    private Vector3 ContactPoint()
    {
        if (!climber.HasTower)
            return climber.transform.position;

        Vector3 axis = climber.TowerPosition;
        Vector3 surface = new Vector3(axis.x, 0f, axis.z) + climber.RadialDirection * climber.TowerRadius;
        return new Vector3(surface.x, climber.transform.position.y + contactHeightOffset, surface.z);
    }

    private void Shake(Vector2 durationAndStrength)
    {
        if (cameraShake != null)
            cameraShake.Shake(durationAndStrength.x, durationAndStrength.y);
    }

    private void SetTrail(bool emitting)
    {
        if (trail == null)
            return;

        trail.emitting = emitting;
        if (!emitting)
            trail.Clear();
    }

    private static ParticleSystem CreateInstance(ParticleSystem prefab)
    {
        if (prefab == null)
            return null;

        ParticleSystem instance = Instantiate(prefab);
        instance.name = prefab.name;
        ParticleSystem.MainModule main = instance.main;
        main.playOnAwake = false;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        instance.gameObject.SetActive(false);
        return instance;
    }

    private static void PlayAt(ParticleSystem instance, Vector3 position, float scale)
    {
        if (instance == null)
            return;

        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        instance.transform.localScale = Vector3.one * scale;
        instance.gameObject.SetActive(true);
        instance.Clear(true);
        instance.Play(true);
    }
}
