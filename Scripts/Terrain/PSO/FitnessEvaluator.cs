using UnityEngine;

public class FitnessEvaluator
{
    private readonly float[,] heightMap;
    private readonly Vector2 chunkOffset;
    private readonly float idealElevation, elevationTolerance;
    private readonly float slopeSharpness, slopeWeight, elevationWeight, minFitnessThreshold, maxAllowedSlope;

    public FitnessEvaluator(float[,] heightMap, Vector2 chunkOffset, float idealElevation, float elevationTolerance, float slopeSharpness, float slopeWeight, float elevationWeight, float minFitnessThreshold, float maxAllowedSlope)
    {
        this.heightMap = heightMap;
        this.chunkOffset = chunkOffset;
        this.idealElevation = idealElevation;
        this.elevationTolerance = elevationTolerance;
        this.slopeSharpness = slopeSharpness;
        this.slopeWeight = slopeWeight;
        this.elevationWeight = elevationWeight;
        this.minFitnessThreshold = minFitnessThreshold;
        this.maxAllowedSlope = maxAllowedSlope;
    }

    public float BasicEvaluate(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x - chunkOffset.x);
        int z = Mathf.FloorToInt(pos.y - chunkOffset.y);

        if (x < 1 || z < 1 || x >= heightMap.GetLength(0) - 1 || z >= heightMap.GetLength(1) - 1)
            return 0f;

        float h = heightMap[x, z];
        float dx = (heightMap[x + 1, z] - heightMap[x - 1, z]) * 0.5f; //central diff approximation -> diff between points ahead and behind / distance
        float dz = (heightMap[x, z + 1] - heightMap[x, z - 1]) * 0.5f; //approximation, as height map is a discrete grid (no in-between values)
        float slope = Mathf.Sqrt(dx * dx + dz * dz); //slope formula -> how fast height changes in x and z directions;

        float slopeScore = 1f - Mathf.Clamp01(slope * slopeSharpness); //near 1 -> flat slope (good)
        float elevationScore = 1f - Mathf.Clamp01(Mathf.Abs(h - idealElevation) / elevationTolerance); // near 1 -> near idealElevation

        float fitness = (slopeScore * slopeWeight + elevationScore * elevationWeight) / (slopeWeight + elevationWeight); 
        return (fitness < minFitnessThreshold) ? 0f : fitness;
    }

    public float ClusteredEvaluate(Vector2 pos, Vector2 leader, float clusteringStrength)
    {
        float baseScore = BasicEvaluate(pos);
        float distToLeader = Vector2.Distance(pos, leader);
        float clustering = Mathf.Exp(-distToLeader / clusteringStrength); // tight cluster
        return baseScore * clustering;
    }
}