using UnityEditor;
using UnityEngine;

public static class GodTowerVfxSetup
{
    private const string Folder = "Assets/VFX";
    private const string UrpParticleMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";

    [MenuItem("Tools/God Tower/Create VFX Prefabs")]
    private static void CreateVfxPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "VFX");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(UrpParticleMaterial)
                            ?? AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

        Save(BuildContact(material), "VFX_ClimbContact");
        Save(BuildHit(material), "VFX_HitImpact");
        Save(BuildCelebration(material), "VFX_LevelComplete");

        AssetDatabase.SaveAssets();
        Debug.Log($"God Tower: VFX prefabs created in {Folder}.");
    }

    private static GameObject BuildContact(Material material)
    {
        ParticleSystem ps = CreateSystem("VFX_ClimbContact", material, 12);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new Color(0.95f, 0.92f, 0.85f, 0.7f);
        main.gravityModifier = 0.2f;

        SetBurst(ps, 6);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        FadeOut(ps);
        return ps.gameObject;
    }

    private static GameObject BuildHit(Material material)
    {
        ParticleSystem ps = CreateSystem("VFX_HitImpact", material, 30);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.45f, 0.1f));

        SetBurst(ps, 18);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        FadeOut(ps);
        return ps.gameObject;
    }

    private static GameObject BuildCelebration(Material material)
    {
        ParticleSystem ps = CreateSystem("VFX_LevelComplete", material, 80);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.gravityModifier = 0.8f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.3f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.33f),
                new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0.66f),
                new GradientColorKey(new Color(0.5f, 1f, 0.4f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };

        SetBurst(ps, 60);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.2f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        FadeOut(ps);
        return ps.gameObject;
    }

    private static ParticleSystem CreateSystem(string name, Material material, int maxParticles)
    {
        var go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.stopAction = ParticleSystemStopAction.Disable;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    private static void SetBurst(ParticleSystem ps, int count)
    {
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    private static void FadeOut(ParticleSystem ps)
    {
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
    }

    private static void Save(GameObject go, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(go, $"{Folder}/{name}.prefab");
        Object.DestroyImmediate(go);
    }
}
