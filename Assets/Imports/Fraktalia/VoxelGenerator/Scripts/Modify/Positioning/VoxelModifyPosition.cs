using UnityEngine;
using System.Collections;



namespace Fraktalia.VoxelGen.Modify.Positioning
{
    public class VoxelModifyPosition : MonoBehaviour {
		public float ModifyRadius_Min;
		public float ModifyRadius_Max;

		public virtual float ModifyRadius
		{
			get
			{
				return Random.Range(ModifyRadius_Min, ModifyRadius_Max);
			}
		}

		public LayerMask CollisionLayer;

		public bool NoTargetFound { get; protected set; }
	
		private void OnDrawGizmosSelected()
		{
			Preview();
		}

		//Class to draw gizmos. Is called by the generator.
		public virtual void Preview()
        {
            Gizmos.DrawSphere(transform.position + calculatePosition(), ModifyRadius);
        }

        //Implement here your positioning algorithms etc.
		public Vector3 Calculate()
		{
			NoTargetFound = false;
			return calculatePosition();
		}



        protected virtual Vector3 calculatePosition()
        {
            return new Vector3(0, 0, 0);
        }
    }
}
