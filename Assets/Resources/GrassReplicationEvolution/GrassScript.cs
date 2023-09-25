using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassScript : MonoBehaviour
{
    public GrassSO gene;
    public GameObject grassGO;

    public float spreadRange = 1f;
    public float currentGrowth = 0f;

    [Header("Gene")]
    public Vector3 size;
    public Color32 color;
    public float growthSpeed;
    public float growthCap;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        currentGrowth += gene.growthSpeed;
        if(currentGrowth >= gene.growthCap)
        {
            currentGrowth = Random.Range(0,60);
            Spread();
        }
    }

    void Spread()
    {
        // Position
        Vector3 spawnPos = transform.position;
        Vector2 offset = ((Vector3)Random.insideUnitCircle * spreadRange);
        spawnPos.x += offset.x;
        spawnPos.z += offset.y;
        
        // New Object
        var newGrass = Instantiate(grassGO, spawnPos, Quaternion.identity);
        
        
        // Rotation
        var euler = transform.eulerAngles;
        euler.y = Random.Range(0, 360);
        newGrass.transform.eulerAngles = euler;

    }
}
