using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildPlatform : MonoBehaviour
{
    public LayerMask collisionMask;
    Rigidbody rb;


    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if(transform.parent)
        {
            //var 
        }
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit raycastHit;
        if (Input.GetKeyDown(KeyCode.F))
        {
            if(Physics.Raycast(transform.position, Vector3.down,
                                    out raycastHit, 3, collisionMask))
            {
                Debug.Log("Hit:" + raycastHit.collider.name);
                if (raycastHit.collider.attachedRigidbody)
                {
                    transform.parent = raycastHit.collider.attachedRigidbody.transform;
                }
                else
                    transform.parent = null;
                var MF = raycastHit.collider.transform.parent.GetComponentInParent<MoveForward>();
                if (MF)
                {
                    transform.parent = MF.transform.parent;
                }
            }
            else
                transform.parent = null;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (Physics.Raycast(transform.position, Vector3.down,
                                    out raycastHit, 3, collisionMask))
            {
                Debug.Log("Hit:" + raycastHit.collider.name);
                
                var MF = raycastHit.collider;
                if (MF)
                {
                    transform.parent = MF.transform.parent;
                }
            }
            else
                transform.parent = null;
        }




    }
    

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Touching: " + other.name);
        //transform.parent = other.transform;
    }
    private void OnTriggerStay(Collider other)
    {
        Rigidbody otherRB = other.attachedRigidbody;
        if (!otherRB)
            return;
        
        var force = otherRB.velocity;
        rb.AddForce(force);

        Debug.Log(rb.velocity + ":" + otherRB.velocity + ", Force: " + force + ", Drag: " +rb.drag);

    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Exiting: " + other.name);
        //transform.parent = null;
    }
}
