using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationScript : MonoBehaviour
{
    public Animator Animator { get; private set; }
    Rigidbody rigid;

    public float speed = 0f;

    // Start is called before the first frame update
    void Start()
    {
        Animator = GetComponentInChildren<Animator>();
        rigid = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        var vel = (rigid.velocity.magnitude+0.001f) / 10;
        speed = vel;

        Animator.SetFloat("Speed", vel);
        
    }
}
