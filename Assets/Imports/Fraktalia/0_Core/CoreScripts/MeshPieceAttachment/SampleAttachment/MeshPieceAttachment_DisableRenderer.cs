using UnityEngine;
using System.Collections;


namespace Fraktalia.Core.LMS
{
    public class MeshPieceAttachment_DisableRenderer : MeshPieceAttachment
    {
		
        public override void Effect(GameObject piece)
        {	
			MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
			if (renderer)
			{
				renderer.enabled = false;			
			}
		}
	
	}
}
