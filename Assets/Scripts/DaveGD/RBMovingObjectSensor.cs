using UnityEngine;


namespace PhysicsBasedCharacterController
{
    [RequireComponent(typeof(Collider))]
    public class RBMovingObjectSensor : MonoBehaviour
    {
        public RBMovingObject rbMovingObject;

        /**/


        private void Awake()
        {
            //rbMovingObject = this.transform.parent.GetComponent<RBMovingObject>();
            if (!rbMovingObject)
                Debug.LogError(transform.parent.name + "No rbMovingObject assigned!");
        }


        #region Collision detection

        private void OnTriggerEnter(Collider other)
        {
            
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (rigidbody != null && rigidbody != rbMovingObject.GetComponent<Rigidbody>())
            {
                rbMovingObject.Add(rigidbody);
                return;
            }
            //if(other.attachedRigidbody != null && other.attachedRigidbody != movingPlatform.GetComponent<Rigidbody>()) movingPlatform.Add(rigidbody);
            Debug.Log("Trigger In: " + other.attachedRigidbody);
            if (other.attachedRigidbody)
            {
                rbMovingObject.Add(other.attachedRigidbody);
                Debug.Log("Name of : " + other.attachedRigidbody.name);
            }
                
        }


        private void OnTriggerExit(Collider other)
        {
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (rigidbody != null && rigidbody != rbMovingObject.GetComponent<Rigidbody>())
            {
                rbMovingObject.Remove(rigidbody);
                return;
            }
            if (other.attachedRigidbody)
            {
                rbMovingObject.Remove(other.attachedRigidbody);
                return;
            }
        }

        #endregion
    }
}