using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    public AcquireTargets acquireTargets;
    public ShootKOBullet weaponScript;
    public bool AllowedToShoot = false;
    public Vector3 Offset;

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
            transform.LookAt(acquireTargets.target.transform.position);
        }




        

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
