using UnityEngine;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    // manages multiple ChunkObject chunks

    public GameObject prefab; // template for a chunk, has ChunkObject.cs attached to it
    public Transform player;
    public TerrainSettings terrainSettings;

    private int mapChunkSize;
    public const float maxViewDist = 400;
    public static Vector2 playerPosition;
    public int chunksActive; // grid

    public IEnumerable<TerrainChunk> ActiveChunks => activeChunks.Values; // expose only what's needed

    private Dictionary<Vector2Int, TerrainChunk> activeChunks = new(); // active chunks around player
    private List<TerrainChunk> seenChunks = new(); // all seen chunks


    /// <summary>
    /// repositions player to the center
    /// computes how many chunks should be active around it
    /// </summary>
    private void Start()
    {
        mapChunkSize = ChunkObject.chunkSize - 1;
        chunksActive = Mathf.RoundToInt(maxViewDist / mapChunkSize); // in our case 400/240=2 so we have a 5x5 grid                                                                  

        player.position = new Vector3(ChunkObject.chunkSize / 2f, ChunkObject.chunkSize / 2f - 100f, ChunkObject.chunkSize / 2f);
    }

    /// <summary>
    /// this method is called once per frame
    /// updates player position and trigger chunk updates
    /// </summary>
    private void Update()
    {
        playerPosition = new Vector2(player.position.x, player.position.z);
        UpdateActiveChunks();
    }

    /// <summary>
    /// activates or reuses terrain chunks in a grid around the player
    /// spawns new chunks or updates existing ones based on position
    /// </summary>
    void UpdateActiveChunks()
    {
        for (int i = 0; i < seenChunks.Count; i++)
        {
            seenChunks[i].SetVisible(false);
        }
        seenChunks.Clear();

        // we get the coords of the player
        int currentChunkX = Mathf.RoundToInt(playerPosition.x / mapChunkSize);
        int currentChunkZ = Mathf.RoundToInt(playerPosition.y / mapChunkSize);

        // we iterate through the chunks around the player
        for (int zOffset = -chunksActive; zOffset <= chunksActive; zOffset++) //5x5 grid -- 25 chunks instantiated around the player initially 
        {
            for (int xOffset = -chunksActive; xOffset <= chunksActive; xOffset++)
            {
                Vector2Int viewedChunkCoord = new Vector2Int(currentChunkX + xOffset, currentChunkZ + zOffset);

                if (activeChunks.ContainsKey(viewedChunkCoord)) // if the chunk is active
                {
                    var chunk = activeChunks[viewedChunkCoord];
                    chunk.UpdateTerrainChunk();

                    //if (chunk.ShouldBeDestroyed) { chunk.DestroyChunk(); activeChunks.Remove(viewedChunkCoord); continue; }

                    if (chunk.IsVisible())
                    {
                        seenChunks.Add(chunk);
                    }
                }
                else
                {
                    TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, this);
                    activeChunks.Add(viewedChunkCoord, newChunk);
                    newChunk.UpdateTerrainChunk();
                    if (newChunk.IsVisible())
                    {
                        seenChunks.Add(newChunk);
                    }
                }
            }
        }
    }

    public List<TerrainChunk> GetVisibleChunks()
    {
        return seenChunks;
    }

    /// <summary>
    /// wrapper for each chunk
    /// handles chunk lifecycle and tracks its state
    /// </summary>
    public class TerrainChunk
    {
        GameObject meshObject; // unity object of the terrain chunk
        Vector2 position; // bottom-left corner world coords
        private ChunkObject chunkScript; // responsible for generating mesh from noise and heightmap
        public ChunkObject Chunk => chunkScript;
        public Vector2 WorldPosition => position;
        public float Size => ChunkObject.chunkSize;
        public bool ShouldBeDestroyed { get; private set; } = false;

        public TerrainChunk(Vector2 coord, ChunkManager manager)
        {
            position = coord * manager.mapChunkSize;

            Vector3 positionVec = new Vector3(position.x, 0, position.y);
            meshObject = GameObject.Instantiate(manager.prefab);
            meshObject.transform.position = positionVec;

            chunkScript = meshObject.GetComponent<ChunkObject>();
            if (chunkScript != null)
            {
                chunkScript.GenerateChunk(manager.terrainSettings);
            }

            SetVisible(false);
        }

        /// <summary>
        /// manage chunk visibility
        /// if the chunk is too far or out of view, it is deactivated
        /// </summary>
        public void UpdateTerrainChunk()
        {
            Vector2 chunkCenter = position + Vector2.one * ChunkObject.chunkSize / 2f;
            float distance = Vector2.Distance(playerPosition, chunkCenter);
            if (distance > maxViewDist)
            {
                if (IsVisible())
                    SetVisible(false);
                ShouldBeDestroyed = true;
                return;
            }


            // frustum culling
            Bounds chunkBounds = new Bounds(
                new Vector3(position.x + ChunkObject.chunkSize / 2f, 50f, position.y + ChunkObject.chunkSize / 2f),
                new Vector3(ChunkObject.chunkSize, 100f, ChunkObject.chunkSize)
            );

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            bool visible = GeometryUtility.TestPlanesAABB(frustumPlanes, chunkBounds);
            if (IsVisible() != visible)
            {
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            bool wasVisible = meshObject.activeSelf;

            if (wasVisible == visible)
                return;

            meshObject.SetActive(visible);

            if (!visible && IsVisible())
            {
                ReturnTreesToPool();
            }

        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

        public void DestroyChunk()
        {
            ReturnTreesToPool();
            GameObject.Destroy(meshObject);
        }



        // tree pool handling SECTION -----------------------------------------------------
        public bool HasSpawnedTrees { get; private set; } = false;
        private List<GameObject> spawnedTrees = new();

        public void MarkTreesSpawned()
        {
            HasSpawnedTrees = true;
            //Debug.Log("Marked trees as spawned.");
        }

        public void AddSpawnedTree(GameObject tree)
        {
            spawnedTrees.Add(tree);
            //Debug.Log($"Tree added to spawned list: {tree.name} (Total: {spawnedTrees.Count})");
        }

        public void ReturnTreesToPool()
        {
            //Debug.Log($"Returning {spawnedTrees.Count} trees to pool...");
            foreach (var tree in spawnedTrees)
            {
                //Debug.Log($"Destroying tree: {tree.name}");
                GameObject.Destroy(tree);
            }
            spawnedTrees.Clear();
            HasSpawnedTrees = false;
            //Debug.Log("All trees returned to pool and cleared.");
        }

    }

    public void RegenerateChunks()
    {
        //Debug.Log("Regenerating all seen chunks...");
        foreach (var chunk in seenChunks)
        {
            //Debug.Log($"Regenerating chunk at position: {chunk.Chunk.transform.position}");
            chunk.Chunk.GenerateChunk(terrainSettings);
        }
       // Debug.Log("Chunk regeneration complete.");
    }

    public void RegenerateTrees()
    {
        TreeSpawner.Instance.RegenerateTrees();
    }

    public void RegenerateAll()
    {
        //Debug.Log("Starting full regeneration (chunks + trees)...");
        RegenerateChunks();
        TreeSpawner.Instance.RegenerateTrees();
        //Debug.Log("Full regeneration complete.");
    }


}
