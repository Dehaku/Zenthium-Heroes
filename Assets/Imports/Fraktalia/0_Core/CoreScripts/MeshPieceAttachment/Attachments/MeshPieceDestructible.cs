using UnityEngine;
using System.Collections;

namespace Fraktalia.Core.LMS
{
    public class MeshPieceDestructible : MeshPieceAttachment
    {

        public override void Effect(GameObject piece)
        {
            piece.AddComponent<DestroyOnCollision>();
        }
    }
}
