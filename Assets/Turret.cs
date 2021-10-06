using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    public AcquireTargets acquireTargets;
    public ShootKOBullet weaponScript;
    public GameObject firePoint;
    public bool AllowedToShoot = false;
    public Vector3 Offset;

    public float gravityOffset = 0;
    public float speedOffset = 0;

    public bool rotateParts = true;
    public Transform rotationHelper;
    public Transform rotSpin;
    public Transform rotVert;
    public Transform rotRotary;
    public Vector3 offset;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    public bool alter;

    // Update is called once per frame
    void Update()
    {
        acquireTargets.target = acquireTargets.AcquireNearestTarget();
        if(acquireTargets.target)
        {
            if(rotateParts)
            {
                rotationHelper.transform.LookAt(acquireTargets.target.transform.position + new Vector3(0, 1, 0));
                
                rotSpin.localEulerAngles = new Vector3(0, 0, rotationHelper.localEulerAngles.y);
                rotVert.localEulerAngles = new Vector3(0+offset.x, 90+offset.y, 90+ (-rotationHelper.localEulerAngles.x)+offset.z);
                
                firePoint.transform.rotation = rotationHelper.rotation;
            }
            else
                transform.LookAt(acquireTargets.target.transform.position + new Vector3(0, 1, 0));
            

            
        }

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




        if (Input.GetKeyDown(KeyCode.X))
        {
            alter = !alter;

            
        }
        if(alter)
        {
            transform.eulerAngles = new Vector3(transform.rotation.eulerAngles.x + Offset.x,
                transform.rotation.eulerAngles.y + Offset.y,
                transform.rotation.eulerAngles.z + Offset.z);
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
