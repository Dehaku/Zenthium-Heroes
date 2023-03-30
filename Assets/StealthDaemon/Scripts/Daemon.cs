using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Daemon : MonoBehaviour
{
    public DemonType type;
    
    public Transform target;
    public Transform myGfxObj;

    public NavMeshAgent navAgent;
    public RandomWalk randomWalk;

    public bool NeedsTarget = false;

    public float pathCheckTime = 1;
    public float pathCheckTimeVariance = 0.25f;
    float _pathCheckTimer;

    public void GoTo(Vector3 pos)
    {
        randomWalk.enabled = false;
        navAgent.destination = pos;
        
        NeedsTarget = false;
    }

    void Update()
    {
        _pathCheckTimer -= Time.deltaTime;
        if (_pathCheckTimer < 0)
        {
            _pathCheckTimer = pathCheckTime + Random.Range(0,pathCheckTimeVariance);
            PathLogic();
        }
    }

    void PathLogic()
    {
        if(target)
        {
            navAgent.destination = target.position;
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        Initialize();
        DaemonBrain.AddDaemon(this);
    }

    void AssignRandomType()
    {
        int roll = Random.Range(0, 1000);
        // Hard setting it just in case I mess up the psuedo range.
        type = (DemonType)Random.Range(0, 4);


        // 10% Scout, 60% Grunt, 20% Captain, 5% Overlord, 5% Cubi

        if (roll < 100)
            type = DemonType.Scout;
        else if (roll < 700)
            type = DemonType.Grunt;
        else if (roll < 900)
            type = DemonType.Captain;
        else if (roll < 950)
            type = DemonType.Overlord;
        else if (roll < 10000)
            type = DemonType.Cubi;


    }

    void Initialize()
    {
        AssignRandomType();
        BuildDemon(type);
    }

    void BuildDemon(DemonType demonType)
    {
        if (demonType == DemonType.Scout)
        {
            myGfxObj.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }

        if (demonType == DemonType.Captain)
        {
            myGfxObj.localScale = new Vector3(1f, 2f, 1f);
        }

        if (demonType == DemonType.Overlord)
        {
            myGfxObj.localScale = new Vector3(4f, 4f, 4f);
        }
    }



    private void OnDestroy()
    {
        DaemonBrain.RemoveDaemon(this);
    }

    public enum DemonType
    {
        Scout,
        Grunt,
        Captain,
        Overlord,
        Cubi
    }
}


