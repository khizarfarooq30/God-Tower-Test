using UnityEngine;

[CreateAssetMenu(menuName = "God Tower/Level Build Kit", fileName = "LevelBuildKit")]
public class LevelBuildKit : ScriptableObject
{
    [field: Header("Tower")]
    [field: SerializeField] public GameObject SectionPrefab { get; private set; }
    [field: SerializeField, Min(0.1f)] public float SectionHeight { get; private set; } = 5f;

    [field: Header("Obstacles")]
    [field: SerializeField] public GameObject LedgePrefab { get; private set; }
    [field: SerializeField] public GameObject ArmPrefab { get; private set; }
    [field: SerializeField] public GameObject TwoArmPrefab { get; private set; }
    [field: SerializeField] public GameObject PopOutPrefab { get; private set; }

    [field: Header("Finish")]
    [field: SerializeField] public GameObject FinishMarkerPrefab { get; private set; }
    [field: SerializeField] public float FinishMarkerOffset { get; private set; } = -0.8f;

    [field: Header("Hitbox Alignment")]
    [field: SerializeField] public float LedgeOffsetAboveRest { get; private set; } = 1.2f;
    [field: SerializeField] public float HazardHeightOffset { get; private set; }

    public bool IsValid(out string problem)
    {
        problem = SectionPrefab == null ? nameof(SectionPrefab)
            : LedgePrefab == null ? nameof(LedgePrefab)
            : ArmPrefab == null ? nameof(ArmPrefab)
            : PopOutPrefab == null ? nameof(PopOutPrefab)
            : null;
        return problem == null;
    }
}
