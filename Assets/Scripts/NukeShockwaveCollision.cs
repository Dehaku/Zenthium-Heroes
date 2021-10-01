using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NukeShockwaveCollision : MonoBehaviour
{
    

    private void OnTriggerEnter(Collider other)
    {
        Knockback kb = other.GetComponent < Knockback>();
        if(kb != null)
        {
            Debug.Log(this.name + " collided with " + other.name);
            Vector3 dir = (this.transform.position - other.gameObject.transform.position).normalized;
            Vector3 dist = (this.transform.position - other.gameObject.transform.position);

            kb.ApplyKnockback(dir, 100 * dist.magnitude);
        }

            
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
            Debug.Log(this.name + " STOPPED colliding with " + other.name);

    }

    private void OnTriggerStay(Collider other)
    {
        // Debug.Log(this.name + " persist " + other.name);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
