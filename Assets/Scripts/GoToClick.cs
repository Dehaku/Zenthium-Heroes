using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GoToClick : MonoBehaviour
{
    NavMeshAgent navMeshAgent;
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
        if(Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) { return; }

            navMeshAgent.destination = hit.point;
        }
    }
}
