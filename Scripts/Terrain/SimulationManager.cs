using System.Collections;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public TreeSpawner treeSpawner;
    public int numSimulations = 10;

    [ContextMenu("Run All Simulations")]
    public void RunAllSimulations()
    {
        StartCoroutine(RunSimulationsCoroutine());
    }

    private IEnumerator RunSimulationsCoroutine()
    {
        if (treeSpawner == null)
        {
            Debug.LogError("Tree Spawner reference is not set!");
            yield break;
        }

        treeSpawner.psoBadPlacements.Clear();
        treeSpawner.psoAvgDistances.Clear();
        treeSpawner.psoTotalAttempts.Clear(); 

        treeSpawner.perlinBadPlacements.Clear();
        treeSpawner.perlinAvgDistances.Clear();
        treeSpawner.perlinTotalAttempts.Clear(); 

        Debug.Log($"--- Starting {numSimulations} Perlin Simulations ---");

        for (int i = 0; i < numSimulations; i++)
        {
            treeSpawner.RunPerlinPlacement();
            Debug.Log($"Perlin Simulation {i + 1} complete.");
            yield return new WaitForEndOfFrame();
        }

        Debug.Log($"--- Starting {numSimulations} PSO Simulations ---");
        for (int i = 0; i < numSimulations; i++)
        {
            treeSpawner.RunPSOPlacement();
            Debug.Log($"PSO Simulation {i + 1} complete.");
            yield return new WaitForEndOfFrame(); 
        }

        yield return new WaitForEndOfFrame();

        PrintResults();
    }

    private void PrintResults()
    {
        float psoAvgBad = 0;
        float psoAvgDist = 0;
        float psoAvgAttempts = 0; 

        foreach (var count in treeSpawner.psoBadPlacements)
            psoAvgBad += count;
        foreach (var dist in treeSpawner.psoAvgDistances)
            psoAvgDist += dist;
        foreach (var attempts in treeSpawner.psoTotalAttempts)
            psoAvgAttempts += attempts; 

        psoAvgBad /= treeSpawner.psoBadPlacements.Count;
        psoAvgDist /= treeSpawner.psoAvgDistances.Count;
        psoAvgAttempts /= treeSpawner.psoTotalAttempts.Count;

        float perlinAvgBad = 0;
        float perlinAvgDist = 0;
        float perlinAvgAttempts = 0; 

        foreach (var count in treeSpawner.perlinBadPlacements)
            perlinAvgBad += count;
        foreach (var dist in treeSpawner.perlinAvgDistances)
            perlinAvgDist += dist;
        foreach (var attempts in treeSpawner.perlinTotalAttempts)
            perlinAvgAttempts += attempts;

        perlinAvgBad /= treeSpawner.perlinBadPlacements.Count;
        perlinAvgDist /= treeSpawner.perlinAvgDistances.Count;
        perlinAvgAttempts /= treeSpawner.perlinTotalAttempts.Count;

        float psoTreesPlaced = 300 - psoAvgBad;
        float perlinTreesPlaced = 300 - perlinAvgBad;

        Debug.Log("\n--- Final Simulation Results ---");
        Debug.Log($"**PSO Method (over {treeSpawner.psoBadPlacements.Count} runs):**");
        Debug.Log($"   - Total Attempts: {psoAvgAttempts:F0}");
        Debug.Log($"   - Trees Placed: {psoTreesPlaced:F2} out of 300");
        Debug.Log($"   - Average Bad Placements (overlap): {psoAvgBad:F2}");
        Debug.Log($"   - Average Neighborhood Distance: {psoAvgDist:F2}");

        Debug.Log("\n**Perlin Method (over {treeSpawner.perlinBadPlacements.Count} runs):**");
        Debug.Log($"   - Total Attempts: {perlinAvgAttempts:F0}");
        Debug.Log($"   - Trees Placed: {perlinTreesPlaced:F2} out of 300");
        Debug.Log($"   - Average Bad Placements (overlap): {perlinAvgBad:F2}");
        Debug.Log($"   - Average Neighborhood Distance: {perlinAvgDist:F2}");

    }
}