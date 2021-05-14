using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{

    public CharacterController controller;
    public Transform cam;

    public float speed = 6f;
    public float jumpSpeed = 8;
    public bool gravity = true;
    float _gravity = 20f;

    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    Vector3 _movement = Vector3.zero;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OriginalMove()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if(direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }
    }

    void OldMove()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        _movement.x = 0;
        _movement.z = 0;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            Vector3 moveTransition = new Vector3(moveDir.normalized.x * speed, _movement.y, moveDir.normalized.z * speed);

            _movement = moveTransition;
        }


        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            _movement.y = jumpSpeed;

        if (gravity && !controller.isGrounded)
            _movement.y -= _gravity * Time.deltaTime;

        controller.Move(_movement * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        // OriginalMove();
        OldMove();
        

    }
}
