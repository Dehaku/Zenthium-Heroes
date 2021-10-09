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

    public int damageType = Damage.GetType("Fire");
    public float damage = 1f;
    
    [Header("Flame Collision")]
    public float colliderRate = 0.2f;
    float _colliderTimer = 0;
    public float colliderSpeed = 1;
    public float colliderDuration = 5;
    public float colliderGrowth = 1.3f;

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
        col.transform.localScale = new Vector3(1,1,1);
        col.durationMax = colliderDuration;
        col.transform.DOMove((col.transform.forward * colliderSpeed) + col.transform.position, colliderDuration);
        col.transform.DOScale(col.transform.localScale * colliderGrowth, colliderDuration);
        colliders.Add(col);

    }
    

    // Update is called once per frame
    void Update()
    {
        if(fireGun)
        {
            StartFiring();
            //fireGun = false;
        }
        else
        {
            StopFiring();
        }
        if(stopGun)
        {
            StopFiring();
            stopGun = false;
            //Destroy(this.gameObject);
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
        _colliderTimer -= Time.deltaTime;



        // start collision spawner
        if (_colliderTimer < 0)
        {
            _colliderTimer = colliderRate;
            GenerateCollider(transform.position, transform.rotation);
        }
    }

    public void StopFiring()
    {
        vfx.Stop();
        // stop collision spawner
    }

    public void ChildCollisionEnter(FlamethrowerColliderScript collider, Collider other)
    {
        // Debug.Log(collider.name + " collided with " + other.name);

        Knockback kb = other.GetComponent<Knockback>();
        if (kb != null)
        {

            Vector3 dir = (collider.transform.position - other.gameObject.transform.position).normalized;
            Vector3 dist = (collider.transform.position - other.gameObject.transform.position);

            kb.ApplyKnockback(dir, 100 * dist.magnitude);
        }

        ShootableObject shootable = other.GetComponent<ShootableObject>();
        if(shootable)
        {
            DamageInfo damageInfo;
            damageInfo.attacker = this.gameObject;
            damageInfo.damageType = damageType;
            if (Input.GetKey(KeyCode.D))
                damageInfo.damageType = -2;
            damageInfo.damage = damage;

            shootable.OnHit(damageInfo);
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
            if(colliders[i])
                Destroy(colliders[i].gameObject);
        }

        foreach (var item in colliderPool)
        {
            if(item)
                Destroy(item.gameObject);
        }
    }

}
