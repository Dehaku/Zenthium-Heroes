using UnityEngine;
using System.Collections;

namespace Fraktalia.Core.LMS
{
    public class DestroyOnCollision : MonoBehaviour
    {

        void OnCollisionEnter(Collision collision)
        {
            Destroy(gameObject);
        }
    }
}
