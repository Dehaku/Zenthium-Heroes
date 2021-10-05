using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hellmade.Sound;

public class WalkingSoundManager : MonoBehaviour
{
    public ThirdPersonMovement tpm;
    public AudioClip walkClip;
    public GameObject leftFoot;
    public GameObject rightFoot;
    public bool leftFootLifted = false;
    public bool rightFootLifted = false;

    public float yThreshold = 0f;
    public float soundVolume = 100f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (tpm.isFlying)
            return;

        

        float leftFootPosition = (leftFoot.transform.position - transform.root.position).y;
        if (leftFootPosition < yThreshold && leftFootLifted)
        {
            leftFootLifted = false;
            EazySoundManager.PlaySound(walkClip, soundVolume, false, transform.root);
        }

        if(leftFootPosition > yThreshold)
        {
            leftFootLifted = true;
        }

        float rightFootPosition = (rightFoot.transform.position - transform.root.position).y;
        if (rightFootPosition < yThreshold && rightFootLifted)
        {
            rightFootLifted = false;
            int audioID = EazySoundManager.PlaySound(walkClip, soundVolume, false, rightFoot.transform);
        }

        if (rightFootPosition > yThreshold)
        {
            rightFootLifted = true;
        }


    }
}
