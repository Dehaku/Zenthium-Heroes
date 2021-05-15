using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimationScript : MonoBehaviour
{
    public Animator Animator { get; private set; }
    Rigidbody rigid;
    NavMeshAgent navAgent;

    public float speed = 0f;
    public float divideBy = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        Animator = GetComponentInChildren<Animator>();
        rigid = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if(navAgent != null)
        {
            if(navAgent.enabled)
            {
                var vel = (navAgent.velocity.magnitude + 0.001f);
                speed = vel;

                Animator.SetFloat("Speed", vel);
            }
            
        }
        
        
    }
}
