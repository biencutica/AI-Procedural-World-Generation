using UnityEngine;
using System.Collections;
using System.IO;

public class PlacementComparer : MonoBehaviour
{
    public TerrainSettings terrainSettings;
    public ChunkManager chunkManager;
    public TreeSpawner treeSpawner;
    public Camera screenshotCamera;
    public int totalRuns = 5;
    public float waitTime = 1.2f;

    private string logPath;

    void Start()
    {
        logPath = Application.dataPath + "/Perlin_vs_PSO_Log.csv";
        File.WriteAllText(logPath, "Run,Seed,Mode,Alpha,Beta,Gamma,Iterations,InertiaStart,InertiaEnd,Screenshot\n");
        StartCoroutine(RunComparison());
    }

    IEnumerator RunComparison()
    {
        for (int run = 1; run <= totalRuns; run++)
        {
            int seed = Random.Range(0, 1000000);
            terrainSettings.seed = seed;

            Debug.Log($"--- Run #{run} | Seed = {seed} ---");

            treeSpawner.RunPerlinPlacement(); 

            yield return new WaitForSeconds(waitTime);

            string perlinShot = $"Run_{run}_Perlin.png";
            CaptureFromCamera(screenshotCamera, perlinShot);
            LogRun(run, seed, "Perlin",
                treeSpawner.Alpha, treeSpawner.Beta, treeSpawner.Gamma,
                treeSpawner.PsoIterations, treeSpawner.InertiaStart, treeSpawner.InertiaEnd,
                perlinShot);

            yield return new WaitForSeconds(0.5f);

            treeSpawner.RunPSOPlacement();

            yield return new WaitForSeconds(waitTime);

            string psoShot = $"Run_{run}_PSO.png";
            CaptureFromCamera(screenshotCamera, psoShot);
            LogRun(run, seed, "PSO",
                treeSpawner.Alpha, treeSpawner.Beta, treeSpawner.Gamma,
                treeSpawner.PsoIterations, treeSpawner.InertiaStart, treeSpawner.InertiaEnd,
                psoShot);

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("All Perlin vs. PSO comparisons completed.");
    }

    void CaptureFromCamera(Camera cam, string filename)
    {
        int width = 1024;
        int height = 1024;
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;

        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string path = Application.dataPath + "/" + filename;
        File.WriteAllBytes(path, bytes);
        Debug.Log($"📸 Saved: {path}");
    }

    void LogRun(int run, int seed, string mode, float alpha, float beta, float gamma, int iterations, float wStart, float wEnd, string screenshot)
    {
        string line = $"{run},{seed},{mode},{alpha},{beta},{gamma},{iterations},{wStart},{wEnd},{screenshot}";
        File.AppendAllText(logPath, line + "\n");
    }
}