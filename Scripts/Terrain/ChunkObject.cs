using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ChunkObject : MonoBehaviour
{   //handles one single chunk
    [SerializeField] private ComputeShader noiseShader;

    public static int chunkSize = 241; // size of each chunk

    public List<GameObject> treePrefabs; // assign different tree types in the Inspector

    public float[,] heightMap { get; private set; }

    public void GenerateChunk(TerrainSettings settings)
    {
        float offsetX = transform.position.x;
        float offsetY = transform.position.z;

        heightMap = ProceduralHelper.GenerateNoiseMap(chunkSize, chunkSize, settings.noiseScale, settings.seed, settings.octaves, settings.persistence, settings.lacunarity, offsetX, offsetY, settings.heightMultiplier, settings.terrainCurve);

        Mesh mesh = ProceduralHelper.GenerateTerrainMesh(heightMap);
        GetComponent<MeshFilter>().mesh = mesh;
        var meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        Texture2D elevationTex = GenerateElevationTexture(heightMap);
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material.mainTexture = elevationTex;

        //PlaceTrees();
    }

    private void PlaceTrees()
    {
        System.Random rng = new System.Random();

        for (int x = 0; x < chunkSize; x += 2) //misses potential spots
        {
            for (int z = 0; z < chunkSize; z += 2)
            {
                if (heightMap[x, z] > 0.2 && heightMap[x,z] < 0.5)
                {
                    float y = heightMap[x, z];
                    Vector3 position = new Vector3(x, y, z) + transform.position;

                    int treeIndex = rng.Next(treePrefabs.Count);
                    Instantiate(treePrefabs[treeIndex], position, Quaternion.identity, transform);
                }
            }
        }
    }

    private void PlaceValleyTrees()
    {
        System.Random rng = new System.Random();

        float idealElevation = 0.2f;
        float elevationTolerance = 0.25f;
        float slopeSharpness = 60f;
        float slopeWeight = 4f;
        float elevationWeight = 1f;
        float minFitnessThreshold = 0.3f;

        for (int x = 1; x < chunkSize - 1; x += 2)
        {
            for (int z = 1; z < chunkSize - 1; z += 2)
            {
                float h = heightMap[x, z];

                // approximate slope
                float dx = (heightMap[x + 1, z] - heightMap[x - 1, z]) * 0.5f;
                float dz = (heightMap[x, z + 1] - heightMap[x, z - 1]) * 0.5f;
                float slope = Mathf.Sqrt(dx * dx + dz * dz);

                float slopeScore = 1f - Mathf.Clamp01(slope * slopeSharpness);
                float elevationScore = 1f - Mathf.Clamp01(Mathf.Abs(h - idealElevation) / elevationTolerance);
                float fitness = (slopeScore * slopeWeight + elevationScore * elevationWeight) / (slopeWeight + elevationWeight);

                if (fitness < minFitnessThreshold) continue;

                Vector3 position = new Vector3(x, h, z) + transform.position;

                int treeIndex = rng.Next(treePrefabs.Count);
                Instantiate(treePrefabs[treeIndex], position, Quaternion.identity, transform);
            }
        }
    }


    public Texture2D GenerateElevationTexture(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Bilinear;


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float h = heightMap[x, y]; // should be in 0–1
                Color c = ElevationColor(h);
                texture.SetPixel(x, y, c);
            }
        }

        texture.Apply();
        return texture;
    }

    Color ElevationColor(float h)
    {
        if (h < 0.18f)
            return new Color(0.18f, 0.31f, 0.12f); // dark green grass
        else if (h < 5f)
            return new Color(0.56f, 0.40f, 0.26f); // light brown hill
        else if (h < 50f)
            return new Color(0.35f, 0.33f, 0.34f);
        else
            return new Color(1f, 1f, 1f); 
    }


}

