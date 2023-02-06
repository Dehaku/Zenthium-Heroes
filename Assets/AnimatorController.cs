using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class AnimatorController : MonoBehaviour
{
    [Header("Rigs")]
    public Rig leftArm;
    public Transform leftArmTarget;
    public Rig rightArm;
    public Transform rightArmTarget;
    public Rig HeadTracking;
    public Transform headTarget;

    [Header("References")]
    public Animator animator;
    public PlayerMovementAdvanced pm;
    public Transform orientation;
    public Transform playerObj;
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
    int hangingHash;
    int isSwingingHash;

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
        hangingHash = Animator.StringToHash("IsHanging");
        dashingHash = Animator.StringToHash("IsDashing");

        wallRunLeftHash = Animator.StringToHash("WallRunLeft");
        wallRunRightHash = Animator.StringToHash("WallRunRight");

        grapplingLeftHash = Animator.StringToHash("grapplingLeft"); 
        grapplingRightHash = Animator.StringToHash("grapplingRight"); 
        swingingLeftHash = Animator.StringToHash("swingingLeft"); 
        swingingRightHash = Animator.StringToHash("swingingRight");
        isSwingingHash = Animator.StringToHash("IsSwinging");


        flyingHash = Animator.StringToHash("IsFlying");

        

    }

    public float debugValue = 0;
    public bool hipWalk = false;

    // Update is called once per frame
    void Update()
    {
        // Hip/Swagger swapper
        animator.SetBool(hipWalkHash, hipWalk);

        var locVel = orientation.InverseTransformDirection(pm.rb.velocity);

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
        animator.SetBool(airHash, ((pm.state == PlayerMovementAdvanced.MovementState.air) ||
            pm.state == PlayerMovementAdvanced.MovementState.swinging));

        // Wallrunning
        if(pm.wallRunScript)
        {
            if (pm.state == PlayerMovementAdvanced.MovementState.wallrunning)
            {
                animator.SetBool(wallRunLeftHash, pm.wallRunScript.wallLeft);
                animator.SetBool(wallRunRightHash, pm.wallRunScript.wallRight);
            }
            else
            {
                animator.SetBool(wallRunLeftHash, false);
                animator.SetBool(wallRunRightHash, false);
            }

            
        }

        // Climbing
        if(pm.climbingScript)
        {
            if(pm.state == PlayerMovementAdvanced.MovementState.climbing)
            {
                animator.SetFloat("ClimbSpeed", pm.climbingScript.climbSpeed);
                if(!animator.GetBool(climbingHash))
                {
                    animator.SetBool(climbingHash, true);
                    int climb = Animator.StringToHash("Climbing");
                    //animator.Play(climb);
                    animator.CrossFade(climb, 0.25f);
                }
                
            }
            else if(animator.GetBool(climbingHash))
                animator.SetBool(climbingHash, false);
        }

        // Hanging from Ledge
        if (pm.ledgeGrabScript)
        {
            if (pm.ledgeGrabScript.holding)
            {
                if (!animator.GetBool(hangingHash))
                {
                    animator.SetBool(hangingHash, true);
                    int hang = Animator.StringToHash("HangingIdle");
                    animator.CrossFade(hang, 0.25f);
                }

            }
            else if (animator.GetBool(hangingHash))
                animator.SetBool(hangingHash, false);
        }

        // Swinging/Grappling
        if (pm.dualHooksScript)
        {
            if (pm.swinging)
            {
                if(pm.dualHooksScript.swingsActive[0])
                    leftArmTarget.position = pm.dualHooksScript.swingPoints[0];
                if (pm.dualHooksScript.swingsActive[1])
                    rightArmTarget.position = pm.dualHooksScript.swingPoints[1];

                if (!animator.GetBool(isSwingingHash))
                {
                    if (pm.dualHooksScript.swingsActive[0])
                        leftArm.weight = 1;
                    if (pm.dualHooksScript.swingsActive[1])
                        rightArm.weight = 1;

                    animator.SetBool(isSwingingHash, true);
                    int swing = Animator.StringToHash("Falling");
                    animator.SetBool(airHash, true);
                    animator.CrossFade(swing, 0.25f);
                    
                }

            }
            else if (animator.GetBool(isSwingingHash))
            {
                leftArm.weight = 0;
                rightArm.weight = 0;
                animator.SetBool(isSwingingHash, false);
            }

            // Bit lazy on this one.
            if (pm.activeGrapple)
            {
                int swing = Animator.StringToHash("StillSlide");
                animator.CrossFade(swing, 0.25f);
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            //animator.SetBool(climbingHash, true);
            //animator.CrossFade("Climbing", 0.25f);
            if(pm.dualHooksScript)
            {
                if (pm.dualHooksScript.gunTips[0])
                    pm.dualHooksScript.gunTips[0] = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                if (pm.dualHooksScript.gunTips[1])
                    pm.dualHooksScript.gunTips[1] = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
            

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

    private void LateUpdate()
    {
        // Swinging
        if (pm.dualHooksScript)
        {
            if (pm.swinging)
            {
                if (pm.dualHooksScript.swingPoints[0] != Vector3.zero)
                {
                    //playerObj.LookAt(Quaternion.AngleAxis(-45, Vector3.up) * pm.dualHooksScript.swingPoints[0], transform.up);
                    //Vector3 newDirection = Vector3.RotateTowards(transform.forward, pm.dualHooksScript.swingPoints[0], 1, 0.0f);
                    //playerObj.rotation = Quaternion.LookRotation(newDirection);
                }
                    
            }
        }
    }
}
