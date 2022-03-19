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

    Faction FindNearestEnemyBySight()
    {
        //List<GameObject> enemies = new List<GameObject>();
        List<GameObject> enemies = myTargets.AcquireAllEnemyTargets();
        List<GameObject> seenEnemies = new List<GameObject>();

        foreach (var enemy in enemies)
        {
            //Add a little height for the raytracer.
            var enemyPos = enemy.transform.position;
            enemyPos.y += rayTraceHeightOffset*enemy.transform.lossyScale.y;
            
            RaycastHit rayInfo;
            Vector3 rayDir = (enemyPos - SightPos.position).normalized;
            if(Physics.Raycast(SightPos.position,rayDir,out rayInfo))
            {
                var creature = rayInfo.collider.GetComponentInParent<Creature>();
                if(creature)
                {
                    Debug.DrawLine(SightPos.position, rayInfo.point,Color.green);
                    Debug.Log("Sight: " + rayInfo.collider.name + ":" + creature.name);
                }
                else
                {
                    Debug.DrawLine(SightPos.position, rayInfo.point, Color.red);
                }
                    
                //else
                //    Debug.Log("Sight: " + rayInfo.collider.name );
            }
        }


        return null;
    }

    // Update is called once per frame
    void Update()
    {
        //if(Random.Range(0,100) == 0)
        {
            var enemy = FindNearestEnemyBySight();
            if(enemy)
                Debug.Log("Found " + enemy.name);
            else
                Debug.Log("!Found Nothing!");
        }
    }
}
