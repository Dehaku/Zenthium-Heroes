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


    // Update is called once per frame
    void Update()
    {
        if (cc != null)
            speed = Mathf.Lerp(speed, cc.velocity.normalized.magnitude, Time.deltaTime * 10);

        if (animator != null && cc != null)
            animator.SetFloat("Speed", speed);

        

        if (navAgent != null)
        {
            if(navAgent.enabled)
            {
                var vel = (navAgent.velocity.magnitude + 0.001f);
                speed = vel;

                animator.SetFloat("Speed", vel);
            }
            
        }

        if (animator == null || cc == null)
        {
            return;
        }

        animator.SetBool("IsGrounded", cc.isGrounded);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetFloat("Speed", 0);
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
