using UnityEngine;

[CreateAssetMenu(menuName = "God Tower/Level Blueprint", fileName = "LevelBlueprint")]
public class LevelBlueprint : ScriptableObject
{
    [field: Header("Climb")]
    [field: SerializeField] public int Seed { get; private set; } = 1;
    [field: SerializeField] public float StartHeight { get; private set; } = 1f;
    [field: SerializeField, Min(4)] public int JumpCount { get; private set; } = 60;
    [field: SerializeField, Min(2)] public int IntroJumps { get; private set; } = 5;
    [field: SerializeField, Min(1)] public int OutroJumps { get; private set; } = 4;
    [field: SerializeField, Min(1)] public int MaxScore { get; private set; } = 5000;

    [field: Header("Spacing (in jumps)")]
    [field: SerializeField, Min(2)] public int MinGapJumps { get; private set; } = 4;
    [field: SerializeField, Min(2)] public int MaxGapJumps { get; private set; } = 7;

    [field: Header("Obstacle Weights")]
    [field: SerializeField, Min(0f)] public float LedgeWeight { get; private set; } = 3f;
    [field: SerializeField, Min(0f)] public float LedgePairWeight { get; private set; }
    [field: SerializeField, Min(0f)] public float ArmWeight { get; private set; } = 2f;
    [field: SerializeField, Min(0f)] public float TwoArmWeight { get; private set; }
    [field: SerializeField, Min(0f)] public float PopOutWeight { get; private set; } = 2f;
    [field: SerializeField, Min(0f)] public float PopOutPairWeight { get; private set; }

    [field: Header("Rotating")]
    [field: SerializeField] public Vector2 ArmSpeedRange { get; private set; } = new Vector2(35f, 50f);
    [field: SerializeField, Range(0.3f, 1f)] public float TwoArmSpeedScale { get; private set; } = 0.75f;
    [field: SerializeField] public bool AllowReverseRotation { get; private set; } = true;

    [field: Header("Pop-Out")]
    [field: SerializeField, Min(0.01f)] public float PopExtensionDuration { get; private set; } = 0.25f;
    [field: SerializeField, Min(0.01f)] public float PopRetractionDuration { get; private set; } = 0.3f;
    [field: SerializeField] public Vector2 PopHoldRange { get; private set; } = new Vector2(0.7f, 0.9f);
    [field: SerializeField] public Vector2 PopWaitRange { get; private set; } = new Vector2(1.8f, 2.4f);

    [field: Header("Tower")]
    [field: SerializeField] public Material[] SectionMaterials { get; private set; }
    [field: SerializeField, Min(0f)] public float TowerTopMargin { get; private set; } = 12f;

    public void RandomizeSeed()
    {
        Seed = Random.Range(1, int.MaxValue);
    }
}
