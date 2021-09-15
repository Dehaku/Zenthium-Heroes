using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshUpdater : MonoBehaviour
{
    public NavMeshSurface surface;

    [SerializeField] bool pulseUpdate = false;
    [SerializeField] bool autoUpdate = false;
    [SerializeField] float updateTimer = 1f;
    float updateTime = 0;

    // Start is called before the first frame update
    void Start()
    {
        surface.BuildNavMesh();
    }

    // Update is called once per frame
    void Update()
    {
        updateTime += Time.deltaTime;

        if(autoUpdate && updateTime >= updateTimer || pulseUpdate)
        {
            surface.BuildNavMesh();
            updateTime = 0;
            pulseUpdate = false;
        }
            
    }
}
