using Hellmade.Sound;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHit : ShootableObject
{
    public float damageMulti = 1;
    public Ragdoll ragdoll;
    public GameObject particlesPrefab;
    public AudioClip impactSound;
    public Creature creature;

    private void Start()
    {
        if (!ragdoll)
            ragdoll = GetComponentInParent<Ragdoll>();
        if(!creature) 
            creature = GetComponentInParent<Creature>();
    }

    public override void OnHit(RaycastHit hit, DamageInfo dI)
    {
        //Debug.Log(transform.root.name + " hit on " + gameObject.name + ", " + hit.collider.name);
        //Debug.Log("dI: " + dI
        //    + ", Atkr: " + dI.attacker
        //    + " damType: " + dI.damageType
        //    + " damage: " + dI.damage
        //    );
        
        // GameObject particles = Instantiate(particlesPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal), transform.root.parent);

        //Sticking the effect to the limb instead of worldspace
        if(particlesPrefab)
        {
            GameObject particles = Instantiate(particlesPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal), transform);
            Destroy(particles, 2f);
        }
        if(impactSound)
        {
            int audioID = EazySoundManager.PlaySound(impactSound, 1, false, transform);
        }

        if(creature)
        {
            bool unconscious = creature.ChangeHealth(dI, damageMulti);
            if(unconscious)
                if (ragdoll)
                    if (!ragdoll.isRagdolled)
                        ragdoll.EnableRagdoll();
        }
        else
            if (ragdoll)
                if (!ragdoll.isRagdolled)
                    ragdoll.EnableRagdoll();
        
    }

    public override void OnHit(DamageInfo dI)
    {
        if (creature)
        {
            bool unconscious = creature.ChangeHealth(dI, damageMulti);
            if (unconscious)
                if (ragdoll)
                    if (!ragdoll.isRagdolled)
                        ragdoll.EnableRagdoll();
        }
        else
            if (ragdoll)
            if (!ragdoll.isRagdolled)
                ragdoll.EnableRagdoll();
    }
}
