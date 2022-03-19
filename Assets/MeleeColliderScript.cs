using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeColliderScript : MonoBehaviour
{
    public DamageInfo damageInfo;
    List<Creature> creaturesHitThisFrame = new List<Creature>();
    public Animator anim;
    public float animSpeed = 1;
    int forwardThrust = Animator.StringToHash("ForwardThrust");
    int uppercut = Animator.StringToHash("Uppercut");
    public bool animForwardThrust = false;
    public bool animUppercut = false;



    private void OnEnable()
    {
        creaturesHitThisFrame.Clear();
        if(anim)
        {
            anim.speed = animSpeed;
            if(animForwardThrust) 
                anim.Play(forwardThrust);
            else if(animUppercut)
                anim.Play(uppercut);
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        ShootableObject shootableObject = other.GetComponent<ShootableObject>();
        if (shootableObject)
        {
            
            // Make sure we haven't already hit this creature this frame.
            if (creaturesHitThisFrame.Contains(shootableObject.GetComponentInParent<Creature>()))
            {
                return;
            }

            var impactSound = other.GetComponent<ImpactSoundScript>();
            if(!impactSound)
                impactSound = other.GetComponentInParent<ImpactSoundScript>();
            if (impactSound)
                impactSound.PlayImpactSound();




            var targetFaction = other.GetComponent<Faction>();
            if(!targetFaction)
                targetFaction = other.GetComponentInParent<Faction>();
            if (!targetFaction) // I don't think this one will ever be used, this would be a dumb setup.
                targetFaction = other.GetComponentInChildren<Faction>();


            var attackerFaction = damageInfo.attacker.GetComponent<Faction>();
            if (!attackerFaction)
                attackerFaction = damageInfo.attacker.GetComponentInParent<Faction>();
            if (!attackerFaction) // I don't think this one will ever be used, this would be a dumb setup.
                attackerFaction = damageInfo.attacker.GetComponentInChildren<Faction>();

            // Make sure the factions are valid, and that they're not part of your own faction.
            if (targetFaction && attackerFaction)
            {
                if (targetFaction.CurrentFactionID != attackerFaction.CurrentFactionID)
                {
                    shootableObject.OnHit(damageInfo);
                    creaturesHitThisFrame.Add(other.GetComponentInParent<Creature>());
                }
            }
            else // If faction is missing, it's probably terrain being hit, pass it through.
            {
                shootableObject.OnHit(damageInfo);
            }
        }
    }
}
