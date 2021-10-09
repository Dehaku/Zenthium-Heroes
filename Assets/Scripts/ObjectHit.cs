using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectHit : ShootableObject
{
    public GameObject particlesPrefab;

    public override void OnHit(RaycastHit hit, DamageInfo dI)
    {
        GameObject particles = Instantiate(particlesPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal), transform.root.parent);
        //ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
        //if(particleSystem)
        //{
        //    particleSystem.main.startColor = Color.red;
        //}
        Destroy(particles, 2f);
    }

    public override void OnHit(DamageInfo dI)
    {

    }
}
