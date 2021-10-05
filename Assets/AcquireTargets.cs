using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcquireTargets : MonoBehaviour
{
    public List<int> TargetFactions;
    
    [HideInInspector] public GameObject target;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // transform.Rotate(Vector3.up * 150f * Time.deltaTime);
        GameObject tar = AcquireNearestTarget();

        if (tar == null)
            return;
        transform.LookAt(tar.transform.position);

        Debug.DrawLine(transform.position, tar.transform.position);
    }

    GameObject AcquireNearestTarget()
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
}