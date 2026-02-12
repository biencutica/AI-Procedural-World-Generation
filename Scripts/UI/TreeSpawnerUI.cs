using UnityEngine;
using UnityEngine.UI;

public class TreeSpawnerUI : MonoBehaviour
{
    [SerializeField] private TreeSpawner treeSpawner;

    [Header("Sliders")]
    //public Slider treesPerChunkSlider;
    //public Slider psoIterationsSlider;
    //public Slider alphaSlider;
    //public Slider betaSlider;
    //public Slider gammaSlider;
    //public Slider inertiaStartSlider;
    //public Slider inertiaEndSlider;

    public Slider idealElevationSlider;
    public Slider elevationToleranceSlider;
    public Slider slopeSharpnessSlider;
    public Slider slopeWeightSlider;
    public Slider elevationWeightSlider;

    void Start()
    {

        //treesPerChunkSlider.value = treeSpawner.TreesPerChunk;
        //psoIterationsSlider.value = treeSpawner.PsoIterations;
        //alphaSlider.value = treeSpawner.Alpha;
        //betaSlider.value = treeSpawner.Beta;
        //gammaSlider.value = treeSpawner.Gamma;
        //inertiaStartSlider.value = treeSpawner.InertiaStart;
        //inertiaEndSlider.value = treeSpawner.InertiaEnd;

        idealElevationSlider.value = treeSpawner.IdealElevation;
        elevationToleranceSlider.value = treeSpawner.ElevationTolerance;
        slopeSharpnessSlider.value = treeSpawner.SlopeSharpness;
        slopeWeightSlider.value = treeSpawner.SlopeWeight;
        elevationWeightSlider.value = treeSpawner.ElevationWeight;

        //treesPerChunkSlider.onValueChanged.AddListener(v => { treeSpawner.TreesPerChunk = (int)v; treeSpawner.RegenerateTrees(); });
        //psoIterationsSlider.onValueChanged.AddListener(v => { treeSpawner.PsoIterations = (int)v; treeSpawner.RegenerateTrees(); });
        //alphaSlider.onValueChanged.AddListener(v => { treeSpawner.Alpha = v; treeSpawner.RegenerateTrees(); });
        //betaSlider.onValueChanged.AddListener(v => { treeSpawner.Beta = v; treeSpawner.RegenerateTrees(); });
        //gammaSlider.onValueChanged.AddListener(v => { treeSpawner.Gamma = v; treeSpawner.RegenerateTrees(); });
        //inertiaStartSlider.onValueChanged.AddListener(v => { treeSpawner.InertiaStart = v; treeSpawner.RegenerateTrees(); });
        //inertiaEndSlider.onValueChanged.AddListener(v => { treeSpawner.InertiaEnd = v; treeSpawner.RegenerateTrees(); });

        idealElevationSlider.onValueChanged.AddListener(v => { treeSpawner.IdealElevation = v; treeSpawner.RegenerateTrees(); });
        elevationToleranceSlider.onValueChanged.AddListener(v => { treeSpawner.ElevationTolerance = v; treeSpawner.RegenerateTrees(); });
        slopeSharpnessSlider.onValueChanged.AddListener(v => { treeSpawner.SlopeSharpness = v; treeSpawner.RegenerateTrees(); });
        slopeWeightSlider.onValueChanged.AddListener(v => { treeSpawner.SlopeWeight = v; treeSpawner.RegenerateTrees(); });
        elevationWeightSlider.onValueChanged.AddListener(v => { treeSpawner.ElevationWeight = v; treeSpawner.RegenerateTrees(); });
    }


}
