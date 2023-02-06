using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LedgeGrabbing : MonoBehaviour
{
    [Header("References")]
    public PlayerInput playerInput;
    public PlayerMovementAdvanced pm;
    public Transform orientation;
    public Transform playerOBJ;
    public Transform cam;
    public Rigidbody rb;

    [Header("Ledge Grabbing")]
    public float moveToLedgeSpeed;
    public float maxLedgeGrabDistance;

    public float minTimeOnLedge;
    private float timeOnLedge;

    public bool faceTowardsLedge = true;

    public bool holding;

    [Header("Ledge Jumping")]
    public float ledgeJumpForwardForce;
    public float ledgeJumpUpwardForce;

    [Header("Ledge Detection")]
    public float ledgeDetectionLength;
    public float ledgeSphereCastRadius;
    public LayerMask whatIsLedge;

    private Transform lastLedge;
    private Transform currLedge;

    private RaycastHit ledgeHit;

    [Header("Exiting")]
    public bool exitingLedge;
    public float exitLedgeTime;
    private float exitLedgeTimer;

    private void Update()
    {
        LedgeDetection();
        SubStateMachine();
    }

    void FaceToLedge()
    {
        if (!currLedge)
            return;
        if (!ledgeHit.collider)
            return;

        Vector3 ledgeDirection = transform.position + -ledgeHit.normal;
        //ledgeDirection.z = 0;
        playerOBJ.LookAt(ledgeDirection);
    }

    private void SubStateMachine()
    {
        float horizontalInput = playerInput.actions["Movement"].ReadValue<Vector2>().x;
        float verticalInput = playerInput.actions["Movement"].ReadValue<Vector2>().y;
        bool anyInputKeyPressed = horizontalInput != 0 || verticalInput != 0;

        pm.hanging = holding;

        // SubState 1 - Holding onto ledge
        if (holding)
        {
            FreezeRigidbodyOnLedge();

            if(faceTowardsLedge)
                FaceToLedge();

            timeOnLedge += Time.deltaTime;

            if (timeOnLedge > minTimeOnLedge && anyInputKeyPressed) ExitLedgeHold();

            if (playerInput.actions["Jump"].WasPressedThisFrame()) LedgeJump();
        }

        // Substate 2 - Exiting Ledge
        else if (exitingLedge)
        {
            if (exitLedgeTimer > 0) exitLedgeTimer -= Time.deltaTime;
            else exitingLedge = false;
        }
    }

    private void LedgeDetection()
    {
        bool ledgeDetected = false;
        // Look for new Ledge
        if(!holding)
            ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, orientation.forward, out ledgeHit, ledgeDetectionLength, whatIsLedge);
        
        // Aim at our Current ledge instead, so we can look away without breaking it.
        if(!ledgeDetected && holding)
            ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, (ledgeHit.point-transform.position).normalized, out ledgeHit, ledgeDetectionLength, whatIsLedge);
        if (!ledgeDetected) return;

        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.point);

        if (ledgeHit.transform == lastLedge) return;

        if (distanceToLedge < maxLedgeGrabDistance && !holding) EnterLedgeHold();
    }

    private void LedgeJump()
    {
        ExitLedgeHold();

        Invoke(nameof(DelayedJumpForce), 0.05f);
    }

    private void DelayedJumpForce()
    {
        
        Vector3 forceToAdd = cam.forward * ledgeJumpForwardForce + orientation.up * ledgeJumpUpwardForce;
        //Debug.Log((cam.forward * ledgeJumpForwardForce) + " : " + (orientation.up * ledgeJumpUpwardForce) + ": " + forceToAdd);
        //Debug.Log(forceToAdd);
        rb.velocity = Vector3.zero;
        rb.AddForce(forceToAdd, ForceMode.Impulse);
    }

    private void EnterLedgeHold()
    {
        holding = true;

        //pm.unlimited = true;
        pm.restricted = true;

        currLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;

        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }

    private void FreezeRigidbodyOnLedge()
    {
        //if (!ledgeHit.collider)
          //  Debug.Log("No collider found in LedgeHit, something went wrong.");
        rb.useGravity = false;

        Vector3 directionToLedge = currLedge.position - transform.position;
        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.point);

        // Move player towards ledge
        if(distanceToLedge > 1f)
        {
            if(rb.velocity.magnitude < moveToLedgeSpeed)
                rb.AddForce(directionToLedge.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime);
        }

        // Hold onto ledge
        else
        {
            if (!pm.freeze) pm.freeze = true;
            if (pm.unlimited) pm.unlimited = false;
        }

        // Exiting if something goes wrong
        if (distanceToLedge > maxLedgeGrabDistance) ExitLedgeHold();
    }

    private void ExitLedgeHold()
    {
        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;

        holding = false;
        timeOnLedge = 0f;

        pm.restricted = false;
        pm.freeze = false;

        rb.useGravity = true;

        StopAllCoroutines();
        Invoke(nameof(ResetLastLedge), 1f);
    }

    private void ResetLastLedge()
    {
        lastLedge = null;
    }
}
