using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public bool hyperSpeed;
    public float hyperSpeedMulti = 3;
    public CharacterController controller;
    public Transform cam;
    AnimationScript animationController;

    public ParticleSystem particleTrails;
    public Vector2 trailLifeTime;

    public float defaultSpeed = 6f;
    public float defaultSprintMulti = 1.4f;
    public float speed;
    public float defaultFlySpeed = 1;
    public float defaultFlySprintMulti = 1.4f;
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

    public float speedMagnitude;
    float speedMagnitudePrevious;
    private bool _rotateOnMove = true;

    public float GetSprintSpeed()
    {
        return defaultSpeed * defaultSprintMulti;
    }

    public float GetFlySprintSpeed()
    {
        return defaultFlySpeed * defaultFlySprintMulti;
    }
    

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
            //_movement.x = Mathf.Lerp(_movement.x, 0, Time.deltaTime*5);
            //_movement.z = Mathf.Lerp(_movement.z, 0, Time.deltaTime*5);
        }



        if(isFlying)
        {
            
                
        }    
        
        else if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            if(_rotateOnMove)
            {
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
                

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


        
        if (controller.isGrounded)
        { // Landed
            animationController.isGroundedFunc(true);
            isJumping = false;
            isFlying = false;
            _movement.y = -0.3f;
        }
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
        { // Jumping
            _movement.y = jumpSpeed;
            isJumping = true;
            animationController.JumpingAnimation(true);
            animationController.isGroundedFunc(false);
        }

        if (timeOffGround > 0.1 && Input.GetKeyDown(KeyCode.Space) && !isFlying)
        { // Time to fly!
            animationController.FlyingAnimation(true);
            isFlying = true;
        }
        

        if(animationController.FlyingAnimation())
        { // Flying Animation Facing, needs lerping, not sure how currently.
            //controller.transform.rotation = Quaternion.LookRotation(controller.transform.position, Camera.main.transform.up);
            var lookPosition = controller.transform.position + controller.velocity.normalized;

            
            // Make it look smooth as it aims towards velocity.
            if(controller.velocity.magnitude > 1)
                controller.transform.LookAt(lookPosition);

            // Constantly adjust to be upright.
            if (controller.transform.rotation.x != 0)
                controller.transform.Rotate(new Vector3(-controller.transform.rotation.x, 0, 0));
        }



        if (gravity && !controller.isGrounded && !isFlying)
            _movement.y -= _gravity * Time.deltaTime;

        // if(!isFlying)
        controller.Move(_movement * Time.deltaTime);
    }

    public void SetRotateOnMove(bool newRotateOnMove)
    {
        _rotateOnMove = newRotateOnMove;
    }

    void Flying()
    {
        // Air Drag
        _movement = Vector3.Lerp(_movement, new Vector3(0, 0, 0), Time.deltaTime * airDrag);
        

        var lookPosition = controller.transform.position + Camera.main.transform.forward;

        Vector3 flyDirection = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            controller.transform.LookAt(lookPosition);
            flyDirection += controller.transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            controller.transform.LookAt(lookPosition);
            flyDirection += controller.transform.right;
        }
        if (Input.GetKey(KeyCode.S))
        {
            controller.transform.LookAt(lookPosition);
            flyDirection += -controller.transform.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            controller.transform.LookAt(lookPosition);
            flyDirection += -controller.transform.right;
        }
        if (Input.GetKey(KeyCode.Space))
            flyDirection.y += 1;
        if (Input.GetKey(KeyCode.LeftControl))
            flyDirection.y -= 1;

        flyDirection = flyDirection.normalized * (flySpeed * Time.deltaTime);
        _movement += flyDirection;
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
            
            if(speed <= GetSprintSpeed())
                speed = Mathf.Lerp(speed, GetSprintSpeed(), Time.deltaTime);

            if (flySpeed <= GetFlySprintSpeed())
                flySpeed = Mathf.Lerp(flySpeed, GetFlySprintSpeed(), Time.deltaTime);

            // Hyperspeed Power: Making sonic boom and turning on particles when we go fast.
            if(hyperSpeed && (speedMagnitude > defaultSpeed || speedMagnitude > defaultFlySpeed))
            {
                bool sonicBoomTime = false;
                if (speed >= GetSprintSpeed() * 0.95)
                {
                    speed = GetSprintSpeed() * hyperSpeedMulti;
                    sonicBoomTime = true;
                }

                if (flySpeed >= GetFlySprintSpeed() * 0.95)
                {
                    flySpeed = GetFlySprintSpeed() * hyperSpeedMulti;
                    sonicBoomTime = true;
                }

                if(sonicBoomTime && !wentSuperSonic)
                {
                    wentSuperSonic = true;
                    particleTrails.Play();
                    SoundManager.PlaySound(SoundManager.Sound.SoftSonicBoom);
                }
            }

            // Disabling our particles temporarily when we're not going super fast.
            if(hyperSpeed)
            {
                if (isFlying && speedMagnitude <= GetFlySprintSpeed())
                    particleTrails.Stop();
                else if (!isFlying && speedMagnitude <= GetSprintSpeed())
                    particleTrails.Stop();
                else if (wentSuperSonic)
                    particleTrails.Play();
            }
        }
        else
        {
            particleTrails.Stop();
            if (speed >= GetSprintSpeed())
            {
                speed = GetSprintSpeed();
                wentSuperSonic = false;
            }
            speed = Mathf.Lerp(speed, defaultSpeed, Time.deltaTime);

            if (flySpeed >= GetFlySprintSpeed())
            {
                flySpeed = GetFlySprintSpeed();
                wentSuperSonic = false;
            }
            flySpeed = Mathf.Lerp(flySpeed, defaultFlySpeed, Time.deltaTime);
        }

        // OriginalMove();
        Move();
        speedMagnitudePrevious = speedMagnitude;
        speedMagnitude = controller.velocity.magnitude;

    }
}
