using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.VoxelGen.Modify.Positioning
{
    public class VoxelModifyPosition_Beam : VoxelModifyPosition {

        public float Range = 100; 
        

        public float Boundary = 5;
        public float Spread = 0;

       

        public override void Preview()
        {
            Vector3 point = calculatePosition();

            float angleX = Random.Range(0, 360);
           
            
            Gizmos.DrawWireSphere(point + transform.position, Boundary / 100);
            Vector3 direction_local = new Vector3(0, 0, 0);
            Vector2 expansion = VoxelMath.kreisPosition(angleX) * Boundary * Random.Range(0.0f, 1.0f);
            direction_local.x += expansion.x;
            direction_local.y += expansion.y;
            Vector3 startpos = transform.localToWorldMatrix * direction_local;
            Vector3 endpos = Vector3.zero;
            endpos.z = Range;
            endpos = transform.localToWorldMatrix * endpos;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, endpos + transform.position);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(startpos + transform.position, transform.position + point);

            
            Gizmos.DrawSphere(transform.position + point, ModifyRadius);

            for (int i = 0; i < 8; i++)
            {
                direction_local = new Vector3(0, 0, 0);
                expansion = VoxelMath.kreisPosition(i*45) * Boundary;
                direction_local.x += expansion.x;
                direction_local.y += expansion.y;
                startpos = transform.localToWorldMatrix * direction_local;
                Gizmos.DrawLine(transform.position + startpos, endpos + startpos + transform.position);
            }

           
        }

        protected override Vector3 calculatePosition()
        {
            float angleX = Random.Range(0, 360);
            //float angleY = Random.Range(HitAngle_MIN.y, HitAngle_MAX.y);

            Vector3 direction_local = new Vector3(0, 0, 0);


            Vector2 expansion = VoxelMath.kreisPosition(angleX) * Boundary * Random.Range(0.0f, 1.0f);
            direction_local.x += expansion.x;
            direction_local.y += expansion.y;
            Vector3 startpos = transform.localToWorldMatrix * direction_local;
            Vector3 endpos = direction_local;
            endpos.z = Range;
            endpos = transform.localToWorldMatrix * endpos;

            Ray ray = new Ray(startpos + transform.position, endpos - startpos);


            RaycastHit hit = new RaycastHit();
            if (!Physics.Raycast(ray, out hit, Range, CollisionLayer))
            {
				NoTargetFound = true;
                return Vector3.zero;
            }

            Vector3 point = hit.point - transform.position;


            point += new Vector3(Random.Range(-Spread, Spread), Random.Range(-Spread, Spread), Random.Range(-Spread, Spread));

            return point;
        }
    }

}
