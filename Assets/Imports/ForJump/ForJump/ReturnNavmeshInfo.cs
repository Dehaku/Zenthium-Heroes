using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ReturnNavmeshInfo : MonoBehaviour
{
    NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public bool IsOnNavMesh()
    {
        if(agent.isOnNavMesh)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public Vector3 ReturnClosestPointBackToAgent(Vector3 agentPosition)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(agentPosition, path);
        var endPointIndex = path.corners.Length -1 ;
        return path.corners[endPointIndex];
    }


}
