using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Faction))]
[RequireComponent(typeof(AcquireTargets))]
public class CanSeeEnemy : MonoBehaviour
{
    public Transform SightPos;
    public float rayTraceHeightOffset = 1f;
    
    Faction myFaction;
    AcquireTargets myTargets;
    // Start is called before the first frame update
    void Start()
    {
        myFaction = GetComponent<Faction>();
        myTargets = GetComponent<AcquireTargets>();
    }


    // Update is called once per frame
    void Update()
    {
        //if(Random.Range(0,100) == 0)
        {
            //var enemy = FindNearestEnemyBySight();
            var enemy = myTargets.AcquireNearestVisibleEnemyWithinRange(SightPos.position,0,100);
            if (enemy)
                Debug.Log("Found " + enemy.name);
            else
                Debug.Log("!Found Nothing!");
        }
    }
}
