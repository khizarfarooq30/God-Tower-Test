using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var generator = (LevelGenerator)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Level", GUILayout.Height(30)))
            generator.Generate();

        if (GUILayout.Button("Generate With New Seed") && generator.Blueprint != null)
        {
            Undo.RecordObject(generator.Blueprint, "Randomize Level Seed");
            generator.Blueprint.RandomizeSeed();
            EditorUtility.SetDirty(generator.Blueprint);
            generator.Generate();
        }
    }

    [MenuItem("Tools/God Tower/Generate All Levels")]
    private static void GenerateAllLevels()
    {
        var generators = Object.FindObjectsByType<LevelGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LevelGenerator generator in generators)
            generator.Generate();

        Debug.Log($"Generated {generators.Length} level(s).");
    }
}
