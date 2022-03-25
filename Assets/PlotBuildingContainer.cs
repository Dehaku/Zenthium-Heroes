using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName = "PlotBuildingContainerSO", menuName = "Plots/PBC", order = 100)]
[CreateAssetMenu(fileName = "PlotBuildingContainerSO", menuName = "Plots/PBC", order = 0)]
public class PlotBuildingContainer : ScriptableObject
{
    public GameObject[] BuildingPrefabs;
}
