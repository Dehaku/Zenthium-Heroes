using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotSpawner : MonoBehaviour
{
    public PlotBuildingContainer PBC;
    public GameObject myBuilding;
    [SerializeField] GameObject _plane;
    [Range(0f, 100f)] public float chanceOfSubplots = 0;
    [HideInInspector] public bool subPlotting = false;
    public List<GameObject> myPlots = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        if(!PBC)
        {
            Debug.LogError(name + " does not have a PBC set.");
        }
            
        if(PBC.BuildingPrefabs.Length == 0)
        {
            Debug.LogWarning(name + "PlotBuildingContainer is empty!");
            return;
        }

        if(myPlots.Count == 0)
        {
            if(chanceOfSubplots != 0)
            {
                Debug.LogWarning(name + " myPlots are not filled, but chance of Subplots is above 0!");
                return;
            }
        }

        Generate();
        
        _plane.SetActive(false);
    }

    [EButton.BeginHorizontal("Buttons"),EButton]
    public void Generate()
    {
        if(myBuilding)
        {
            // Debug.Log("There's already something here!");
            return;
        }
            
        if(chanceOfSubplots > 0 || subPlotting)
        {
            float rolledChance = Random.Range(0f, 100f);

            if(rolledChance <= chanceOfSubplots)
            {
                foreach (var subPlot in myPlots)
                {
                    subPlot.SetActive(true);
                    subPlot.GetComponent<PlotSpawner>().Generate();
                    subPlotting = true;
                }
                
                return;
            }
        }

        GameObject item = PBC.BuildingPrefabs[Random.Range(0, PBC.BuildingPrefabs.Length)];
        myBuilding = Instantiate(item, transform.position, transform.rotation, transform);
    }

    [EButton("Clear")]
    public void Clear()
    {
        if(subPlotting)
        {
            foreach (var subPlot in myPlots)
            {
                subPlot.GetComponent<PlotSpawner>().Clear();
                subPlot.SetActive(false);
                subPlotting = false;
            }
        }
        else if (myBuilding)
        {
            Destroy(myBuilding);
            myBuilding = null;
        }
            
    }

    [EButton("Clear*")]
    public void ClearInstant()
    {
        if (subPlotting)
        {
            foreach (var subPlot in myPlots)
            {
                subPlot.GetComponent<PlotSpawner>().ClearInstant();
                subPlot.SetActive(false);
                subPlotting = false;
            }
        }
        else if (myBuilding)
        {
            DestroyImmediate(myBuilding);
            myBuilding = null;
        }
            
    }

    
    
}
