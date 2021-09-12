using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimationScript : MonoBehaviour
{
    public Animator animator { get; private set; }
    Rigidbody rigid;
    NavMeshAgent navAgent;
    CharacterController cc;

    int movex = Animator.StringToHash("X_Move");
    int movey = Animator.StringToHash("Y_Move");
    int isJumping = Animator.StringToHash("Jump");
    int isGrounded = Animator.StringToHash("isGrounded");
    int isFlying = Animator.StringToHash("Flying");

    int moveSpeed = Animator.StringToHash("Speed");
    int animationSpeed = Animator.StringToHash("AnimationSpeed");



    public float speed = 0f;
    public float divideBy = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rigid = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();
        cc = GetComponent<CharacterController>();
    }


    public void JumpingAnimation(bool value)
    {
        if (animator.GetBool(isJumping) != value)
            animator.SetBool(isJumping, value);
    }

    public void isGroundedFunc(bool value)
    {
        if (animator.GetBool(isGrounded) != value)
            animator.SetBool(isGrounded, value);
    }

    public void FlyingAnimation(bool value)
    {
        if (animator.GetBool(isFlying) != value)
            animator.SetBool(isFlying, value);
    }

    public bool FlyingAnimation()
    {
        return animator.GetBool(isFlying);
    }

    public void playerMovementAnimation(Vector2 value)
    {
        // animator.SetFloat(movex, value.x);
        // animator.SetFloat(movey, value.y);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (cc != null)
            speed = Mathf.Lerp(speed, cc.velocity.normalized.magnitude, Time.deltaTime * 10);

        if (animator != null && cc != null)
        {
            animator.SetFloat(moveSpeed, speed);
            if(speed > 1)
                animator.SetFloat(animationSpeed, speed);
            else
                animator.SetFloat(animationSpeed, 1);
        }
            

        

        if (navAgent != null)
        {
            if(navAgent.enabled)
            {
                var vel = (navAgent.velocity.magnitude + 0.001f);
                speed = vel;

                animator.SetFloat(moveSpeed, vel);
            }
            
        }

        if (animator == null || cc == null)
        {
            return;
        }

        animator.SetBool("isGrounded", cc.isGrounded);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetBool("Jump", true);
            
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            animator.SetBool("Jump", false);
            // animator.Play("Jump", -1, 0f);
        }

        if(Input.GetKey(KeyCode.H))
        {
            animator.Play("Base Layer.Jump", 0, 0f);
        }
            


    }
}
