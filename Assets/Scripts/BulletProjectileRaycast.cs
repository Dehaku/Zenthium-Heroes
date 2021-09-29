using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectileRaycast : MonoBehaviour
{
    public GameObject muzzlePrefab;
    public GameObject hitPrefab;
    public float speed = 50;
    public float gravity = 1;
    public float knockbackForce = 50;

    Vector3 startPosition;
    Vector3 startForward;
    bool isInitialized = false;
    float startTime = -1;

    public void Initialize(Transform startPoint, float speed, float gravity)
    {
        startPosition = startPoint.position;
        startForward = startPoint.forward.normalized;
        this.speed = speed;
        this.gravity = gravity;
        isInitialized = true;
    }

    Vector3 FindPointOnParabola(float time)
    {
        Vector3 point = startPosition + (startForward * speed * time);
        Vector3 gravityVec = Vector3.down * gravity * time * time;
        return point + gravityVec;
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;
        if (startTime < 0) startTime = Time.time;


        RaycastHit hit;
        float currentTime = Time.time - startTime;
        float nextTime = currentTime + Time.fixedDeltaTime;

        Vector3 currentPoint = FindPointOnParabola(currentTime);
        Vector3 nextPoint = FindPointOnParabola(nextTime);

        if(Physics.Linecast(currentPoint, nextPoint, out hit))
        {
            //Hit

            if (hitPrefab != null)
            {

                var hitVFX = Instantiate(muzzlePrefab, hit.point, Quaternion.Euler(hit.normal));
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

            //StartCoroutine(WaitToDelete(0.02f));
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isInitialized) return;
        
        float currentTime = Time.time - startTime;
        Vector3 currentPoint = FindPointOnParabola(currentTime);

        transform.position = currentPoint;

    }

    

    

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
        return;

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

    
}
