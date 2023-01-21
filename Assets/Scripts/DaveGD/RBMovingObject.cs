﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PhysicsBasedCharacterController
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class RBMovingObject : MonoBehaviour
    {
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

                    if (angularVelocity.y > 0)
                    {
                        if (!rb)
                            Debug.Log("RB Missing!" + name + gameObject.name + ", Size: " + rigidbodies.Count);
                        //rb.transform.RotateAround(transform.position, Vector3.up, angularVelocity.y);
                        try { rb.GetComponent<CharacterManager>().targetAngle = rb.GetComponent<CharacterManager>().targetAngle + angularVelocity.y; }
                        catch { /* Debug.Log("There is no player on the platform") */ }
                    }

                    //if (rigidbody.velocity.magnitude > 0) rb.velocity += rigidbody.velocity;

                    rb.position += velocity;
                    if(rb.name == "Player")
                        Debug.Log(rb.name + ", v: " + velocity + ", a:" + angularVelocity);
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
            if (rigidbodies.Contains(_rb)) rigidbodies.Remove(_rb);
        }

        #endregion
    }
}