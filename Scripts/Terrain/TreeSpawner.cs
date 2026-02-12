using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [SerializeField] private ChunkManager chunkManager;
    public static TreeSpawner Instance { get; private set; }

    [SerializeField] private int treesPerChunk = 200;
    [SerializeField] private int psoIterations = 50;

    [Header("PSO Parameters")]
    [SerializeField] private float alpha = 0.5f;
    [SerializeField] private float beta = 1.0f;
    [SerializeField] private float gamma = 50f;
    [SerializeField] private float inertiaStart = 0.6f;
    [SerializeField] private float inertiaEnd = 0.1f;
    
    [Header("Fitness Parameters")]
    [SerializeField, Range(0f, 1f)] private float idealElevation = 0.5f;
    [SerializeField, Range(0.01f, 1f)] private float elevationTolerance = 0.25f;
    [SerializeField, Range(1f, 100f)] private float slopeSharpness = 20f;
    [SerializeField, Range(0f, 5f)] private float slopeWeight = 2f;
    [SerializeField, Range(0f, 5f)] private float elevationWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float minFitnessThreshold = 0.4f;
    [SerializeField, Range(0f, 1f)] private float maxAllowedSlope = 0.15f;

    [Header("Forest Clustering Parameters")]
    [SerializeField] private bool useClusteredPlacement = true;
    [SerializeField, Range(0.1f, 10f)] private float clusteringStrength = 2f;
    [SerializeField, Range(0.5f, 10f)] private float minSpacing = 1.5f;


    public int TreesPerChunk { get => treesPerChunk; set => treesPerChunk = value; }
    public int PsoIterations { get => psoIterations; set => psoIterations = value; }
    public float Alpha { get => alpha; set => alpha = value; }
    public float Beta { get => beta; set => beta = value; }
    public float Gamma { get => gamma; set => gamma = value; }
    public float InertiaStart { get => inertiaStart; set => inertiaStart = value; }
    public float InertiaEnd { get => inertiaEnd; set => inertiaEnd = value; }
    public float IdealElevation { get => idealElevation; set => idealElevation = value; }
    public float ElevationTolerance { get => elevationTolerance; set => elevationTolerance = value; }
    public float SlopeSharpness { get => slopeSharpness; set => slopeSharpness = value; }
    public float SlopeWeight { get => slopeWeight; set => slopeWeight = value; }
    public float ElevationWeight { get => elevationWeight; set => elevationWeight = value; }

    //------------------TREE MODELS-------------------
    const int Tree_ClusteredMini = 0;
    const int Tree_Default = 1;
    const int Tree_TallThin = 2;
    const int Tree_ShortDense = 3;

    [Header("Simulation Results")]
    public List<int> psoBadPlacements = new List<int>();
    public List<float> psoAvgDistances = new List<float>();
    public List<int> psoTotalAttempts = new List<int>();

    public List<int> perlinBadPlacements = new List<int>();
    public List<float> perlinAvgDistances = new List<float>();
    public List<int> perlinTotalAttempts = new List<int>();

    void Awake()
    {
        Instance = this; //initialize before start is called for chunkmanager
    }

    void Start()
    {
        chunkManager = FindObjectOfType<ChunkManager>();
    }

    void Update()
    {
        //SpawnTrees();
    }

    public void RunPSOPlacement()
    {
        ClearTrees();
        SpawnTrees();
    }

    public void RunPerlinPlacement()
    {
        ClearTrees();
        SpawnPerlinTrees();
    }

    private void ClearTrees()
    {
        if (chunkManager == null) return;
        foreach (var chunk in chunkManager.ActiveChunks)
        {
            if (chunk.HasSpawnedTrees)
            {
                chunk.ReturnTreesToPool();
            }
        }
    }

    public void SetClusteredPlacement(bool value)
    {
        useClusteredPlacement = value;
    }

    void SpawnTrees()
    {

        if (chunkManager == null) return;

        foreach (var chunk in chunkManager.ActiveChunks)
        {
            if (chunk.HasSpawnedTrees || !chunk.Chunk.gameObject.activeSelf) //spawns only on empty or visible chunks
                continue;

            var chunkScript = chunk.Chunk;
            float[,] heightMap = chunkScript.heightMap; //get terrain heightmap
            Vector2 min = chunk.WorldPosition;
            Vector2 max = min + new Vector2(chunk.Size, chunk.Size); //det bounds

            float[,] localMap = heightMap;

            //base fitness based on slope steepness and elevation toleration
            var fitnessEvaluator = new FitnessEvaluator(heightMap, min, idealElevation, elevationTolerance,
                                                        slopeSharpness, slopeWeight, elevationWeight, minFitnessThreshold, maxAllowedSlope);

            System.Func<Vector2, float> finalFitness;

            if (useClusteredPlacement)
            {
                //run pso without leader for 10 iterations
                PSOManager warmup = new PSOManager(min, max, fitnessEvaluator.BasicEvaluate)
                {
                    NumParticles = treesPerChunk,
                    MaxIterations = 10,
                    Alpha = alpha,
                    Beta = beta,
                    Gamma = 1f,
                    InertiaStart = inertiaStart,
                    InertiaEnd = inertiaEnd
                };

                List<Vector2> warmupResults = warmup.Run();

                //choose leader based on best fitness
                Vector2 leader = warmupResults[0];
                float bestScore = fitnessEvaluator.BasicEvaluate(leader);
                foreach (var pos in warmupResults)
                {
                    float score = fitnessEvaluator.BasicEvaluate(pos);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        leader = pos;
                    }
                }

                //fitness function for tight clusters 
                finalFitness = (Vector2 pos) => fitnessEvaluator.ClusteredEvaluate(pos, leader, clusteringStrength);
            }
            else 
            {
                finalFitness = fitnessEvaluator.BasicEvaluate;
            }

            //run pso with the selected final fitness
            PSOManager pso = new PSOManager(min, max, finalFitness);
            pso.NumParticles = treesPerChunk;
            pso.MaxIterations = psoIterations;
            pso.Alpha = alpha;
            pso.Beta = beta;
            pso.Gamma = gamma;
            pso.InertiaStart = inertiaStart;
            pso.InertiaEnd = inertiaEnd;

            List<Vector2> positions = pso.Run();
            int totalAttempts = treesPerChunk * psoIterations;
            psoTotalAttempts.Add(totalAttempts);

            int neighborsToCheck = 5; 
            float maxNeighborDist = 20f;

            int badPlacementCount = 0;
            float totalAvgDist = 0;
            int validTrees = 0;

            foreach (Vector2 pos in positions)
            {
                var (tooClose, avgDist) = AnalyzeNeighborhood(pos, positions, minSpacing, neighborsToCheck);
                if (tooClose) continue;
                if (fitnessEvaluator.BasicEvaluate(pos) < minFitnessThreshold)
                {
                    badPlacementCount++;
                    continue; 
                }
                totalAvgDist += avgDist;
                validTrees++;
                float normalized = Mathf.Clamp01(avgDist / maxNeighborDist);


                float y = SampleHeightFromMap(heightMap, pos.x - min.x, pos.y - min.y);
                Vector3 worldPos = new Vector3(pos.x, y, pos.y);
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                //choose index based on position and closeness to other trees
                int selectedIndex;
                if (avgDist < 3f) selectedIndex = Tree_ClusteredMini;
                else if (avgDist < 6f) selectedIndex = Tree_ShortDense;
                else if (avgDist < 10f) selectedIndex = Tree_Default;
                else selectedIndex = Tree_TallThin;
                GameObject selectedTree = chunkScript.treePrefabs[selectedIndex];

                GameObject tree = Instantiate(selectedTree, worldPos, rotation);
                tree.transform.localScale = Vector3.one * 2f;
                tree.transform.parent = chunkScript.transform;
                chunk.AddSpawnedTree(tree);
            }
            psoBadPlacements.Add(badPlacementCount);
            psoAvgDistances.Add(totalAvgDist / validTrees);

            chunk.MarkTreesSpawned();
        }
    }



    float SampleHeightFromMap(float[,] map, float x, float z)
    {
        int xi = Mathf.Clamp(Mathf.FloorToInt(x), 0, map.GetLength(0) - 1);
        int zi = Mathf.Clamp(Mathf.FloorToInt(z), 0, map.GetLength(1) - 1);
        return map[xi, zi];
    }

    public void RegenerateTrees()
    {
        if (chunkManager == null)
        {
            //Debug.LogError("chunkManager is null in RegenerateTrees!");
            return;
        }

        foreach (var chunk in chunkManager.ActiveChunks)
        {
            if (chunk != null && chunk.HasSpawnedTrees)
            {
                chunk.ReturnTreesToPool();
            }
        }

        SpawnTrees();
    }

    (bool tooClose, float avgDist) AnalyzeNeighborhood(Vector2 pos, List<Vector2> others, float minDist, int k)
    {
        List<float> dists = new();
        foreach (var other in others)
        {
            if (Vector2.Distance(pos, other) < 0.01f) continue;
            float dist = Vector2.Distance(pos, other);
            dists.Add(dist);
        }

        dists.Sort();

        //compute average distance to k nearest neighbors
        float avgDist = 0f;
        for (int i = 0; i < Mathf.Min(k, dists.Count); i++)
        {
            avgDist += dists[i];
        }
        avgDist /= Mathf.Max(1, Mathf.Min(k, dists.Count));

        //check if too close to any neighbor
        bool tooClose = dists.Count > 0 && dists[0] < minDist;
        //Debug.Log($"[AnalyzeNeighborhood] pos=({pos.x:F1}, {pos.y:F1}) | avgDist={avgDist:F2} | tooClose={tooClose} | count={dists.Count} | minDist={(dists.Count > 0 ? dists[0].ToString("F2") : "n/a")}");


        return (tooClose, avgDist);
    }

    public void SpawnPerlinTrees()
    {
        if (chunkManager == null)
        {
            chunkManager = FindObjectOfType<ChunkManager>();
            if (chunkManager == null)
            {
                //Debug.LogError("chunkManager is STILL null in SpawnPerlinTrees!");
                return;
            }
        }

        foreach (var chunk in chunkManager.ActiveChunks)
        {
            if (chunk.HasSpawnedTrees || !chunk.Chunk.gameObject.activeSelf)
                continue;

            var chunkScript = chunk.Chunk;
            float[,] heightMap = chunkScript.heightMap;
            Vector2 min = chunk.WorldPosition;
            Vector2 max = min + new Vector2(chunk.Size, chunk.Size);

            //var fitnessEvaluator = new FitnessEvaluator(heightMap, min, idealElevation, elevationTolerance,
                         //                                slopeSharpness, slopeWeight, elevationWeight, minFitnessThreshold, maxAllowedSlope);

            int validCount = 0;
            int totalAttempts = 0;
            //generate all positions 
            List<Vector2> allPositions = new List<Vector2>();
            while (validCount < treesPerChunk && totalAttempts < 50000)
            {
                float x = Random.Range(min.x, max.x);
                float z = Random.Range(min.y, max.y);
                Vector2 pos = new Vector2(x, z);
                totalAttempts++;
                allPositions.Add(pos);
                validCount++;

                //// Check fitness before adding to the list
                //if (fitnessEvaluator.BasicEvaluate(pos) >= minFitnessThreshold)
                //{
                //    allPositions.Add(pos);
                //    validCount++;
                //}
            }

            int badPlacementCount = 0;
            float totalAvgDist = 0;
            int validTrees = 0;
            int neighborsToCheck = 5;
            float minSpacing = 1.5f;

            foreach (Vector2 pos in allPositions)
            {
                var (tooClose, avgDist) = AnalyzeNeighborhood(pos, allPositions, minSpacing, neighborsToCheck);

                // Check for overlap, but we already know fitness is good
                if (tooClose)
                {
                    badPlacementCount++; // Reusing this counter to track overlaps
                    continue;
                }

                totalAvgDist += avgDist;
                validTrees++;

                float y = SampleHeightFromMap(heightMap, pos.x - min.x, pos.y - min.y);
                Vector3 worldPos = new Vector3(pos.x, y, pos.y);
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                //choose index based on position and closeness to other trees
                int selectedIndex;
                if (avgDist < 3f) selectedIndex = Tree_ClusteredMini;
                else if (avgDist < 6f) selectedIndex = Tree_ShortDense;
                else if (avgDist < 10f) selectedIndex = Tree_Default;
                else selectedIndex = Tree_TallThin;
                GameObject selectedTree = chunkScript.treePrefabs[selectedIndex];

                GameObject tree = Instantiate(selectedTree, worldPos, rotation);
                tree.transform.localScale = Vector3.one * 2f;
                tree.transform.parent = chunkScript.transform;
                chunk.AddSpawnedTree(tree);
            }

            perlinBadPlacements.Add(badPlacementCount);
            perlinAvgDistances.Add(totalAvgDist / validTrees);

            perlinTotalAttempts.Add(totalAttempts);

            chunk.MarkTreesSpawned();
        }
    }

}
