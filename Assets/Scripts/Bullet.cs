using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private IEnumerator WaitToDelete(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var myRigid = GetComponent<Rigidbody>();

        var hit = collision.gameObject;

        Rigidbody[] rigidbodies;
        rigidbodies = hit.GetComponentsInChildren<Rigidbody>();
        foreach (var rigid in rigidbodies)
        {
            rigid.velocity += transform.forward * (speed / rigid.mass);   //new Vector3(0, speed, 0);
        }

        StartCoroutine(WaitToDelete(0.05f));
    }

    // Update is called once per frame
    void Update()
    {
        // Move the object forward at 1 unit/second.
        transform.Translate(transform.forward * speed * World.Instance.speedForce * Time.deltaTime, Space.World);
    }
}
