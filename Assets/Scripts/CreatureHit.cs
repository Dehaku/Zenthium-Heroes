using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHit : ShootableObject
{
    public Ragdoll ragdoll;
    public GameObject particlesPrefab;

    private void Start()
    {
        if (!ragdoll)
            ragdoll = GetComponentInParent<Ragdoll>();
    }

    public override void OnHit(RaycastHit hit)
    {
        Debug.Log(transform.root.name + " hit on " + gameObject.name + ", " + hit.collider.name);
        
        
        // GameObject particles = Instantiate(particlesPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal), transform.root.parent);

        //Sticking the effect to the limb instead of worldspace
        GameObject particles = Instantiate(particlesPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal), transform);

        //ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
        //if(particleSystem)
        //{
        //    particleSystem.main.startColor = Color.red;
        //}

        if (ragdoll)
        {
            if (!ragdoll.isRagdolled)
                ragdoll.EnableRagdoll();
            //else if (ragdoll.isRagdolled)
            //    ragdoll.DisableRagdoll();


        }
            

        Destroy(particles, 2f);
    }
}
