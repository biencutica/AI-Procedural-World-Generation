using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject terrainTab;
    public GameObject forestTab;

    void Start()
    {
        ShowTerrainTab(); 
    }

    public void ShowTerrainTab()
    {
        terrainTab.SetActive(true);
        forestTab.SetActive(false);
    }

    public void ShowForestTab()
    {
        terrainTab.SetActive(false);
        forestTab.SetActive(true);
    }
}
