using UnityEngine;
using System.Collections;

namespace Fraktalia.Core.LMS
{
    public class Decay : MonoBehaviour
    {

        public float decaytime;


        void Start()
        {
            StartCoroutine(DecayAway());
        }

        IEnumerator DecayAway()
        {
            yield return new WaitForSeconds(decaytime);

            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter)
            {
                DestroyImmediate(filter.sharedMesh);
            }



            Destroy(gameObject);
        }

        void OnDestroy()
        {
            StopCoroutine(DecayAway());
        }
    }
}