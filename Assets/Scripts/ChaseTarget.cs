using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChaseTarget : MonoBehaviour
{
    NavMeshAgent navMeshAgent;
    public GameObject target;
    [Tooltip("Won't chase target if they're closer than this, leave at 0 to ignore.")]
    public float MinChaseDistance = 0;
    [Tooltip("Won't chase target if they're further than this, leave at 0 to ignore.")]
    public float MaxChaseDistance = 0;
    [Tooltip("How many seconds inbetween destination updates.")]
    public float UpdateFrequency = 0.1f;
    float updateTracker = 0;

    // Start is called before the first frame update
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
            Debug.LogWarning("No nav mesh agent");
    }

    // Update is called once per frame
    void Update()
    {
        if(target == null)
        {
            // var player = FindObjectOfType<Player>();
            // if(player != null)
            //     target = player.gameObject;
        }
            
        if (target == null)
            return;
        
        updateTracker += Time.deltaTime;
        if (updateTracker < UpdateFrequency)
            return;
        updateTracker = 0;

        bool tooClose = false, tooFar = false;

        float distance = Vector3.Distance(this.transform.position, target.transform.position);
        if (distance > MaxChaseDistance && MaxChaseDistance != 0)
            tooFar = true;
        if (distance < MinChaseDistance && MinChaseDistance != 0)
            tooClose = true;

        if(tooClose || tooFar)
        {
            if (navMeshAgent.hasPath)
                navMeshAgent.ResetPath();
            return;
        }
            

        if (navMeshAgent.isOnNavMesh)
            navMeshAgent.destination = target.transform.position;
    }
}
