using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        // Move the object forward at 1 unit/second.
        //transform.Translate(transform.forward * 5 * World.Instance.speedForce * Time.deltaTime);
    }
}
