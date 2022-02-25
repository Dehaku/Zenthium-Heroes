using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectileScript : MonoBehaviour
{
    bool didHit = false;

    [SerializeField] float force;
    [SerializeField] float drag;

    [SerializeField] Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb.AddForce(transform.forward * force);
    }

    // Update is called once per frame
    void Update()
    {
        
        //force = Mathf.Max(0, force - drag);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (didHit)
            return;

        didHit = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.SetParent(other.transform);
        
    }

}
