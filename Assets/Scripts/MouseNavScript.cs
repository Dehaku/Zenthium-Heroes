using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseNavScript : MonoBehaviour
{
    [SerializeField] private float maxReachDistance = Mathf.Infinity;

    // Start is called before the first frame update
    void Start()
    {
        
    }


    public void FireNavver()
    {
        

            /*
            Camera.main.ScreenPointToRay;
            Vector3 startP = playerCamera.position;
            Vector3 destP = startP + playerCamera.forward;
            Vector3 direction = destP - startP;

            Ray ray = new Ray(startP, direction);
            */
            Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxReachDistance)) { return; }
            LocalNavMeshBuilder navMesh = hit.collider.gameObject.GetComponentInChildren<LocalNavMeshBuilder>();
            if (navMesh != null)
                navMesh.WorkOnce = true;
        
    }

    // Update is called once per frame
    void Update()
    {

        /*
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            FireNavver();
        }
        */
    
    }

}
