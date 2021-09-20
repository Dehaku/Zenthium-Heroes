using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using DG.Tweening;

public class FlamethrowerScript : MonoBehaviour
{
    public VisualEffect vfx;

    public bool fireGun = false;
    public bool stopGun = false;


    
    public float colliderSpeed = 1;
    public float colliderDuration = 5;

    public FlamethrowerColliderScript colliderPrefab;
    public Queue<FlamethrowerColliderScript> colliderPool;
    public List<FlamethrowerColliderScript> colliders;
    


    // Start is called before the first frame update
    void Start()
    {
        colliderPool = new Queue<FlamethrowerColliderScript>();

        
            Queue<GameObject> objectPool = new Queue<GameObject>();

            //for (int i = 0; i < pool.size; i++)
            //{
            //    GameObject obj =  Instantiate(pool.prefab, this.transform);
            //    obj.SetActive(false);
            //    objectPool.Enqueue(obj);
            //}

        vfx.Stop();
    }

    public void GenerateCollider(Vector3 spawnPos, Quaternion spawnRot)
    {
        FlamethrowerColliderScript col;

        if(colliderPool.Count > 0)
        {
            col = colliderPool.Dequeue();
            col.gameObject.SetActive(true);
        }
        else
        {
            col = Instantiate(colliderPrefab);
        }

        col.parentScript = this;
        col.transform.position = spawnPos;
        col.transform.rotation = spawnRot;
        col.durationMax = colliderDuration;
        col.transform.DOMove((col.transform.forward * colliderSpeed) + col.transform.position, colliderDuration);
        colliders.Add(col);

    }
    

    // Update is called once per frame
    void Update()
    {
        if(fireGun)
        {
            StartFiring();
            fireGun = false;
        }
        if(stopGun)
        {
            StopFiring();
            stopGun = false;
            Destroy(this.gameObject);
        }

        foreach (var col in colliders)
        {
            if(col.currentDuration < col.durationMax)
            {
                col.currentDuration += Time.deltaTime;
                
                //col.transform.Translate(col.transform.forward * 0.5f);
            }
            else
            {
                col.currentDuration = 0;
                col.gameObject.SetActive(false);
                colliderPool.Enqueue(col);
                colliders.Remove(col);
                break;
            }
        }
        
    }

    public void StartFiring()
    {
        vfx.Play();
        GenerateCollider(transform.position,transform.rotation);

        // start collision spawner
    }

    public void StopFiring()
    {
        vfx.Stop();
        // stop collision spawner
    }

    public void ChildCollisionEnter(FlamethrowerColliderScript collider, Collider other)
    {
        Debug.Log(collider.name + " collided with " + other.name);

        Knockback kb = other.GetComponent<Knockback>();
        if (kb != null)
        {

            Vector3 dir = (collider.transform.position - other.gameObject.transform.position).normalized;
            Vector3 dist = (collider.transform.position - other.gameObject.transform.position);

            kb.ApplyKnockback(dir, 100 * dist.magnitude);
        }
    }

    public void ChildCollisionStay(FlamethrowerColliderScript collider, Collider other)
    {

    }

    public void ChildCollisionExit(FlamethrowerColliderScript collider, Collider other)
    {
        if (other.tag == "Player")
            Debug.Log(collider.name + " STOPPED colliding with " + other.name);
    }


    private void OnDestroy()
    {
        for (int i = colliders.Count-1; i >= 0; i--)
        {
            Destroy(colliders[i].gameObject);
        }

        foreach (var item in colliderPool)
        {
            Destroy(item.gameObject);
        }
    }

}
