using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PhysicsBasedCharacterController
{
    [RequireComponent(typeof(Rigidbody))]
    public class RBMovingObject : MonoBehaviour
    {
        public bool GainMomentumOnExitCollision = true;

        private List<Rigidbody> rigidbodies = new List<Rigidbody>();

        private Vector3 lastEulerAngles;
        private Vector3 lastPosition;
        private Transform _myTransform;
        private Rigidbody _myRB;


        /**/


        private void Awake()
        {
            _myTransform = this.GetComponent<Transform>();
            lastPosition = _myTransform.position;
            lastEulerAngles = _myTransform.eulerAngles;
            _myRB = this.GetComponent<Rigidbody>();
        }


        private void FixedUpdate()
        {
            UpdateBodies();
        }


        #region Platform and Rigidbody
        private void UpdateBodies()
        {
            if (rigidbodies.Count > 0)
            {
                Vector3 velocity = _myTransform.position - lastPosition;
                Vector3 angularVelocity = _myTransform.eulerAngles - lastEulerAngles;

                for (int i = 0; i < rigidbodies.Count; i++)
                {
                    Rigidbody rb = rigidbodies[i];

                    rb.transform.Translate(velocity, Space.World);
                    rb.transform.RotateAround(_myTransform.position, Vector3.up, angularVelocity.y);
                }
            }

            lastPosition = _myTransform.position;
            lastEulerAngles = _myTransform.eulerAngles;
        }

        #endregion


        #region Handle list

        public void Add(Rigidbody _rb)
        {
            if (!rigidbodies.Contains(_rb)) rigidbodies.Add(_rb);
        }


        public void Remove(Rigidbody _rb)
        {
            if (rigidbodies.Contains(_rb))
            {
                if (GainMomentumOnExitCollision && _myRB.velocity.magnitude > 0) _rb.velocity += _myRB.velocity;
                
                rigidbodies.Remove(_rb);
            }
                
                
        }

        #endregion
    }
}