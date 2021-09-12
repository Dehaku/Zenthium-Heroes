using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public bool hyperSpeed;
    public CharacterController controller;
    public Transform cam;
    AnimationScript animationController;

    public ParticleSystem particleTrails;
    public Vector2 trailLifeTime;

    public float defaultSpeed = 6f;
    public float defaultSprintSpeed = 10f;
    public float speed;
    public float flySpeed = 1;
    public float airDrag = 0.2f;
    public float jumpSpeed = 8;
    public bool gravity = true;
    float _gravity = 20f;

    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    Vector3 _movement = Vector3.zero;

    public float walkSoundTimer;
    float walkSoundTimerTrack = 0;

    bool wentSuperSonic;
    public bool isJumping = false;

    public bool isFlying = false;

    float timeOffGround = 0f;



    // Start is called before the first frame update
    void Start()
    {
        speed = defaultSpeed;
        animationController = GetComponent<AnimationScript>();
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

    void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        if(!isFlying)
        {
            _movement.x = 0;
            _movement.z = 0;
        }
        

        animationController.playerMovementAnimation(new Vector2(direction.normalized.x, direction.normalized.z));

        if(isFlying)
        {
            var lookPosition = controller.transform.position + Camera.main.transform.forward;
            if(Input.GetKey(KeyCode.W))
            {
                controller.transform.LookAt(lookPosition);
                _movement += controller.transform.forward * (flySpeed * Time.deltaTime);
                
            }
                
        }    
        
        else if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            

            Vector3 moveTransition = new Vector3(moveDir.normalized.x * speed, _movement.y, moveDir.normalized.z * speed);

            if(isFlying)
                moveTransition = new Vector3(moveDir.normalized.x * speed, moveDir.normalized.y * speed, moveDir.normalized.z * speed);

            _movement = moveTransition;

            // animationController.playerMovementAnimation(new Vector2(moveTransition.normalized.x, moveTransition.normalized.z));


            walkSoundTimerTrack += Time.deltaTime;
            if(walkSoundTimerTrack >= walkSoundTimer)
            {
                walkSoundTimerTrack = 0;
            }
            
            if(controller.isGrounded)
                SoundManager.PlaySound(SoundManager.Sound.PlayerMove);
        }


        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
        { // Jumping
            _movement.y = jumpSpeed;
            isJumping = true;
            animationController.JumpingAnimation(true);
            animationController.isGroundedFunc(false);
        }
        if (controller.isGrounded)
        { // Landed
            animationController.isGroundedFunc(false);
            isJumping = false;
            isFlying = false;
        }

        if (timeOffGround > 0.1 && Input.GetKeyDown(KeyCode.Space))
        { // Time to fly!
            animationController.FlyingAnimation(true);
            //_movement.y = jumpSpeed*10;
            isFlying = true;
            
        }

        if(animationController.FlyingAnimation())
        { // Flying Animation Facing, needs lerping, not sure how currently.
            //controller.transform.rotation = Quaternion.LookRotation(controller.transform.position, Camera.main.transform.up);
            var lookPosition = controller.transform.position + controller.velocity.normalized;

            

            controller.transform.LookAt(lookPosition);
        }



        if (gravity && !controller.isGrounded && !isFlying)
            _movement.y -= _gravity * Time.deltaTime;

        // if(!isFlying)
        controller.Move(_movement * Time.deltaTime);
    }

    void Flying()
    {
        // Air Drag
        _movement = Vector3.Lerp(_movement, new Vector3(0, 0, 0), Time.deltaTime * airDrag);

        if (Input.GetKey(KeyCode.Space))
            _movement.y += (flySpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.LeftControl))
            _movement.y -= (flySpeed * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        if(controller != null)
        {
            if (controller.isGrounded)
            {
                timeOffGround = 0;
                animationController.FlyingAnimation(false);
            }
                
            else
                timeOffGround += Time.deltaTime;

        }

        if (isFlying)
            Flying();

        // Sprint
        if (Input.GetKey(KeyCode.LeftShift))
        {
            
            if(speed <= defaultSprintSpeed)
                speed = Mathf.Lerp(speed, defaultSprintSpeed, Time.deltaTime);

            if(speed >= defaultSprintSpeed * 0.95)
            {
                particleTrails.Play();
                speed = defaultSprintSpeed * 3;
                GetComponent<AnimationScript>().speed = 4;

                if(!wentSuperSonic)
                {
                    wentSuperSonic = true;
                    SoundManager.PlaySound(SoundManager.Sound.SoftSonicBoom);
                }
            }

        }
        else
        {
            particleTrails.Stop();
            if (speed >= defaultSprintSpeed)
            {
                speed = defaultSprintSpeed;
                wentSuperSonic = false;
            }
                
            speed = Mathf.Lerp(speed, defaultSpeed, Time.deltaTime);
        }

        // OriginalMove();
        Move();
        

    }
}
