using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatorController : MonoBehaviour
{
    public Animator animator;
    public PlayerMovementAdvanced pm;

    #region AnimatorHashes
    int speedHash;
    int crouchHash;
    int hipWalkHash;
    #endregion



    // Start is called before the first frame update
    void Start()
    {
        speedHash = Animator.StringToHash("Speed");
        crouchHash = Animator.StringToHash("IsCrouching");
        hipWalkHash = Animator.StringToHash("HipWalk");
    }

    public float debugValue = 0;
    public bool hipWalk = false;

    // Update is called once per frame
    void Update()
    {
        animator.SetBool(hipWalkHash, hipWalk);


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
