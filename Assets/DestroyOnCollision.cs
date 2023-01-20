using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnCollision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello?");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider collision)
    {
        Destroy(this.gameObject);
        Debug.Log("Trigger!");
    }

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(this.gameObject);
        Debug.Log("Collision!");
    }

    
}
