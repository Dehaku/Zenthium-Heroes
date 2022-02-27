using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeColliderScript : MonoBehaviour
{
    public DamageInfo damageInfo;

    private void OnTriggerEnter(Collider other)
    {
        ShootableObject shootableObject = other.GetComponent<ShootableObject>();
        if (shootableObject)
        {
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
                }
            }
            else // If faction is missing, it's probably terrain being hit, pass it through.
            {
                shootableObject.OnHit(damageInfo);
            }
            

            
        }

        
        

    }

}
