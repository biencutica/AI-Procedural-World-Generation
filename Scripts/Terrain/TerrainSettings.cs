using UnityEngine;

[CreateAssetMenu(fileName = "NewTerrainSettings", menuName = "Terrain/Terrain Settings")]
public class TerrainSettings : ScriptableObject
{
    public float noiseScale = 40f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float heightMultiplier = 20f;
    public int seed = 0;
    public AnimationCurve terrainCurve = AnimationCurve.Linear(0, 0, 1, 1);
}
