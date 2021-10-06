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
    public float fireConeAngle = 30;
    public float rotationSpeed = 1;
    public float minRange = 3f;
    public float maxRange = 25f;
    public Vector2 aimSway;
    Vector2 _aimSway;
    public bool aimSwayCross;
    public bool aimSwayNoise;

    [Header("Detection")]
    public AudioClip TargetDetectedClip;
    bool enemyDetected = false;

    
    //public float gravityOffset = 0;
    //public float speedOffset = 0;

    [Header("Turret Parts")]
    public bool rotateParts = true;
    public Transform rotationHelper;
    public GameObject lerpHelper;
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

    bool TargetWithinFireCone()
    {
        if (!acquireTargets.target)
            return false;


        Vector3 directionToTarget = (acquireTargets.target.transform.position - firePoint.transform.position).normalized;
        if (Vector3.Angle(firePoint.transform.forward, directionToTarget) < fireConeAngle / 2)
            return true;
        
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!(Time.timeScale > 0))
            return;


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
                
                lerpHelper.transform.position = rotationHelper.transform.position;
                lerpHelper.transform.rotation = rotationHelper.transform.rotation;

                lerpHelper.transform.LookAt(acquireTargets.target.transform.position + new Vector3(0, 1, 0));
                rotationHelper.transform.rotation = Quaternion.Lerp(rotationHelper.rotation, lerpHelper.transform.rotation, rotationSpeed * Time.deltaTime);



                rotSpin.localEulerAngles = new Vector3(0, 0, rotationHelper.localEulerAngles.y);
                rotVert.localEulerAngles = new Vector3(0, 90, 90+ (-rotationHelper.localEulerAngles.x));
                
                firePoint.transform.rotation = Quaternion.Lerp(firePoint.transform.rotation, rotationHelper.rotation, rotationSpeed * Time.deltaTime);

                var fireSway = firePoint.transform.eulerAngles;

                
                
                    if (aimSwayNoise)
                    {
                        float noiseX = Mathf.PerlinNoise(Time.time, Time.time * 2);
                        float noiseY = Mathf.PerlinNoise(Time.time * 2, Time.time);
                        fireSway.x += -0.5f + noiseX;
                        fireSway.y += -0.5f + noiseY;
                    }
                    else
                    {
                        if (aimSwayCross)
                        { // U pattern
                            fireSway.x += aimSway.y * Mathf.Cos(Time.time);
                            fireSway.y += aimSway.x * Mathf.Sin((Time.time * 2));
                        }
                        else
                        { // 8 pattern
                            fireSway.x += aimSway.y * Mathf.Cos(Time.time * 2);
                            fireSway.y += aimSway.x * Mathf.Sin((Time.time));
                        }
                    }
                

                

                firePoint.transform.eulerAngles = fireSway;

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
            if(TargetWithinFireCone())
            {
                rotRotary.transform.eulerAngles = new Vector3(rotRotary.transform.eulerAngles.x, rotRotary.transform.eulerAngles.y, rotRotary.transform.eulerAngles.z+10);
                weaponScript.Fire();
            }
                
            else
                weaponScript.Fire(false);

        }
        else
            weaponScript.Fire(false);
    }
}
