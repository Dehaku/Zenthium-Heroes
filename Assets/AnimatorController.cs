using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatorController : MonoBehaviour
{
    public Animator animator;
    public PlayerMovementAdvanced pm;
    public Transform Orientation;

    #region AnimatorHashes
    int speedHash;
    int crouchHash;
    int hipWalkHash;

    int velocityXHash;
    int velocityZHash;
    #endregion



    // Start is called before the first frame update
    void Start()
    {
        speedHash = Animator.StringToHash("Speed");
        crouchHash = Animator.StringToHash("IsCrouching");
        hipWalkHash = Animator.StringToHash("HipWalk");
        velocityXHash = Animator.StringToHash("Velocity X");
        velocityZHash = Animator.StringToHash("Velocity Z");
    }

    public float debugValue = 0;
    public bool hipWalk = false;

    // Update is called once per frame
    void Update()
    {
        animator.SetBool(hipWalkHash, hipWalk);

        var locVel = Orientation.InverseTransformDirection(pm.rb.velocity);

        animator.SetFloat(velocityXHash, locVel.x);
        animator.SetFloat(velocityZHash, locVel.z);
        
        

        // Walk/Run
        if (pm.GetVelocity() > 0.25f)
            animator.SetFloat(speedHash, pm.GetVelocity());
        else if (Input.GetKey("y"))
            animator.SetFloat(speedHash, 1);
        else
            animator.SetFloat(speedHash, 0);

        if(debugValue > 0)
            animator.SetFloat(speedHash, debugValue);

        // Crouch
        animator.SetBool(crouchHash, pm.crouching);

    }
}
