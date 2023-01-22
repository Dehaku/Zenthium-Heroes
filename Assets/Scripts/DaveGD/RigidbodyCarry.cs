using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyCarry : MonoBehaviour
{

    public bool keepMomentumOnExitCollision = true;
    public bool keepRotationMomentumOnExitCollision = true;

    [SerializeField] Transform _myTransform;
    [SerializeField] Rigidbody _myRB;

    private Vector3 lastEulerAngles;
    private Vector3 lastPosition;

    [SerializeField] private Transform _carrierTransform;
    [SerializeField] private Rigidbody _carrierRB;

    [Header("Raycast Or Collision")]
    public bool useRayCastInsteadOfCollision = false;
    public bool allowHorizontalRays = false;
    public float downRayCastLength = 1f;
    public float horizontalRayCastLength = 1f;
    public LayerMask carrierLayerMasks;
    public Transform alternateCastPosition;

    [Space(5)]
    public bool drawDebugRaycastLines = false;

    Vector3[] vectorDirections;


    // Start is called before the first frame update
    void Awake()
    {
        if (!_myRB)
            Debug.LogError("No RB assigned");
        if (!_myTransform)
            Debug.LogError("No Transform assigned");

        // Filling in vector positions for Horizontal checks. Added vertical ones for fun.
        vectorDirections = new Vector3[10];
        // Down
        vectorDirections[0] = new Vector3(0, -1, 0);
        // Left
        vectorDirections[1] = new Vector3(-1, 0, 0);
        // ForwardLeft
        vectorDirections[2] = new Vector3(-1, 0, 1);
        // Forward
        vectorDirections[3] = new Vector3(0, 0, 1);
        // ForwardRight
        vectorDirections[4] = new Vector3(1, 0, 1);
        // Right
        vectorDirections[5] = new Vector3(1, 0, 0);
        // RightBack
        vectorDirections[6] = new Vector3(1, 0, -1);
        // Back
        vectorDirections[7] = new Vector3(0, 0, -1);
        // BackLeft
        vectorDirections[8] = new Vector3(-1, 0, -1);
        // Up
        vectorDirections[9] = new Vector3(0, 1, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (useRayCastInsteadOfCollision)
            RaycastChecks();


        if (_carrierRB)
            Debug.Log("Carrier: " + _carrierRB.name);
        else
            Debug.Log("Carrier: None");
    }

    private void FixedUpdate()
    {
        UpdateBodies();
    }


    #region Platform and Rigidbody
    private void UpdateBodies()
    {
        if (_carrierRB)
        {
            Vector3 velocity = _carrierTransform.position - lastPosition;
            Vector3 angularVelocity = _carrierTransform.eulerAngles - lastEulerAngles;

            _myRB.transform.Translate(velocity, Space.World);
            _myRB.transform.RotateAround(_carrierTransform.position, Vector3.up, angularVelocity.y);

            lastPosition = _carrierTransform.position;
            lastEulerAngles = _carrierTransform.eulerAngles;
        }
    }

    #endregion

    #region CollisionAndRaycastChecks


    public void SetCarrier(Rigidbody _rb)
    {
        _carrierRB = _rb;
        _carrierTransform = _rb.transform;
        lastPosition = _carrierTransform.position;
        lastEulerAngles = _carrierTransform.eulerAngles;
    }


    public void RemoveCarrier(Rigidbody _rb)
    {
        if (keepMomentumOnExitCollision && _myRB.velocity.magnitude > 0)
            _myRB.velocity += _rb.velocity;
        
        _carrierRB = null;
    }




    private void RaycastChecks()
    {

        Vector3 castPos = transform.position;
        Vector3 castForward = transform.forward;
        Vector3 castUp = transform.up;
        Vector3 castRight = transform.right;
        if (alternateCastPosition)
        {
            castPos = alternateCastPosition.position;
            castForward = alternateCastPosition.forward;
            castUp = alternateCastPosition.up;
            castRight = alternateCastPosition.right;
        }

        bool anyHits = false;
        RaycastHit raycastHit;

        // =====Down=====
        // Downcast should be universal, if not, feel free to add a bool here.
        Physics.Raycast(castPos, -castUp,
                            out raycastHit, downRayCastLength, carrierLayerMasks);

        if (raycastHit.collider)
            anyHits = RaycastHit(raycastHit.collider);

        if (drawDebugRaycastLines)
        {
            if(raycastHit.collider)
                Debug.DrawLine(castPos, raycastHit.point, Color.green);
            else
                Debug.DrawLine(castPos, castPos + (-castUp * downRayCastLength), Color.red);
        }

        // =====Horizontal=====
        // If down didn't hit, let's cast around us if allowed.
        for(int i = 1; i < vectorDirections.Length; i++) // Start at 1, since 0 has down.
        {
            //Making sure we're allowed. If we are, making sure we don't have any hits.
            if (!allowHorizontalRays || anyHits)
                break;

            Physics.Raycast(castPos, vectorDirections[i],
                                out raycastHit, horizontalRayCastLength, carrierLayerMasks);

            if (raycastHit.collider)
                anyHits = RaycastHit(raycastHit.collider);

            if (drawDebugRaycastLines)
            {
                if (raycastHit.collider)
                    Debug.DrawLine(castPos, raycastHit.point, Color.green);
                else
                    Debug.DrawLine(castPos, castPos + (vectorDirections[i] * horizontalRayCastLength), Color.red);
            }
        }

        if (anyHits == false)
        {
            if (_carrierRB)
                RemoveCarrier(_carrierRB);
                
        }

    }

    bool RaycastHit(Collider collider)
    {
        bool anyHits = false;

        // Assign hit rigidbody as our carrier
        Rigidbody _rb = collider.attachedRigidbody;
        if (_rb)
        {
            anyHits = true;
            if (_rb != _myRB && _rb != _carrierRB)
                SetCarrier(_rb);
        }

        return anyHits;
    }

    private void OnCollisionEnter(Collision other)
    {
        // Return if we're using rays instead.
        if (useRayCastInsteadOfCollision) 
            return;

        Rigidbody _rb = other.collider.attachedRigidbody;
        
        if (!_rb)
            return;

        if (_rb != _myRB)
            SetCarrier(_rb);
    }

    private void OnCollisionExit(Collision other)
    {
        // Return if we're using rays instead.
        if (useRayCastInsteadOfCollision)
            return;

        Rigidbody _rb = other.collider.attachedRigidbody;

        if (!_rb)
            return;

        if (_rb != _myRB)
            RemoveCarrier(_rb);
    }

    #endregion
}
