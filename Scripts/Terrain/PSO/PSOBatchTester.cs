using UnityEngine;
using System.Collections;
using System.IO;

public class PSOBatchTester : MonoBehaviour
{
    public TerrainSettings terrainSettings;
    public ChunkManager chunkManager;
    public TreeSpawner treeSpawner;
    public Camera screenshotCamera;


    [Header("Simulation Settings")]
    public int simulationsToRun = 5;
    public float waitBetween = 1f;

    private int currentSimulation = 0;
    private string logPath;

    void Start()
    {
        logPath = Application.dataPath + "/PSO_Run_Log.csv";
        File.WriteAllText(logPath, "Run,Seed,Alpha,Beta,Gamma,Iterations,InertiaStart,InertiaEnd,Screenshot\n");
        StartCoroutine(RunBatch());
    }

    IEnumerator RunBatch()
    {
        for (currentSimulation = 1; currentSimulation <= simulationsToRun; currentSimulation++)
        {
            Debug.Log($"--- Running PSO Simulation #{currentSimulation} ---");

            int seed = Random.Range(0, 1000000);
            terrainSettings.seed = seed;
            terrainSettings.heightMultiplier = 30f;
            treeSpawner.PsoIterations = 50 + currentSimulation * 10;
            treeSpawner.Alpha = 0.5f + 0.2f * currentSimulation;
            treeSpawner.Beta = 1.0f + 0.2f * currentSimulation;
            treeSpawner.Gamma = 20f + 10f * currentSimulation;

            chunkManager.RegenerateAll(); //regenerate terrain + trees

            yield return new WaitForSeconds(waitBetween); //wait for spawning

            string screenshotName = $"PSO_Simulation_{currentSimulation}.png";
            CaptureFromCamera(screenshotCamera, screenshotName);
            Debug.Log($"Captured screenshot: {screenshotName}");

            string line = $"{currentSimulation},{seed},{treeSpawner.Alpha},{treeSpawner.Beta},{treeSpawner.Gamma},{treeSpawner.PsoIterations},{treeSpawner.InertiaStart},{treeSpawner.InertiaEnd},{screenshotName}";
            File.AppendAllText(logPath, line + "\n");

            yield return new WaitForSeconds(0.5f); //small delay before next
        }

        Debug.Log("✅ All PSO simulations complete.");
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
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log($"Saved screenshot to {path}");
    }

}
