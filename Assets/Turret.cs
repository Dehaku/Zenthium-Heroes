using Hellmade.Sound;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    public AcquireTargets acquireTargets;
    public ShootKOBullet weaponScript;
    public GameObject firePoint;
    
    [Header("Functions")]
    public bool AllowedToShoot = false;
    public float minRange = 3f;
    public float maxRange = 25f;

    [Header("Detection")]
    public AudioClip TargetDetectedClip;
    bool enemyDetected = false;

    
    //public float gravityOffset = 0;
    //public float speedOffset = 0;

    [Header("Turret Parts")]
    public bool rotateParts = true;
    public Transform rotationHelper;
    public Transform rotSpin;
    public Transform rotVert;
    public Transform rotRotary;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    public bool alter;


    

    bool WithinRange(Vector3 myPos, Vector3 itPos)
    {
        float dist = Vector3.Distance(myPos, itPos);
        if (dist > minRange && dist < maxRange)
            return true;

        return false;
    }

    bool WithinRange(GameObject target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > minRange && dist < maxRange)
            return true;

        return false;
    }

    

    // Update is called once per frame
    void Update()
    {
        //acquireTargets.target = acquireTargets.AcquireNearestEnemyTarget();
        acquireTargets.target = acquireTargets.AcquireNearestEnemyTargetWithinRange(minRange,maxRange);

        
        // Play enemy detected sound.
        if (acquireTargets.target && !enemyDetected)
        {
            enemyDetected = true;
            EazySoundManager.PlaySound(TargetDetectedClip, 1, false, transform);
        }
        else if (!acquireTargets.target)
            enemyDetected = false;


        if (acquireTargets.target)
        {
            if(rotateParts)
            {
                rotationHelper.transform.LookAt(acquireTargets.target.transform.position + new Vector3(0, 1, 0));
                
                rotSpin.localEulerAngles = new Vector3(0, 0, rotationHelper.localEulerAngles.y);
                rotVert.localEulerAngles = new Vector3(0, 90, 90+ (-rotationHelper.localEulerAngles.x));
                
                firePoint.transform.rotation = rotationHelper.rotation;
            }
            else
                transform.LookAt(acquireTargets.target.transform.position + new Vector3(0, 1, 0));
            

            
        }


        { // Failed arc calculation.

            //Vector3 solve1 = new Vector3();
            //Vector3 solve2 = new Vector3();
            //int solveIndex = fts.solve_ballistic_arc(weaponScript.spawnPos.position, weaponScript.bulletSpeed+speedOffset,
            //    acquireTargets.target.transform.position, weaponScript.bulletGravity+gravityOffset, out solve1, out solve2);
            //float solve1;
            //float solve2;
            //int solveIndex = fts.solve_ballistic_arc_ang(weaponScript.spawnPos.position, weaponScript.bulletSpeed + speedOffset,
            //    acquireTargets.target.transform.position, weaponScript.bulletGravity+gravityOffset, out solve1, out solve2);
            //
            //solve1 = solve1 * weaponScript.bulletSpeed + solve1 * weaponScript.bulletSpeed;
            //solve2 *= weaponScript.bulletSpeed;
            //
            //Debug.Log(solveIndex + "    :   " + solve1 + "   :   " + solve1 * weaponScript.bulletGravity);

        }







        if (Input.GetKeyDown(KeyCode.X))
        {
            alter = !alter;

            
        }
        if(alter)
        {
            transform.eulerAngles = new Vector3(transform.rotation.eulerAngles.x,
                transform.rotation.eulerAngles.y,
                transform.rotation.eulerAngles.z);
        }


        if (!AllowedToShoot)
        {
            weaponScript.Fire(false);
            return;
        }

        if (acquireTargets.target)
        {
            weaponScript.Fire();
            
        }
        else
            weaponScript.Fire(false);
    }
}
