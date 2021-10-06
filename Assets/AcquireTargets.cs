using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcquireTargets : MonoBehaviour
{
    public List<int> TargetFactions;
    
    [HideInInspector] public GameObject target;
    [HideInInspector] public List<GameObject> targets;


    // Start is called before the first frame update
    void Start()
    {
        
    }



    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject AcquireNearestEnemyTarget()
    {
        var pawns = FindObjectsOfType<Faction>();
        GameObject closestGO = null;
        float closestDist = float.PositiveInfinity;

        foreach (var pawn in pawns)
        {
            if(TargetFactions.Contains(pawn.GetFactionID()))
            {
                if (!closestGO)
                {
                    closestGO = pawn.gameObject;
                    closestDist = Vector3.Distance(transform.position, closestGO.transform.position);
                    continue;
                }
                
                if(Vector3.Distance(transform.position, pawn.transform.position) < closestDist)
                {
                    closestGO = pawn.gameObject;
                    closestDist = Vector3.Distance(transform.position, closestGO.transform.position);
                }
            }
        }

        return closestGO;
    }

    bool WithinRange(Vector3 myPos, Vector3 itPos, float minRange, float maxRange)
    {
        float dist = Vector3.Distance(myPos, itPos);
        if (dist > minRange && dist < maxRange)
            return true;

        return false;
    }

    bool WithinRange(GameObject target, float minRange, float maxRange)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > minRange && dist < maxRange)
            return true;

        return false;
    }

    public GameObject AcquireNearestEnemyTargetWithinRange(float minRange, float maxRange)
    {
        return AcquireNearestEnemyTargetWithinRange(transform.position, minRange, maxRange);
    }


    public GameObject AcquireNearestEnemyTargetWithinRange(Vector3 searchPoint, float minRange, float maxRange)
    {
        var pawns = FindObjectsOfType<Faction>();
        GameObject closestGO = null;
        float closestDist = float.PositiveInfinity;

        foreach (var pawn in pawns)
        {
            if (TargetFactions.Contains(pawn.GetFactionID()))
            {
                if (!closestGO && WithinRange(pawn.gameObject,minRange,maxRange))
                {
                    closestGO = pawn.gameObject;
                    closestDist = Vector3.Distance(transform.position, closestGO.transform.position);
                    continue;
                }

                if (Vector3.Distance(transform.position, pawn.transform.position) < closestDist && WithinRange(pawn.gameObject, minRange, maxRange))
                {
                    closestGO = pawn.gameObject;
                    closestDist = Vector3.Distance(transform.position, closestGO.transform.position);
                }
            }
        }

        return closestGO;
    }

    public List<GameObject> AcquireAllEnemyTargets()
    {
        List<GameObject> targetList = new List<GameObject>();
        var pawns = FindObjectsOfType<Faction>();

        foreach (var pawn in pawns)
        {
            if (TargetFactions.Contains(pawn.GetFactionID()))
            {
                targetList.Add(pawn.gameObject);
            }
        }
        return targetList;
    }
}
