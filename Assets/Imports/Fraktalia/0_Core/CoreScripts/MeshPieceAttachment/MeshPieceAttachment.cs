using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Fraktalia.Core.LMS
{
    public class MeshPieceAttachment : MonoBehaviour
    {
		public bool TargetSpecificSlot;
		public List<int> TargetSlots = new List<int>();

		public virtual void PreProcessing()
		{

		}

        public virtual void Effect(GameObject piece)
        {

        }

		public virtual void UpdatePiece(GameObject piece, Mesh proceduralMesh) { }
    }
}

