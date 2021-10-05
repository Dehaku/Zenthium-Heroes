using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPosition : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello world");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Update: " + transform.TransformPoint(transform.position));

    }

    private void FixedUpdate()
    {
        //Debug.Log("Fixed Update: " + transform.TransformPoint(transform.position));
    }
    private void LateUpdate()
    {
        // Debug.Log("Late Update: " + transform.TransformPoint(transform.position));
        //Debug.Log("Late Update: " + transform.position);
        Debug.Log("Late Update: " + (transform.position - transform.root.position) );
    }
}
