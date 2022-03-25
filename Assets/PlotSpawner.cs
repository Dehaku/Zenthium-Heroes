using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotSpawner : MonoBehaviour
{
    public PlotBuildingContainer PBC;
    public GameObject myBuildings;
    [SerializeField] GameObject _plane;

    // Start is called before the first frame update
    void Start()
    {
        if(!PBC)
        {
            Debug.LogError("Plot " + name + " does not have a PBC set.");
        }
            
        if(PBC.BuildingPrefabs.Length == 0)
        {
            Debug.LogWarning("PlotBuildingContainer is empty!");
            return;
        }
        Generate();
        
        _plane.SetActive(false);
    }

    [EButton.BeginHorizontal("Buttons"),EButton]
    public void Generate()
    {
        if(myBuildings)
        {
            Debug.Log("There's already something here!");
            return;
        }
            
        GameObject item = PBC.BuildingPrefabs[Random.Range(0, PBC.BuildingPrefabs.Length)];
        myBuildings = Instantiate(item, transform.position, transform.rotation, transform);
    }

    [EButton("Clear")]
    public void Clear()
    {
        if (myBuildings)
            Destroy(myBuildings);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
