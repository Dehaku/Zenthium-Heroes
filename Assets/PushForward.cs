using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushForward : MonoBehaviour
{
    Rigidbody rb;
    public float pushPower;
    public float maxVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /*
        var rbs = GetComponentsInChildren<Rigidbody>();
        foreach (var item in rbs)
        {
            item.AddForce(transform.forward * pushPower, ForceMode.VelocityChange);
        }
        */
        
        if(rb.velocity.magnitude < maxVelocity)
            rb.AddForce(transform.forward * pushPower);
        
    }
}
