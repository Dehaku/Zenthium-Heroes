using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using UnityEngine.Animations.Rigging;

public class AnimationScript : MonoBehaviour
{
    public Animator animator { get; private set; }
    public Rig handsOnGun;
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

    int punch = Animator.StringToHash("Punch");
    int wholePunch = Animator.StringToHash("WholePunch");




    public float speed = 0f;
    public float divideBy = 0.5f;

    Vector2 relativeMovement;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rigid = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();
        cc = GetComponent<CharacterController>();
        handsOnGun = GetComponentInChildren<Rig>();
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
        animator.SetFloat(movex, value.x);
        animator.SetFloat(movey, value.y);
        
    }

    public void PunchAnimation(bool value)
    {
        if(value)
        {
            animator.Play(wholePunch);
            return;
        }
        else
            animator.Play(punch);
    }


    private void FixedUpdate()
    {
        if (cc != null)
        {
            speed = Mathf.Lerp(speed, cc.velocity.magnitude, Time.deltaTime * 10);

            //LerpRelativeMovementToRelativeDirection(cc.velocity,cc.transform);

            Vector3 forward = cc.transform.TransformDirection(Vector3.forward);
            Vector3 right = cc.transform.TransformDirection(Vector3.right);
            // 
            relativeMovement.y = Mathf.Lerp(relativeMovement.y, Vector3.Dot(cc.velocity.normalized, forward), Time.deltaTime * 10);
            relativeMovement.x = Mathf.Lerp(relativeMovement.x, Vector3.Dot(cc.velocity.normalized, right), Time.deltaTime * 10);


            //Debug.Log(relativeMovement + " : " + cc.velocity);
            playerMovementAnimation(relativeMovement);
        }
        else if(navAgent != null)
        {
            Vector3 forward = navAgent.transform.TransformDirection(Vector3.forward);
            Vector3 right = navAgent.transform.TransformDirection(Vector3.right);

            relativeMovement.y = Mathf.Lerp(relativeMovement.y, Vector3.Dot(navAgent.velocity.normalized, forward), Time.deltaTime * 10);
            relativeMovement.x = Mathf.Lerp(relativeMovement.x, Vector3.Dot(navAgent.velocity.normalized, right), Time.deltaTime * 10);
            //relativeMovement.y = Vector3.Dot(navAgent.velocity.normalized, forward);
            //relativeMovement.x = Vector3.Dot(navAgent.velocity.normalized, right);
            //Debug.Log(relativeMovement +" : "+ navAgent.velocity);
            playerMovementAnimation(relativeMovement);
        }
        
    }
    // Update is called once per frame
    void Update()
    {
        
            

        


        if (animator != null && cc != null)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                //Punch.play();
                animator.Play(punch);
                //animator.CrossFade(Punch, 0.25f);
            }
            if (Input.GetKeyDown(KeyCode.G))
                DOTween.To(() => handsOnGun.weight, x => handsOnGun.weight = x, 1, 0.25f);
            if (Input.GetKeyDown(KeyCode.J))
                DOTween.To(() => handsOnGun.weight, x => handsOnGun.weight = x, 0, 0.25f);

            animator.SetFloat(moveSpeed, speed);
            
            if (speed > 50)
                animator.SetFloat(animationSpeed, speed);
            else
                animator.SetFloat(animationSpeed, 1);
            //if(Input.GetKey(KeyCode.S) && Input.GetMouseButton(1))
            //    animator.SetFloat(moveSpeed, -1);

            


        }
            

        

        if (navAgent != null)
        {
            if(navAgent.enabled)
            {
                var vel = (navAgent.velocity.magnitude + 0.001f);
                speed = vel;

                animator.SetFloat(moveSpeed, vel);
                {
                    //relativeMovement.y = Mathf.Min(vel, 1);
                    //playerMovementAnimation(relativeMovement);
                }
                    
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
