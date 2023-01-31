using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatorController : MonoBehaviour
{
    public Animator animator;
    public PlayerMovementAdvanced pm;
    public Transform Orientation;
    public ThirdPersonCam cam;

    [Range(0,10)]
    public float idleAnimation = 0;

    #region AnimatorHashes
    int speedHash;
    int crouchHash;
    int hipWalkHash;

    int velocityXHash;
    int velocityZHash;

    int idleAnimationHash;
    #endregion



    // Start is called before the first frame update
    void Start()
    {
        speedHash = Animator.StringToHash("Speed");
        crouchHash = Animator.StringToHash("IsCrouching");
        hipWalkHash = Animator.StringToHash("HipWalk");
        velocityXHash = Animator.StringToHash("Velocity X");
        velocityZHash = Animator.StringToHash("Velocity Z");
        idleAnimationHash = Animator.StringToHash("IdleAnimation");
    }

    public float debugValue = 0;
    public bool hipWalk = false;

    // Update is called once per frame
    void Update()
    {
        // Hip/Swagger swapper
        animator.SetBool(hipWalkHash, hipWalk);

        var locVel = Orientation.InverseTransformDirection(pm.rb.velocity);

        // Idle Animation
        animator.SetFloat(idleAnimationHash, idleAnimation);

        // Strafe/First Person Movement
        if(cam.currentCam == (int)ThirdPersonCam.CameraStyle.Combat || cam.currentCam == (int)ThirdPersonCam.CameraStyle.FirstPerson)
        {
            animator.SetFloat(velocityXHash, locVel.x);
            animator.SetFloat(velocityZHash, locVel.z);
            
        }
        // Platformer Movement
        else
        {
            animator.SetFloat(velocityXHash, 0);
            animator.SetFloat(velocityZHash, pm.GetVelocity());
        }

        // Velocity
        animator.SetFloat(speedHash, pm.GetVelocity());

        // Crouch
        animator.SetBool(crouchHash, pm.crouching);

        /*
        // Walk/Run
        if (pm.GetVelocity() > 0.25f)
            animator.SetFloat(speedHash, pm.GetVelocity());
        else if (Input.GetKey("y"))
            animator.SetFloat(speedHash, 1);
        else
            animator.SetFloat(speedHash, 0);

        if(debugValue > 0)
            animator.SetFloat(speedHash, debugValue);
        */



    }
}
