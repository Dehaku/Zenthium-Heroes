using UnityEngine;


namespace PhysicsBasedCharacterController
{
    [RequireComponent(typeof(Collider))]
    public class PlatformSensor : MonoBehaviour
    {
        private MovingPlatform movingPlatform;
        private BoxCollider boxCollider;


        /**/


        private void Awake()
        {
            movingPlatform = this.transform.parent.GetComponent<MovingPlatform>();
        }


        #region Collision detection

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (rigidbody != null && rigidbody != movingPlatform.GetComponent<Rigidbody>()) movingPlatform.Add(rigidbody);
        }


        private void OnTriggerExit(Collider other)
        {
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (rigidbody != null && rigidbody != movingPlatform.GetComponent<Rigidbody>()) movingPlatform.Remove(rigidbody);
        }

        #endregion
    }
}