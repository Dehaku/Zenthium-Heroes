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
        private Transform transform;
        private Rigidbody rigidbody;


        /**/


        private void Awake()
        {
            transform = this.GetComponent<Transform>();
            lastPosition = transform.position;
            lastEulerAngles = transform.eulerAngles;
            rigidbody = this.GetComponent<Rigidbody>();
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
                Vector3 velocity = transform.position - lastPosition;
                Vector3 angularVelocity = transform.eulerAngles - lastEulerAngles;

                for (int i = 0; i < rigidbodies.Count; i++)
                {
                    Rigidbody rb = rigidbodies[i];

                    //rb.transform.RotateAround(transform.position, Vector3.up, angularVelocity.y);

                    //rb.position += velocity;
                    rb.transform.Translate(velocity, Space.World);
                    rb.transform.RotateAround(transform.position, Vector3.up, angularVelocity.y);

                    string text = "";
                    if (angularVelocity.y > 0.1f || angularVelocity.y < -0.1f)
                    {
                        Debug.Log("Spinning");
                            
                        text += rb.position.ToString("F4") + " : ";
                        //rb.transform.RotateAround(transform.position, Vector3.up, angularVelocity.y);
                        text += rb.position.ToString("F4");
                        
                        //rb.transform.RotateAround(transform.position, Vector3.up, angularVelocity.y);
                    }
                    else
                    {
                        //rb.position += velocity;
                    }

                    //if (rigidbody.velocity.magnitude > 0) rb.velocity += rigidbody.velocity;

                    
                    text += " : " + rb.position.ToString("F4");
                    Debug.Log(text);
                    if (rb.name == "Player")
                        Debug.Log(rb.name + ", v: " + velocity.ToString("F4") + ", a:" + angularVelocity.ToString("F4"));
                }
            }
            Debug.Log("RBs: " + rigidbodies.Count);

            lastPosition = transform.position;
            lastEulerAngles = transform.eulerAngles;
        }

        #endregion


        #region Handle list

        public void Add(Rigidbody _rb)
        {
            Debug.Log("Adding: " + _rb.name);
            if (!rigidbodies.Contains(_rb)) rigidbodies.Add(_rb);
        }


        public void Remove(Rigidbody _rb)
        {
            if (rigidbodies.Contains(_rb))
            {
                if (GainMomentumOnExitCollision && rigidbody.velocity.magnitude > 0) _rb.velocity += rigidbody.velocity;
                rigidbodies.Remove(_rb);
            }
                
                
        }

        #endregion
    }
}