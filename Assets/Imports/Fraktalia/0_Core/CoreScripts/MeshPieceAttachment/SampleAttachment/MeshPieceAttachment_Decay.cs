using UnityEngine;
using System.Collections;


namespace Fraktalia.Core.LMS
{
    public class MeshPieceAttachment_Decay : MeshPieceAttachment
    {
        public Vector2Int DecayTime_Min_Max;
        private bool Initilized = false;
        public int DecayTime = 0;

        public override void Effect(GameObject piece)
        {
            MeshPieceAttachment_Decay decay = piece.AddComponent<MeshPieceAttachment_Decay>();
            decay.DecayTime_Min_Max = DecayTime_Min_Max;
            decay.Initilized = true;
            decay.DecayTime = Random.Range(DecayTime_Min_Max.x, DecayTime_Min_Max.y);
            
          
        }

        private void FixedUpdate()
        {
            if (!Initilized) return;
            DecayTime--;
            if(DecayTime <= 0)
            {
                Destroy(gameObject);
            }
        }

      
    }
}