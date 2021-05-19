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
        
        
    }
}
