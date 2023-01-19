using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DualHooks : MonoBehaviour
{
    [Header("References")]
    public List<Transform> gunTips;
    public Transform cam;
    public Transform player;
    public LayerMask whatIsGrappleable;
    public PlayerMovementAdvanced pm;
    public ThirdPersonCam camScript;

    [Header("Swinging")]
    public float maxSwingDistance = 25f;
    public List<Vector3> swingPoints;
    private List<SpringJoint> joints;

    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    public float overshootYAxis;

    public List<bool> grapplesActive;

    [Header("Cooldown")]
    public float grapplingCd;
    private float grapplingCdTimer;

    [Header("OdmGear")]
    public Transform orientation;
    public Rigidbody rb;
    public float horizontalThrustForce;
    public float forwardThrustForce;
    public float extendCableSpeed;

    [Header("Prediction")]
    public List<RaycastHit> predictionHits;
    public List<Transform> predictionPoints;
    public float predictionSphereCastRadius;

    [Header("Input")]
    public PlayerInput playerInput;
    public KeyCode swingKey1 = KeyCode.Mouse0;
    public KeyCode swingKey2 = KeyCode.Mouse1;


    [Header("DualSwinging")]
    public int amountOfSwingPoints = 2;
    public List<Transform> pointAimers;
    public List<bool> swingsActive;

    private void Start()
    {
        camScript.onCameraSwitch += SwapAimPointsOnCameraSwap;

        ListSetup();
    }

    void SwapAimPointsOnCameraSwap(List<GameObject> go)
    {
        Debug.Log("Camera Swapped!");
        pointAimers.Clear();
        foreach (var item in go)
        {
            pointAimers.Add(item.transform);
        }
    }

    private void ListSetup()
    {
        predictionHits = new List<RaycastHit>();

        swingPoints = new List<Vector3>();
        joints = new List<SpringJoint>();

        swingsActive = new List<bool>();
        grapplesActive = new List<bool>();

        //currentGrapplePositions = new List<Vector3>();

        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            predictionHits.Add(new RaycastHit());
            joints.Add(null);
            swingPoints.Add(Vector3.zero);
            swingsActive.Add(false);
            grapplesActive.Add(false);
            //currentGrapplePositions.Add(Vector3.zero);
        }
    }

    private void Update()
    {
        MyInput();
        CheckForSwingPoints();

        if (joints[0] != null || joints[1] != null) OdmGearMovement();

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    

    private void MyInput()
    {
        
    }

    private void CheckForSwingPoints()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            if (swingsActive[i]) { /* Do Nothing */ }
            else
            {
                RaycastHit sphereCastHit;
                Physics.SphereCast(pointAimers[i].position, predictionSphereCastRadius, pointAimers[i].forward,
                                    out sphereCastHit, maxSwingDistance, whatIsGrappleable);

                RaycastHit raycastHit;
                Physics.Raycast(cam.position, cam.forward,
                                    out raycastHit, maxSwingDistance, whatIsGrappleable);

                Vector3 realHitPoint;

                // Option 1 - Direct Hit
                if (raycastHit.point != Vector3.zero)
                    realHitPoint = raycastHit.point;

                // Option 2 - Indirect (predicted) Hit
                else if (sphereCastHit.point != Vector3.zero)
                    realHitPoint = sphereCastHit.point;

                // Option 3 - Miss
                else
                    realHitPoint = Vector3.zero;

                // realHitPoint found
                if (realHitPoint != Vector3.zero)
                {
                    predictionPoints[i].gameObject.SetActive(true);
                    predictionPoints[i].position = realHitPoint;
                }
                // realHitPoint not found
                else
                {
                    predictionPoints[i].gameObject.SetActive(false);
                }

                predictionHits[i] = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
            }
        }
    }

    #region Swinging

    private void StartSwing(int swingIndex)
    {
        Debug.Log("Swing: " + swingIndex);
        // return if predictionHit not found
        if (predictionHits[swingIndex].point == Vector3.zero) return;

        // deactivate active grapple
        CancelActiveGrapples();
        pm.ResetRestrictions();

        pm.swinging = true;
        swingsActive[swingIndex] = true;

        swingPoints[swingIndex] = predictionHits[swingIndex].point;
        joints[swingIndex] = player.gameObject.AddComponent<SpringJoint>();
        joints[swingIndex].autoConfigureConnectedAnchor = false;
        joints[swingIndex].connectedAnchor = swingPoints[swingIndex];

        float distanceFromPoint = Vector3.Distance(player.position, swingPoints[swingIndex]);

        // the distance grapple will try to keep from grapple point. 
        joints[swingIndex].maxDistance = distanceFromPoint * 0.8f;
        joints[swingIndex].minDistance = distanceFromPoint * 0.25f;

        // customize values as you like
        joints[swingIndex].spring = 15f;
        joints[swingIndex].damper = 7f;
        joints[swingIndex].massScale = 4.5f;

        //lineRenderers[swingIndex].positionCount = 2;
        //currentGrapplePositions[swingIndex] = gunTips[swingIndex].position;
    }

    public void StopSwing(int swingIndex)
    {
        pm.swinging = false;

        swingsActive[swingIndex] = false;

        Destroy(joints[swingIndex]);
    }

    #endregion

    #region Grappling

    private void StartGrapple(int grappleIndex)
    {
        if (grapplingCdTimer > 0) return;

        CancelActiveSwings();
        CancelAllGrapplesExcept(grappleIndex);

        // Case 1 - target point found
        if (predictionHits[grappleIndex].point != Vector3.zero)
        {
            Invoke(nameof(DelayedFreeze), 0.05f);

            grapplesActive[grappleIndex] = true;

            swingPoints[grappleIndex] = predictionHits[grappleIndex].point;

            StartCoroutine(ExecuteGrapple(grappleIndex));
        }

        // Case 2 - target point not found
        else
        {
            swingPoints[grappleIndex] = cam.position + cam.forward * maxGrappleDistance;

            StartCoroutine(StopGrapple(grappleIndex, grappleDelayTime));
        }

    }

    private void DelayedFreeze()
    {
        pm.freeze = true;
    }

    private IEnumerator ExecuteGrapple(int grappleIndex)
    {
        yield return new WaitForSeconds(grappleDelayTime);

        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = swingPoints[grappleIndex].y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(swingPoints[grappleIndex], highestPointOnArc);

        //StartCoroutine(StopGrapple(grappleIndex, 1f));
    }

    public IEnumerator StopGrapple(int grappleIndex, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        pm.freeze = false;

        pm.ResetRestrictions();

        grapplesActive[grappleIndex] = false;

        grapplingCdTimer = grapplingCd;
    }

    #endregion

    #region OdmGear

    private Vector3 pullPoint;
    private void OdmGearMovement()
    {
        if (swingsActive[0] && !swingsActive[1]) pullPoint = swingPoints[0];
        if (swingsActive[1] && !swingsActive[0]) pullPoint = swingPoints[1];
        // get midpoint if both swing points are active
        if (swingsActive[0] && swingsActive[1])
        {
            Vector3 dirToGrapplePoint1 = swingPoints[1] - swingPoints[0];
            pullPoint = swingPoints[0] + dirToGrapplePoint1 * 0.5f;
        }

        // right
        if (playerInput.actions["Movement"].ReadValue<Vector2>().x > 0.05f) 
            rb.AddForce(orientation.right * horizontalThrustForce * Time.deltaTime);
        // left
        if (playerInput.actions["Movement"].ReadValue<Vector2>().x < -0.05f) 
            rb.AddForce(-orientation.right * horizontalThrustForce * Time.deltaTime);
        // forward
        if (playerInput.actions["Movement"].ReadValue<Vector2>().y > 0.05f) 
            rb.AddForce(orientation.forward * forwardThrustForce * Time.deltaTime);
        // backward
        if (playerInput.actions["Movement"].ReadValue<Vector2>().y < -0.05f) 
            rb.AddForce(-orientation.forward * forwardThrustForce * Time.deltaTime);
        // shorten cable
        if (playerInput.actions["Jump"].IsPressed())
        {
            Vector3 directionToPoint = pullPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            // calculate the distance to the grapplePoint
            float distanceFromPoint = Vector3.Distance(transform.position, pullPoint);

            // the distance grapple will try to keep from grapple point.
            UpdateJoints(distanceFromPoint);
        }
        // extend cable
        if (playerInput.actions["CrouchSlide"].IsPressed())
        {
            // calculate the distance to the grapplePoint
            float extendedDistanceFromPoint = Vector3.Distance(transform.position, pullPoint) + extendCableSpeed;

            // the distance grapple will try to keep from grapple point.
            UpdateJoints(extendedDistanceFromPoint);
        }
    }

    private void UpdateJoints(float distanceFromPoint)
    {
        for (int i = 0; i < joints.Count; i++)
        {
            if (joints[i] != null)
            {
                joints[i].maxDistance = distanceFromPoint * 0.8f;
                joints[i].minDistance = distanceFromPoint * 0.25f;
            }
        }
    }

    #endregion

    #region CancelAbilities

    public void CancelActiveGrapples()
    {
        StartCoroutine(StopGrapple(0));
        StartCoroutine(StopGrapple(1));
    }

    private void CancelAllGrapplesExcept(int grappleIndex)
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
            if (i != grappleIndex) StartCoroutine(StopGrapple(i));
    }

    private void CancelActiveSwings()
    {
        StopSwing(0);
        StopSwing(1);
    }

    #endregion

    #region Visualisation





    #endregion


    #region inputfunctions

    public void SwingLeft(InputAction.CallbackContext context)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (playerInput.actions["GrappleModifier"].IsPressed())
            return;

        if (context.started)
            StartSwing(0);

        if (context.canceled)
            StopSwing(0);
    }

    public void GrappleLeft(InputAction.CallbackContext context)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (!playerInput.actions["GrappleModifier"].IsPressed())
            return;

        if (context.started)
            StartGrapple(0);
    }

    public void SwingRight(InputAction.CallbackContext context)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (playerInput.actions["GrappleModifier"].IsPressed())
            return;

        if (context.started)
            StartSwing(1);

        if (context.canceled)
            StopSwing(1);
    }

    public void GrappleRight(InputAction.CallbackContext context)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (!playerInput.actions["GrappleModifier"].IsPressed())
            return;

        if (context.started)
            StartGrapple(1);
    }

    #endregion

}
