using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class LevelGenerator : MonoBehaviour
{
    private const string GeneratedRootName = "Generated";
    private const float StartPointForwardOffset = -1f;

    [SerializeField] private LevelController level;
    [SerializeField] private LevelBuildKit kit;
    [SerializeField] private LevelBlueprint blueprint;

    public LevelBlueprint Blueprint => blueprint;

    public void Generate()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Level generation is edit-mode only.", this);
            return;
        }

        if (kit == null || blueprint == null)
        {
            Debug.LogError($"{nameof(LevelGenerator)} on '{name}' needs a build kit and a blueprint.", this);
            return;
        }

        if (!kit.IsValid(out string missing))
        {
            Debug.LogError($"{nameof(LevelBuildKit)} '{kit.name}' is missing {missing}.", kit);
            return;
        }

        if (level == null)
            level = GetComponent<LevelController>();

        if (level == null)
        {
            Debug.LogError($"{nameof(LevelGenerator)} on '{name}' has no Level assigned.", this);
            return;
        }

        TowerClimber climber = level.Climber;
        if (climber == null)
        {
            Debug.LogError($"Assign the Climber on '{level.name}' LevelController before generating.", level);
            return;
        }

        float jump = climber.JumpHeight;
        int fallJumps = Mathf.Max(1, Mathf.RoundToInt(climber.FallDistance / jump));
        float startY = blueprint.StartHeight;
        float finishY = startY + blueprint.JumpCount * jump;

        DestroyGenerated(level.transform);

        Transform root = CreateChild(level.transform, GeneratedRootName, Vector3.zero);
        Transform tower = CreateChild(root, "Tower", Vector3.zero);
        BuildSections(CreateChild(tower, "Sections", Vector3.zero), finishY + blueprint.TowerTopMargin);

        Transform obstacles = CreateChild(tower, "Obstacles", Vector3.zero);
        var placements = LevelLayoutPlanner.Plan(blueprint, fallJumps);
        foreach (ObstaclePlacement placement in placements)
            BuildObstacle(obstacles, placement, startY + placement.RestIndex * jump);

        Transform start = CreateChild(root, "StartPoint", new Vector3(0f, startY, StartPointForwardOffset));
        Transform finish = CreateChild(root, "FinishPoint", new Vector3(0f, finishY, 0f));

        if (kit.FinishMarkerPrefab != null)
            Spawn(kit.FinishMarkerPrefab, root, new Vector3(0f, finishY + kit.FinishMarkerOffset, 0f), 0f).name = "FinishMarker";

#if UNITY_EDITOR
        Undo.RecordObject(level, "Generate Level");
#endif
        level.Configure(tower, start, finish, blueprint.MaxScore);
#if UNITY_EDITOR
        EditorUtility.SetDirty(level);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
#endif

        Debug.Log($"Generated '{level.name}': {placements.Count} obstacles, finish height {finishY}.", this);
    }

    private static void DestroyGenerated(Transform levelRoot)
    {
        Transform existing = levelRoot.Find(GeneratedRootName);
        if (existing != null)
            DestroyImmediate(existing.gameObject);
    }

    private void BuildSections(Transform parent, float totalHeight)
    {
        int count = Mathf.CeilToInt(totalHeight / kit.SectionHeight);
        Material[] materials = blueprint.SectionMaterials;

        for (int i = 0; i < count; i++)
        {
            GameObject section = Spawn(kit.SectionPrefab, parent, new Vector3(0f, kit.SectionHeight * i, 0f), 0f);
            section.name = $"Section_{i + 1:00}";

            if (materials == null || materials.Length == 0)
                continue;

            Renderer body = section.GetComponentInChildren<Renderer>();
            if (body != null)
                body.sharedMaterial = materials[i % materials.Length];
        }
    }

    private void BuildObstacle(Transform parent, ObstaclePlacement placement, float restY)
    {
        string label = $"{placement.Kind}_{placement.RestIndex:000}";
        float hazardY = restY + kit.HazardHeightOffset;

        switch (placement.Kind)
        {
            case ObstacleKind.Ledge:
                SpawnLedge(parent, label, hazardY, placement.AngleA);
                break;

            case ObstacleKind.LedgePair:
                SpawnLedge(parent, label + "_A", hazardY, placement.AngleA);
                SpawnLedge(parent, label + "_B", hazardY, placement.AngleB);
                break;

            case ObstacleKind.Arm:
            case ObstacleKind.TwoArm:
                GameObject prefab = placement.Kind == ObstacleKind.TwoArm && kit.TwoArmPrefab != null ? kit.TwoArmPrefab : kit.ArmPrefab;
                GameObject arm = Spawn(prefab, parent, new Vector3(0f, hazardY, 0f), placement.AngleA);
                arm.name = label;
                ConfigureRotating(arm, placement.RotationSpeed);
                break;

            case ObstacleKind.PopOut:
                SpawnPopOut(parent, label, hazardY, placement.AngleA, placement.DelayA, placement);
                break;

            case ObstacleKind.PopOutPair:
                SpawnPopOut(parent, label + "_A", hazardY, placement.AngleA, placement.DelayA, placement);
                SpawnPopOut(parent, label + "_B", hazardY, placement.AngleB, placement.DelayB, placement);
                break;
        }
    }

    private void SpawnLedge(Transform parent, string label, float hazardY, float angle)
    {
        Spawn(kit.LedgePrefab, parent, new Vector3(0f, hazardY + kit.LedgeOffsetAboveRest, 0f), angle).name = label;
    }

    private void SpawnPopOut(Transform parent, string label, float hazardY, float angle, float delay, ObstaclePlacement placement)
    {
        GameObject popOut = Spawn(kit.PopOutPrefab, parent, new Vector3(0f, hazardY, 0f), angle);
        popOut.name = label;

        if (!popOut.TryGetComponent(out PopOutTowerObstacle obstacle))
        {
            Debug.LogError($"Pop-out prefab '{kit.PopOutPrefab.name}' has no {nameof(PopOutTowerObstacle)} on its root.", kit);
            return;
        }

        obstacle.Configure(delay, blueprint.PopExtensionDuration, placement.HoldDuration, blueprint.PopRetractionDuration, placement.WaitDuration);
        RecordModification(obstacle);
    }

    private void ConfigureRotating(GameObject arm, float speed)
    {
        if (!arm.TryGetComponent(out RotatingTowerObstacle obstacle))
        {
            Debug.LogError($"Arm prefab '{arm.name}' has no {nameof(RotatingTowerObstacle)} on its root.", kit);
            return;
        }

        obstacle.Configure(speed);
        RecordModification(obstacle);
    }

    private static Transform CreateChild(Transform parent, string childName, Vector3 localPosition)
    {
        var child = new GameObject(childName).transform;
        child.SetParent(parent, false);
        child.localPosition = localPosition;
        return child;
    }

    private static GameObject Spawn(GameObject prefab, Transform parent, Vector3 localPosition, float yaw)
    {
        GameObject instance = null;
#if UNITY_EDITOR
        instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
#endif
        if (instance == null)
            instance = Instantiate(prefab, parent);

        instance.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(0f, yaw, 0f));
        return instance;
    }

    private static void RecordModification(Object target)
    {
#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
#endif
    }
}
