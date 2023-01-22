using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyCarry : MonoBehaviour
{
    public bool KeepMomentumOnExitCollision = true;
    public bool KeepRotationMomentumOnExitCollision = true;

    [SerializeField] Transform _myTransform;
    [SerializeField] Rigidbody _myRB;

    private Vector3 lastEulerAngles;
    private Vector3 lastPosition;

    [SerializeField] private Transform _carrierTransform;
    [SerializeField] private Rigidbody _carrierRB;
    

    // Start is called before the first frame update
    void Awake()
    {
        if (!_myRB)
            Debug.LogError("No RB assigned");
        if (!_myTransform)
            Debug.LogError("No Transform assigned");

        Debug.Log("Replace collision checks with raytraces and layermask checks");
    }

    // Update is called once per frame
    void Update()
    {
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

    #region CollisionChecks


    public void Add(Rigidbody _rb)
    {
        _carrierRB = _rb;
        _carrierTransform = _rb.transform;
        lastPosition = _carrierTransform.position;
        lastEulerAngles = _carrierTransform.eulerAngles;
    }


    public void Remove(Rigidbody _rb)
    {
        if (KeepMomentumOnExitCollision && _myRB.velocity.magnitude > 0)
            _myRB.velocity += _rb.velocity;
        
        _carrierRB = null;
    }

    private void OnCollisionEnter(Collision other)
    {
        Rigidbody _rb = other.collider.attachedRigidbody;
        
        if (!_rb)
            return;

        if (_rb != _myRB)
        {
            Add(_rb);
        }
    }

    private void OnCollisionExit(Collision other)
    {
        Rigidbody _rb = other.collider.attachedRigidbody;

        if (!_rb)
            return;

        if (_rb != _myRB)
        {
            Remove(_rb);
        }
    }

    #endregion
}
