using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerrainUI : MonoBehaviour
{
    [SerializeField] private TerrainSettings terrainSettings;
    [SerializeField] private ChunkManager chunkManager;

    [Header("Sliders")]
    public Slider noiseScaleSlider;
    public TMP_InputField seedSlider;
    public Slider octavesSlider;
    public Slider persistenceSlider;
    public Slider lacunaritySlider;
    public Slider heightMultiplierSlider;

    void Start()
    {
        noiseScaleSlider.value = terrainSettings.noiseScale;
        octavesSlider.value = terrainSettings.octaves;
        persistenceSlider.value = terrainSettings.persistence;
        lacunaritySlider.value = terrainSettings.lacunarity;
        heightMultiplierSlider.value = terrainSettings.heightMultiplier;
        seedSlider.text = terrainSettings.seed.ToString();
    }

    public void OnNoiseScaleChanged(float value)
    {
        terrainSettings.noiseScale = value;
        UpdateTerrainSettings();
    }

    public void OnOctavesChanged(float value)
    {
        terrainSettings.octaves = Mathf.RoundToInt(value);
        UpdateTerrainSettings();
    }

    public void OnPersistenceChanged(float value)
    {
        terrainSettings.persistence = value;
        UpdateTerrainSettings();
    }

    public void OnLacunarityChanged(float value)
    {
        terrainSettings.lacunarity = value;
        UpdateTerrainSettings();
    }

    public void OnHeightMultiplierChanged(float value)
    {
        terrainSettings.heightMultiplier = value;
        UpdateTerrainSettings();
    }

    public void OnSeedChanged(string value)
    {
        if (int.TryParse(value, out int seed))
        {
            terrainSettings.seed = seed;
            UpdateTerrainSettings();
        }
    }

    public void OnRegenerateTreesClicked()
    {
        chunkManager.RegenerateTrees();
    }

    public void UpdateTerrainSettings()
    {
        terrainSettings.noiseScale = noiseScaleSlider.value;
        terrainSettings.octaves = Mathf.RoundToInt(octavesSlider.value);
        terrainSettings.persistence = persistenceSlider.value;
        terrainSettings.lacunarity = lacunaritySlider.value;
        terrainSettings.heightMultiplier = heightMultiplierSlider.value;

        if (int.TryParse(seedSlider.text, out int seed))
        {
            terrainSettings.seed = seed;
        }

        chunkManager.RegenerateChunks();
    }

}
