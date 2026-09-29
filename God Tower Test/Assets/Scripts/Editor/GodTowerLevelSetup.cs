using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class GodTowerLevelSetup
{
    private const string BlueprintFolder = "Assets/ScriptableObjects/Levels";
    private const string MaterialFolder = "Assets/Materials/Levels";

    private struct Preset
    {
        public int Seed;
        public int JumpCount;
        public int MinGap;
        public int MaxGap;
        public float Ledge;
        public float LedgePair;
        public float Arm;
        public float TwoArm;
        public float Pop;
        public float PopPair;
        public Vector2 ArmSpeed;
        public float TwoArmScale;
        public float PopExtension;
        public float PopRetraction;
        public Vector2 PopHold;
        public Vector2 PopWait;
        public string[] Colors;
    }

    private static readonly Preset[] Presets =
    {
        new Preset
        {
            Seed = 1, JumpCount = 60, MinGap = 4, MaxGap = 7,
            Ledge = 3, LedgePair = 0, Arm = 2, TwoArm = 0, Pop = 2, PopPair = 0,
            ArmSpeed = new Vector2(35, 50), TwoArmScale = 0.75f,
            PopExtension = 0.25f, PopRetraction = 0.3f, PopHold = new Vector2(0.7f, 0.9f), PopWait = new Vector2(1.8f, 2.4f),
            Colors = new[] { "#9A9A8A", "#8A8C80", "#A6A493" }
        },
        new Preset
        {
            Seed = 2, JumpCount = 63, MinGap = 4, MaxGap = 6,
            Ledge = 3, LedgePair = 1, Arm = 2, TwoArm = 0, Pop = 2, PopPair = 0,
            ArmSpeed = new Vector2(45, 60), TwoArmScale = 0.75f,
            PopExtension = 0.22f, PopRetraction = 0.28f, PopHold = new Vector2(0.7f, 0.9f), PopWait = new Vector2(1.6f, 2.0f),
            Colors = new[] { "#D8C8A0", "#CDBB90", "#E2D3AE" }
        },
        new Preset
        {
            Seed = 3, JumpCount = 66, MinGap = 3, MaxGap = 6,
            Ledge = 2, LedgePair = 1, Arm = 3, TwoArm = 0, Pop = 2, PopPair = 2,
            ArmSpeed = new Vector2(60, 80), TwoArmScale = 0.75f,
            PopExtension = 0.2f, PopRetraction = 0.25f, PopHold = new Vector2(0.6f, 0.8f), PopWait = new Vector2(1.3f, 1.6f),
            Colors = new[] { "#9AA6B2", "#8C98A6", "#A7B3BF" }
        },
        new Preset
        {
            Seed = 4, JumpCount = 69, MinGap = 3, MaxGap = 5,
            Ledge = 2, LedgePair = 2, Arm = 3, TwoArm = 1, Pop = 2, PopPair = 1,
            ArmSpeed = new Vector2(75, 95), TwoArmScale = 0.75f,
            PopExtension = 0.18f, PopRetraction = 0.25f, PopHold = new Vector2(0.6f, 0.75f), PopWait = new Vector2(1.1f, 1.4f),
            Colors = new[] { "#6F7A6A", "#65705F", "#7A8575" }
        },
        new Preset
        {
            Seed = 5, JumpCount = 72, MinGap = 3, MaxGap = 5,
            Ledge = 2, LedgePair = 2, Arm = 3, TwoArm = 2, Pop = 2, PopPair = 2,
            ArmSpeed = new Vector2(85, 110), TwoArmScale = 0.7f,
            PopExtension = 0.15f, PopRetraction = 0.2f, PopHold = new Vector2(0.55f, 0.7f), PopWait = new Vector2(1.0f, 1.2f),
            Colors = new[] { "#C9A55A", "#BD984C", "#D4B36B" }
        }
    };

    [MenuItem("Tools/God Tower/Build All 5 Levels")]
    private static void BuildAllLevels()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("God Tower", "Exit Play Mode first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("God Tower",
                "Create/update 5 level blueprints and regenerate Level_01 to Level_05?\nManual edits inside each level's Generated object will be replaced.",
                "Build", "Cancel"))
            return;

        LevelBuildKit kit = FindAsset<LevelBuildKit>();
        TowerClimber climber = Object.FindFirstObjectByType<TowerClimber>(FindObjectsInactive.Include);
        TowerCameraController cameraController = Object.FindFirstObjectByType<TowerCameraController>(FindObjectsInactive.Include);
        LevelFlow flow = Object.FindFirstObjectByType<LevelFlow>(FindObjectsInactive.Include);

        if (kit == null || climber == null || cameraController == null || flow == null)
        {
            EditorUtility.DisplayDialog("God Tower",
                "Missing requirement:\n" +
                (kit == null ? "• LevelBuildKit asset\n" : "") +
                (climber == null ? "• TowerClimber (Player) in scene\n" : "") +
                (cameraController == null ? "• TowerCameraController (CameraRig) in scene\n" : "") +
                (flow == null ? "• LevelFlow in scene\n" : ""),
                "OK");
            return;
        }

        EnsureFolder(BlueprintFolder);
        EnsureFolder(MaterialFolder);

        var levels = new LevelController[Presets.Length];

        for (int i = 0; i < Presets.Length; i++)
        {
            int number = i + 1;
            LevelBlueprint blueprint = CreateOrUpdateBlueprint(number, Presets[i]);
            LevelController level = FindOrCreateLevel(number, flow);

            SetReference(level, "climber", climber);
            SetReference(level, "cameraController", cameraController);

            LevelGenerator generator = level.GetComponent<LevelGenerator>();
            if (generator == null)
                generator = Undo.AddComponent<LevelGenerator>(level.gameObject);

            SetReference(generator, "level", level);
            SetReference(generator, "kit", kit);
            SetReference(generator, "blueprint", blueprint);

            generator.Generate();
            level.gameObject.SetActive(i == 0);
            levels[i] = level;
        }

        AssignFlow(flow, levels);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);

        Debug.Log("God Tower: built Level_01 to Level_05 and assigned them to LevelFlow. Save the scene.");
    }

    private static LevelBlueprint CreateOrUpdateBlueprint(int number, Preset preset)
    {
        string path = $"{BlueprintFolder}/Level{number:00}_Blueprint.asset";
        var blueprint = AssetDatabase.LoadAssetAtPath<LevelBlueprint>(path);

        if (blueprint == null)
        {
            blueprint = ScriptableObject.CreateInstance<LevelBlueprint>();
            AssetDatabase.CreateAsset(blueprint, path);
        }

        var so = new SerializedObject(blueprint);
        SetInt(so, nameof(LevelBlueprint.Seed), preset.Seed);
        SetFloat(so, nameof(LevelBlueprint.StartHeight), 1f);
        SetInt(so, nameof(LevelBlueprint.JumpCount), preset.JumpCount);
        SetInt(so, nameof(LevelBlueprint.IntroJumps), 5);
        SetInt(so, nameof(LevelBlueprint.OutroJumps), 4);
        SetInt(so, nameof(LevelBlueprint.MaxScore), 5000);
        SetInt(so, nameof(LevelBlueprint.MinGapJumps), preset.MinGap);
        SetInt(so, nameof(LevelBlueprint.MaxGapJumps), preset.MaxGap);
        SetFloat(so, nameof(LevelBlueprint.LedgeWeight), preset.Ledge);
        SetFloat(so, nameof(LevelBlueprint.LedgePairWeight), preset.LedgePair);
        SetFloat(so, nameof(LevelBlueprint.ArmWeight), preset.Arm);
        SetFloat(so, nameof(LevelBlueprint.TwoArmWeight), preset.TwoArm);
        SetFloat(so, nameof(LevelBlueprint.PopOutWeight), preset.Pop);
        SetFloat(so, nameof(LevelBlueprint.PopOutPairWeight), preset.PopPair);
        SetVector2(so, nameof(LevelBlueprint.ArmSpeedRange), preset.ArmSpeed);
        SetFloat(so, nameof(LevelBlueprint.TwoArmSpeedScale), preset.TwoArmScale);
        SetBool(so, nameof(LevelBlueprint.AllowReverseRotation), true);
        SetFloat(so, nameof(LevelBlueprint.PopExtensionDuration), preset.PopExtension);
        SetFloat(so, nameof(LevelBlueprint.PopRetractionDuration), preset.PopRetraction);
        SetVector2(so, nameof(LevelBlueprint.PopHoldRange), preset.PopHold);
        SetVector2(so, nameof(LevelBlueprint.PopWaitRange), preset.PopWait);
        SetFloat(so, nameof(LevelBlueprint.TowerTopMargin), 12f);

        SerializedProperty materials = so.FindProperty(Backing(nameof(LevelBlueprint.SectionMaterials)));
        materials.arraySize = preset.Colors.Length;
        for (int i = 0; i < preset.Colors.Length; i++)
            materials.GetArrayElementAtIndex(i).objectReferenceValue = CreateOrUpdateMaterial(number, i, preset.Colors[i]);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(blueprint);
        return blueprint;
    }

    private static Material CreateOrUpdateMaterial(int level, int index, string hex)
    {
        string path = $"{MaterialFolder}/Level{level:00}_Stone_{(char)('A' + index)}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            bool srp = GraphicsSettings.defaultRenderPipeline != null;
            Shader shader = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        ColorUtility.TryParseHtmlString(hex, out Color color);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        material.color = color;

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.2f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0.2f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static LevelController FindOrCreateLevel(int number, LevelFlow flow)
    {
        string levelName = $"Level_{number:00}";
        LevelController[] all = Object.FindObjectsByType<LevelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        LevelController level = all.FirstOrDefault(l => l.name == levelName);

        if (level == null && number == 1)
        {
            var flowSo = new SerializedObject(flow);
            SerializedProperty list = flowSo.FindProperty("levels");
            if (list != null && list.arraySize > 0)
                level = list.GetArrayElementAtIndex(0).objectReferenceValue as LevelController;

            if (level == null)
                level = all.FirstOrDefault(l => !l.name.StartsWith("Level_0"));

            if (level != null)
            {
                Undo.RecordObject(level.gameObject, "Rename Level");
                level.gameObject.name = levelName;
            }
        }

        if (level != null)
            return level;

        var root = new GameObject(levelName);
        Undo.RegisterCreatedObjectUndo(root, "Create Level");
        root.transform.position = Vector3.zero;
        return root.AddComponent<LevelController>();
    }

    private static void AssignFlow(LevelFlow flow, LevelController[] levels)
    {
        var so = new SerializedObject(flow);
        SerializedProperty list = so.FindProperty("levels");
        list.arraySize = levels.Length;
        for (int i = 0; i < levels.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];

        so.FindProperty("firstLevelIndex").intValue = 0;
        so.ApplyModifiedProperties();
    }

    private static void SetReference(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"God Tower: field '{field}' not found on {target.GetType().Name}.", target);
            return;
        }

        property.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    private static string Backing(string propertyName) => $"<{propertyName}>k__BackingField";

    private static void SetInt(SerializedObject so, string name, int value) => so.FindProperty(Backing(name)).intValue = value;

    private static void SetFloat(SerializedObject so, string name, float value) => so.FindProperty(Backing(name)).floatValue = value;

    private static void SetBool(SerializedObject so, string name, bool value) => so.FindProperty(Backing(name)).boolValue = value;

    private static void SetVector2(SerializedObject so, string name, Vector2 value) => so.FindProperty(Backing(name)).vector2Value = value;

    private static T FindAsset<T>() where T : Object
    {
        string guid = AssetDatabase.FindAssets($"t:{typeof(T).Name}").FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
