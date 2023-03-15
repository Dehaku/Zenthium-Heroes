using UnityEngine;
using System.Collections;

namespace Fraktalia.Core.LMS
{
    public class MeshPieceDecay : MeshPieceAttachment
    {
        public float DecayTime = 1;

        public override void Effect(GameObject piece)
        {
            Decay decay = piece.AddComponent<Decay>();
            decay.decaytime = DecayTime;


        }
    }
}
