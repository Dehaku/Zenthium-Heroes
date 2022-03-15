using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackIfCan : MonoBehaviour
{
    public float meleeRange = 2;
    public float shootRange = 20;

    ChaseTarget target;
    MeleeAttack meleeCol;
    MeleeSeekAttack meleeSeek;
    ShootKOBullet rangedAttack;

    // Start is called before the first frame update
    void Start()
    {
        target = GetComponent<ChaseTarget>();
        meleeCol = GetComponent<MeleeAttack>();
        meleeSeek = GetComponent<MeleeSeekAttack>();
        rangedAttack = GetComponent<ShootKOBullet>();
    }

    
    
    // Update is called once per frame
    void Update()
    {
        // No target? No attacking.
        if (!target)
            return;
        if (!target.target)
            return;
        var distFromTarget = Vector3.Distance(transform.position, target.target.transform.position);
        bool withinMeleeRange = (distFromTarget < meleeRange);
        bool withinShootRange = (distFromTarget < shootRange);


        
        if (meleeCol)
        {
            if(withinMeleeRange)
                meleeCol.Melee(true);
            else
                meleeCol.Melee(false);
        }
        if(meleeSeek)
        {
            if (withinMeleeRange)
            {
                Debug.LogWarning("No aiming or anything is set here.");
                meleeSeek.Melee(true);
            }
            else
                meleeSeek.Melee(false);
        }
        if (rangedAttack)
        {
            if(withinShootRange)
            {
                Debug.LogWarning("No aiming or anything is set here.");
                rangedAttack.Fire(true);
            }
            else
                rangedAttack.Fire(false);
        }
    }
}
