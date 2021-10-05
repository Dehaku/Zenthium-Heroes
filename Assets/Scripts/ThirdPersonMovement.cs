using Hellmade.Sound;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonMovement : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

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

    public bool isSprinting = false;


    float timeOffGround = 0f;

    public float speedMagnitude;
    float speedMagnitudePrevious;
    private bool _rotateOnMove = true;

    Vector3 inputMovement;
    public AudioClip sonicBoomClip;

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

        //Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 direction = new Vector3(inputMovement.x, 0f, inputMovement.y);
        if(!isFlying)
        {
            _movement.x = 0;
            _movement.z = 0;
        }


        if(isFlying)
        { // This is needed to prevent movement weirdness when flying

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

            // We're walking backwards, so slow us down.
            if(!_rotateOnMove && Input.GetKey(KeyCode.S))
                moveTransition = new Vector3(moveDir.normalized.x * (speed / 2), _movement.y, moveDir.normalized.z * (speed/2) );

            _movement = moveTransition;

            // animationController.playerMovementAnimation(new Vector2(moveTransition.normalized.x, moveTransition.normalized.z));


            walkSoundTimerTrack += Time.deltaTime;
            if(walkSoundTimerTrack >= walkSoundTimer)
            {
                walkSoundTimerTrack = 0;
            }
            
            /*
            //
            if(controller.isGrounded)
                SoundManager.PlaySound(SoundManager.Sound.PlayerMove);
            //
            */
        }


        
        if (controller.isGrounded)
        { // Landed
            animationController.isGroundedFunc(true);
            animationController.JumpingAnimation(false);
            isJumping = false;
            isFlying = false;
            _movement.y = -0.3f;
        }

        if (controller.isGrounded && playerInput.actions["Jump"].WasPressedThisFrame())
            CharacterJump();

        if (timeOffGround > 0.1 && playerInput.actions["Jump"].WasPressedThisFrame() && !isFlying)
            CharacterFly();


        if (animationController.FlyingAnimation())
        { // Flying Animation Facing, needs lerping, not sure how currently.
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

    public void CharacterJump()
    {
        _movement.y = jumpSpeed;
        isJumping = true;
        animationController.JumpingAnimation(true);
        animationController.isGroundedFunc(false);
    }

    public void CharacterFly()
    {
        animationController.FlyingAnimation(true);
        isFlying = true;
    }

    public void SetRotateOnMove(bool newRotateOnMove)
    {
        _rotateOnMove = newRotateOnMove;
    }

    void Flying()
    {
        // Air Drag
        _movement = Vector3.Lerp(_movement, new Vector3(0, 0, 0), Time.deltaTime * airDrag);

        var oldLook = controller.transform.rotation;
        var lookPosition = controller.transform.position + Camera.main.transform.forward;

        Vector3 flyDirection = new Vector3();
        var input = playerInput.actions["Movement"].ReadValue<Vector2>();

        controller.transform.LookAt(lookPosition);

        if (input.y > 0)
        {
            
            flyDirection += controller.transform.forward * input.y;
        }
        if (input.x > 0)
        {
            flyDirection += controller.transform.right * input.x;
        }
        if (input.y < 0)
        {
            flyDirection += -controller.transform.forward * -input.y;
        }
        if (input.x < 0)
        {
            flyDirection += -controller.transform.right * -input.x;
        }

        if (playerInput.actions["Jump"].IsPressed())
            flyDirection.y += 1;
        if (Input.GetKey(KeyCode.LeftControl))
            flyDirection.y -= 1;

        flyDirection = flyDirection.normalized * (flySpeed * Time.deltaTime);
        _movement += flyDirection;

        controller.transform.rotation = oldLook;
    }

    public void Movement(InputAction.CallbackContext context)
    {
        var inputValue = context.ReadValue<Vector2>();
        inputMovement = inputValue;
    }

    public void InputSprintLogic()
    {
        if (playerInput.actions["Sprint"].WasPressedThisFrame())
            isSprinting = true;

        

        if (inputMovement.magnitude == 0)
            isSprinting = false;

    }

    public void CharacterSprint(bool sprinting)
    {
        if(sprinting)
        {

            if (speed <= GetSprintSpeed())
                speed = Mathf.Lerp(speed, GetSprintSpeed(), Time.deltaTime);

            if (flySpeed <= GetFlySprintSpeed())
                flySpeed = Mathf.Lerp(flySpeed, GetFlySprintSpeed(), Time.deltaTime);

            // Hyperspeed Power: Making sonic boom and turning on particles when we go fast.
            if (hyperSpeed && (speedMagnitude > defaultSpeed || speedMagnitude > defaultFlySpeed))
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

                if (sonicBoomTime && !wentSuperSonic)
                {
                    wentSuperSonic = true;
                    particleTrails.Play();
                    int audioID = EazySoundManager.PlaySound(sonicBoomClip, 10, false, transform);
                    EazySoundManager.GetAudio(audioID).Min3DDistance = 10;
                }
            }

            // Disabling our particles temporarily when we're not going super fast.
            if (hyperSpeed)
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
    }

    private void FixedUpdate()
    {
        if (controller != null)
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
        InputSprintLogic();
        CharacterSprint(isSprinting);



        Move();
        speedMagnitudePrevious = speedMagnitude;
        speedMagnitude = controller.velocity.magnitude;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
