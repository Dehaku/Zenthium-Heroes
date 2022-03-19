using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcquireTargets : MonoBehaviour
{
    public Transform SightPos;
    public float rayTraceHeightOffset = 1f;

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

    bool LineOfSightCheck(GameObject enemy)
    {
        var enemyPos = enemy.transform.position;
        enemyPos.y += rayTraceHeightOffset * enemy.transform.lossyScale.y;

        RaycastHit rayInfo;
        Vector3 rayDir = (enemyPos - SightPos.position).normalized;
        if (Physics.Raycast(SightPos.position, rayDir, out rayInfo))
        {
            var creature = rayInfo.collider.GetComponentInParent<Creature>();
            if (creature)
            {
                //Debug.DrawLine(SightPos.position, rayInfo.point, Color.green);
                //Debug.Log("Sight: " + rayInfo.collider.name + ":" + creature.name);
                return true;
            }
            else
            {
                //Debug.DrawLine(SightPos.position, rayInfo.point, Color.red);
            }

            //else
            //    Debug.Log("Sight: " + rayInfo.collider.name );
        }

        return false;
    }

    public GameObject AcquireNearestVisibleEnemyWithinRange(Vector3 searchPoint, float minRange, float maxRange)
    {
        var enemies = AcquireAllEnemyTargets();
        GameObject closestGO = null;
        float closestDist = float.PositiveInfinity;

        foreach (var pawn in enemies)
        {
            if (!closestGO && WithinRange(pawn.gameObject, minRange, maxRange))
            {
                if(LineOfSightCheck(pawn))
                {
                    closestGO = pawn.gameObject;
                    closestDist = Vector3.Distance(transform.position, closestGO.transform.position);
                }
                continue;
            }

            if (Vector3.Distance(transform.position, pawn.transform.position) < closestDist && WithinRange(pawn.gameObject, minRange, maxRange))
            {
                if (LineOfSightCheck(pawn))
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
