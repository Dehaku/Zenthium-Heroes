using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatorController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerMovementAdvanced pm;
    public Transform Orientation;
    public ThirdPersonCam cam;

    [Header("Test Settings")]
    [Range(0,10)]
    public float idleAnimation = 0;

    #region AnimatorHashes
    int speedHash;
    int crouchHash;
    int hipWalkHash;

    int grapplingLeftHash;
    int grapplingRightHash;
    int swingingLeftHash;
    int swingingRightHash;
    int wallRunLeftHash;
    int wallRunRightHash;
    int climbingHash;
    int dashingHash;
    int slidingHash;
    int airHash;
    int flyingHash;

    int velocityXHash;
    int velocityZHash;

    int idleAnimationHash;
    #endregion



    // Start is called before the first frame update
    void Start()
    {
        idleAnimationHash = Animator.StringToHash("IdleAnimation");
        hipWalkHash = Animator.StringToHash("HipWalk");
        velocityXHash = Animator.StringToHash("Velocity X");
        velocityZHash = Animator.StringToHash("Velocity Z");

        speedHash = Animator.StringToHash("Speed");
        crouchHash = Animator.StringToHash("IsCrouching");
        slidingHash = Animator.StringToHash("IsSliding");
        airHash = Animator.StringToHash("IsInAir");
        climbingHash = Animator.StringToHash("IsClimbing");
        dashingHash = Animator.StringToHash("IsDashing");

        wallRunLeftHash = Animator.StringToHash("WallRunLeft");
        wallRunRightHash = Animator.StringToHash("WallRunRight");

        grapplingLeftHash = Animator.StringToHash("grapplingLeft"); 
        grapplingRightHash = Animator.StringToHash("grapplingRight"); 
        swingingLeftHash = Animator.StringToHash("swingingLeft"); 
        swingingRightHash = Animator.StringToHash("swingingRight"); 
        

        flyingHash = Animator.StringToHash("IsFlying");



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

        // Sliding
        animator.SetBool(slidingHash, pm.sliding);

        // Is In Air
        animator.SetBool(airHash, (pm.state == PlayerMovementAdvanced.MovementState.air) );

        // Wallrunning
        if(pm.wallRunScript)
        {
            animator.SetBool(wallRunLeftHash, pm.wallRunScript.wallLeft);
            animator.SetBool(wallRunRightHash, pm.wallRunScript.wallRight);
        }
        

        /*
         * 
         * 
         *       animator.Play(punchHash);
                //animator.CrossFade(Punch, 0.25f);
         *  
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
