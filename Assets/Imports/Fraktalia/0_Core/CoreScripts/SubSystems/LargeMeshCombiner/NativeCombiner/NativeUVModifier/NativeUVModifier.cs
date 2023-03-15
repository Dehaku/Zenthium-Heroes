using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace Fraktalia.Core.LMS
{
    public class NativeUVModifier : MonoBehaviour
    {
        public void LaunchAlgorithm(
            ref NativeArray<Vector3> positionData,
               ref NativeArray<Vector2> uvData)
        {
            if (HasErrors()) return;
            Algorithm(ref positionData, ref uvData);
        }


        protected virtual void Algorithm(
            ref NativeArray<Vector3> positionData,
               ref NativeArray<Vector2> uvData)
        {

        }    

        public virtual bool HasErrors()
        {
            return false;
        }	
	}
}
