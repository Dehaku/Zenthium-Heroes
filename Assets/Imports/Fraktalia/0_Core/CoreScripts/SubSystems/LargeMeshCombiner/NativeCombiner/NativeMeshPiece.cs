using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.Core.LMS
{
    public class NativeMeshPiece : MonoBehaviour
    {
        public MeshFilter meshfilter;
        public MeshRenderer meshrenderer;
        public MeshCollider meshcollider;

		public MeshPieceAttachment[] attachments;
		public int slot;

#if UNITY_EDITOR
		void OnDrawGizmosSelected()
        {

            if (Selection.activeGameObject == gameObject)
            {
                NativeMeshCombiner realparent = GetComponentInParent<NativeMeshCombiner>();
                if (realparent && realparent.HideInHierachy)
                {
                    if (realparent) Selection.activeObject = realparent.gameObject;
                }
            }

        }
#endif

		public void UpdateAttachments()
		{
			if (attachments == null) return;
			for (int i = 0; i < attachments.Length; i++)
			{				
				attachments[i].UpdatePiece(gameObject, meshfilter.sharedMesh);
			}
		}
	}
}

