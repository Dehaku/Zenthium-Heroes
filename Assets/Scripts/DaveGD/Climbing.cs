using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Climbing : MonoBehaviour
{


    [Header("Climbing")]
    public bool allowClimbingWhileGrounded = false;
    public float climbSpeed;
    public float maxClimbTime;
    private float climbTimer;

    private bool climbing;

    [Header("ClimbJumping")]
    public float climbJumpUpForce;
    public float climbJumpBackForce;

    public int climbJumps;
    public int climbJumpsLeft;

    [Header("Detection")]
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;

    private RaycastHit frontWallHit;
    private bool wallFront;

    private Transform lastWall;
    private Vector3 lastWallNormal;
    public float minWallNormalAngleChange;

    [Header("Exiting")]
    public bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;

    [Header("References")]
    public PlayerInput playerInput;
    public Transform orientation;
    public Rigidbody rb;
    public PlayerMovementAdvanced pm;
    public LedgeGrabbing lg;
    public LayerMask whatIsWall;
    
    [Header("Optional")]
    public Transform sphereCastPosition;

    public float ClimbTimer { get => climbTimer; set => climbTimer = value; }

    private void Start()
    {
        lg = GetComponent<LedgeGrabbing>();
        Debug.Log("Add x/z drag while climbing.");
    }

    private void Update()
    {
        WallCheck();
        StateMachine();

        if (climbing && !exitingWall) ClimbingMovement();
    }

    private void StateMachine()
    {
        // State 0 - Ledge Grabbing
        if(lg.holding)
        {
            if (climbing) StopClimbing();

        }

        // State 1 - Climbing
        else if (wallFront && (playerInput.actions["Movement"].ReadValue<Vector2>().y > 0.05f) && 
            wallLookAngle < maxWallLookAngle && !exitingWall &&
            !pm.swinging)
        {
            if (!climbing && climbTimer > 0)
            {
                
                if (!allowClimbingWhileGrounded)
                {
                    if (!pm.grounded)
                        StartClimbing();
                }
                else
                    StartClimbing();

            }

            // timer
            if (climbTimer > 0) climbTimer -= Time.deltaTime;
            if (climbTimer < 0) StopClimbing();
        }

        // State 2 - Exiting
        else if (exitingWall)
        {
            if (climbing) StopClimbing();

            if (exitWallTimer > 0) exitWallTimer -= Time.deltaTime;
            if (exitWallTimer < 0) exitingWall = false;
        }

        // State 3 - None
        else
        {
            if (climbing) StopClimbing();
        }

        if (wallFront && playerInput.actions["Jump"].WasPressedThisFrame() && climbJumpsLeft > 0 && !exitingWall) ClimbJump();
    }

    private void WallCheck()
    {
        if(sphereCastPosition)
            wallFront = Physics.SphereCast(sphereCastPosition.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsWall);
        else
            wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsWall);
        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);

        bool newWall = frontWallHit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

        if ((wallFront && newWall) || pm.grounded)
        {
            climbTimer = maxClimbTime;
            climbJumpsLeft = climbJumps;
        }
    }

    private void StartClimbing()
    {
        climbing = true;
        pm.climbing = true;

        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;

        /// idea - camera fov change
    }

    private void ClimbingMovement()
    {
        rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);

        /// idea - sound effect
    }

    private void StopClimbing()
    {
        climbing = false;
        pm.climbing = false;

        /// idea - particle effect
        /// idea - sound effect
    }

    private void ClimbJump()
    {
        if (pm.grounded) return;
        if (lg.holding || lg.exitingLedge) return;

        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 forceToApply = transform.up * climbJumpUpForce + frontWallHit.normal * climbJumpBackForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);

        climbJumpsLeft--;
    }
}
