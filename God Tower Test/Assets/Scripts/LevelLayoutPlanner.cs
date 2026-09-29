using System.Collections.Generic;
using UnityEngine;

public enum ObstacleKind { Ledge, LedgePair, Arm, TwoArm, PopOut, PopOutPair }

public struct ObstaclePlacement
{
    public ObstacleKind Kind;
    public int RestIndex;
    public float AngleA;
    public float AngleB;
    public float RotationSpeed;
    public float DelayA;
    public float DelayB;
    public float HoldDuration;
    public float WaitDuration;
}

public static class LevelLayoutPlanner
{
    private const int PlacementAttempts = 4;

    public static List<ObstaclePlacement> Plan(LevelBlueprint blueprint, int fallJumps)
    {
        var rng = new System.Random(blueprint.Seed);
        var placements = new List<ObstaclePlacement>();
        var dangers = new HashSet<int>();
        var landings = new HashSet<int>();

        int nextIndex = blueprint.IntroJumps;
        int lastIndex = blueprint.JumpCount - blueprint.OutroJumps;
        int minGap = Mathf.Min(blueprint.MinGapJumps, blueprint.MaxGapJumps);
        int maxGap = Mathf.Max(blueprint.MinGapJumps, blueprint.MaxGapJumps);
        float lastLedgeAngle = 0f;
        bool hasLedge = false;

        while (nextIndex <= lastIndex)
        {
            ObstacleKind kind = PickKind(blueprint, rng);
            bool placed = false;

            for (int attempt = 0; attempt < PlacementAttempts && nextIndex + attempt <= lastIndex; attempt++)
            {
                int index = nextIndex + attempt;
                GetFootprint(kind, index, fallJumps, out int danger, out int landingA, out int landingB);

                if (danger == 0 || dangers.Contains(danger) || landings.Contains(danger) ||
                    dangers.Contains(landingA) || dangers.Contains(landingB))
                    continue;

                placements.Add(CreatePlacement(kind, index, blueprint, rng, ref lastLedgeAngle, ref hasLedge));
                dangers.Add(danger);
                landings.Add(landingA);
                landings.Add(landingB);
                nextIndex = index + rng.Next(minGap, maxGap + 1);
                placed = true;
                break;
            }

            if (!placed)
                nextIndex++;
        }

        return placements;
    }

    private static bool IsLedge(ObstacleKind kind) => kind == ObstacleKind.Ledge || kind == ObstacleKind.LedgePair;

    private static void GetFootprint(ObstacleKind kind, int index, int fallJumps, out int danger, out int landingA, out int landingB)
    {
        if (IsLedge(kind))
        {
            danger = index + 1;
            landingA = landingB = Mathf.Max(index - fallJumps, 0);
            return;
        }

        danger = index;
        landingA = Mathf.Max(index - fallJumps, 0);
        landingB = Mathf.Max(index - 1 - fallJumps, 0);
    }

    private static ObstacleKind PickKind(LevelBlueprint b, System.Random rng)
    {
        float[] weights =
        {
            b.LedgeWeight, b.LedgePairWeight, b.ArmWeight, b.TwoArmWeight, b.PopOutWeight, b.PopOutPairWeight
        };

        float total = 0f;
        foreach (float weight in weights)
            total += weight;

        if (total <= 0f)
            return ObstacleKind.Ledge;

        float roll = (float)rng.NextDouble() * total;
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0f)
                return (ObstacleKind)i;
        }

        return ObstacleKind.Ledge;
    }

    private static ObstaclePlacement CreatePlacement(ObstacleKind kind, int index, LevelBlueprint b, System.Random rng,
        ref float lastLedgeAngle, ref bool hasLedge)
    {
        var placement = new ObstaclePlacement { Kind = kind, RestIndex = index };

        switch (kind)
        {
            case ObstacleKind.Ledge:
                placement.AngleA = hasLedge ? lastLedgeAngle + Sign(rng) * Range(rng, 90f, 150f) : 0f;
                lastLedgeAngle = placement.AngleA;
                hasLedge = true;
                break;

            case ObstacleKind.LedgePair:
                placement.AngleA = lastLedgeAngle + Sign(rng) * Range(rng, 60f, 120f);
                placement.AngleB = placement.AngleA + Sign(rng) * Range(rng, 110f, 150f);
                lastLedgeAngle = placement.AngleA;
                hasLedge = true;
                break;

            case ObstacleKind.Arm:
            case ObstacleKind.TwoArm:
                float speed = Range(rng, b.ArmSpeedRange.x, b.ArmSpeedRange.y);
                if (kind == ObstacleKind.TwoArm)
                    speed *= b.TwoArmSpeedScale;
                placement.RotationSpeed = b.AllowReverseRotation ? Sign(rng) * speed : speed;
                placement.AngleA = Range(rng, 0f, 360f);
                break;

            case ObstacleKind.PopOut:
            case ObstacleKind.PopOutPair:
                placement.HoldDuration = Range(rng, b.PopHoldRange.x, b.PopHoldRange.y);
                placement.WaitDuration = Range(rng, b.PopWaitRange.x, b.PopWaitRange.y);
                float cycle = b.PopExtensionDuration + placement.HoldDuration + b.PopRetractionDuration + placement.WaitDuration;
                placement.AngleA = Mathf.Round(Range(rng, 0f, 360f) / 15f) * 15f;
                placement.AngleB = placement.AngleA + Sign(rng) * Range(rng, 60f, 110f);
                placement.DelayA = Range(rng, 0f, cycle);
                placement.DelayB = placement.DelayA + cycle * 0.5f;
                break;
        }

        placement.AngleA = Mathf.Repeat(placement.AngleA, 360f);
        placement.AngleB = Mathf.Repeat(placement.AngleB, 360f);
        return placement;
    }

    private static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

    private static float Sign(System.Random rng) => rng.NextDouble() < 0.5 ? -1f : 1f;
}
