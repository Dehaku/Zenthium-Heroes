using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyCarry : MonoBehaviour
{

    public bool allowNonRigidbodyCarriersToo = false;
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
    public bool useSidewaysRays = false;
    public bool useFrontAndBackRays = false;
    public float rayCastLength = 1f;
    public LayerMask carrierLayerMasks;
    public Transform alternateCastPosition;


    // Start is called before the first frame update
    void Awake()
    {
        if (!_myRB)
            Debug.LogError("No RB assigned");
        if (!_myTransform)
            Debug.LogError("No Transform assigned");

        Debug.Log("Swap to allow nonrigidbody carriers for COLLISION too");
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
        Debug.Log("Add third velocity tracker to allow for transform based exits");
        
        _carrierRB = null;
    }


    private void RaycastChecks()
    {

        /*
          public bool useRayCastInsteadOfCollision = false;
          public bool useSidewaysRays = false;
          public bool useFrontAndBackRays = false;
          public float rayCastLength = 1f;
          public LayerMask carrierLayerMasks;
          public Transform alternateCastPosition;
        */

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
        // Downcast should be universal, if not, feel free to add a bool here.
        Physics.Raycast(castPos, -castUp,
                            out raycastHit, rayCastLength, carrierLayerMasks);
        
        if (raycastHit.collider)
        {
            

            // Do we allow moving objects without rigidbodies?
            if(allowNonRigidbodyCarriersToo)
            {
                anyHits = true;
                Debug.Log("Not implemented yet");
            }

            // Assign hit rigidbody as our carrier
            Rigidbody _rb = raycastHit.collider.attachedRigidbody;
            if (_rb)
            {
                anyHits = true;
                if (_rb != _myRB && _rb != _carrierRB)
                    SetCarrier(_rb);
            }
                
        }

        

        if(anyHits == false)
        {
            if (_carrierRB)
                RemoveCarrier(_carrierRB);
                
        }

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
