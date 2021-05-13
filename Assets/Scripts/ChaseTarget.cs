using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChaseTarget : MonoBehaviour
{
    NavMeshAgent navMeshAgent;
    public GameObject target;

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
            target = FindObjectOfType<Player>().gameObject;
        
        if(navMeshAgent.isOnNavMesh)
            navMeshAgent.destination = target.transform.position;
    }
}
