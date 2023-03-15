using UnityEngine;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.Core.LMS
{
    public class LargeMeshSelector : MonoBehaviour
    {
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {

            if (Selection.activeGameObject == gameObject)
            {
                LargeMeshCombiner realparent = GetComponentInParent<LargeMeshCombiner>();
                if (realparent && realparent.HideInHierachy)
                {
                    if (realparent) Selection.activeObject = realparent.gameObject;
                }
            }
        }
#endif
    }

}
