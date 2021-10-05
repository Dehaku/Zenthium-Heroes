using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    public AcquireTargets acquireTargets;
    public ShootKOBullet weaponScript;
    public bool AllowedToShoot = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

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
