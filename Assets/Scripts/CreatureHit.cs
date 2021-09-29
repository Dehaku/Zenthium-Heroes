using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHit : ShootableObject
{
    public RagdollOnOff ragdoll;
    public GameObject particlesPrefab;

    public override void OnHit(RaycastHit hit)
    {
        Debug.Log("Creature hit on " + gameObject.name + ", " + hit.collider.name);
        GameObject particles = Instantiate(particlesPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal), transform.root.parent);
        //ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
        //if(particleSystem)
        //{
        //    particleSystem.main.startColor = Color.red;
        //}
        
        
        //ragdoll.RagdollModeOn();
        Destroy(particles, 2f);
    }
}
