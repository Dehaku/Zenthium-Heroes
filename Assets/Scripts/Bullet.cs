using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject muzzlePrefab;
    public GameObject hitPrefab;
    public float speed = 50;
    public float knockbackForce = 50;
    // Start is called before the first frame update
    void Start()
    {
        if(muzzlePrefab != null)
        {
            var muzzleVFX = Instantiate(muzzlePrefab, transform.position, Quaternion.identity);
            muzzleVFX.transform.forward = gameObject.transform.forward;
            var psMuzzle = muzzleVFX.GetComponent<ParticleSystem>();
            if (psMuzzle != null)
                Destroy(muzzleVFX, psMuzzle.main.duration);
            else
            {
                var psChild = muzzleVFX.transform.GetChild(0).GetComponent<ParticleSystem>();
                Destroy(muzzleVFX, psChild.main.duration);
            }
        }
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
        ContactPoint contact = collision.contacts[0];
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
        Vector3 pos = contact.point;

        if (hitPrefab != null)
        {
            
            var hitVFX = Instantiate(muzzlePrefab, pos, rot);
            hitVFX.transform.forward = gameObject.transform.forward;
            var psHit = hitVFX.GetComponent<ParticleSystem>();
            if (psHit != null)
                Destroy(hitVFX, psHit.main.duration);
            else
            {
                var psChild = hitVFX.transform.GetChild(0).GetComponent<ParticleSystem>();
                Destroy(hitVFX, psChild.main.duration);
            }
        }

        var myRigid = GetComponent<Rigidbody>();

        var hit = collision.gameObject;

        Rigidbody[] rigidbodies;
        rigidbodies = hit.GetComponentsInChildren<Rigidbody>();
        foreach (var rigid in rigidbodies)
        {
            rigid.velocity += transform.forward * (knockbackForce / rigid.mass);   //new Vector3(0, speed, 0);
        }

        StartCoroutine(WaitToDelete(0.02f));
        //speed = 0;
        //transform.SetParent(collision.gameObject.transform);
    }

    // Update is called once per frame
    void Update()
    {
        // Move the object forward at speed, modified by time and time powers.
        if(speed != 0)
            transform.Translate(transform.forward * speed * World.Instance.speedForce * Time.deltaTime, Space.World);
    }
}
