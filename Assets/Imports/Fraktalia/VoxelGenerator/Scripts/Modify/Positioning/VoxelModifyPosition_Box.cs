using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.VoxelGen.Modify.Positioning
{
    public class VoxelModifyPosition_Box : VoxelModifyPosition {

        public Vector3 BOX_Size = new Vector3(20,20,100);
        public float Spread;

        public override void Preview()
        {
            Vector3 point = calculatePosition();
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(new Vector3(0,0,BOX_Size.z/2), BOX_Size);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawSphere(transform.position + point, ModifyRadius);

            Vector3 direction_loc = new Vector3(0, 0, 0);

            direction_loc.x += BOX_Size.x * Random.Range(-0.5f, 0.5f);
            direction_loc.y += BOX_Size.y * Random.Range(-0.5f, 0.5f);
            Vector3 start = transform.localToWorldMatrix * direction_loc;
            Vector3 end = Vector3.zero;
            end.z = BOX_Size.z;
            end = transform.localToWorldMatrix * end;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, end + transform.position);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(start + transform.position, transform.position + point);


        }

		protected override Vector3 calculatePosition()
        {
            Vector3 direction_local = new Vector3(0, 0, 0);



            direction_local.x += BOX_Size.x * Random.Range(-0.5f, 0.5f);
            direction_local.y += BOX_Size.y * Random.Range(-0.5f, 0.5f);
            Vector3 startpos = transform.localToWorldMatrix * direction_local;
            Vector3 endpos = direction_local;
            endpos.z = BOX_Size.z;
            endpos = transform.localToWorldMatrix * endpos;

            Ray ray = new Ray(startpos + transform.position, endpos - startpos);


            RaycastHit hit = new RaycastHit();
            if (!Physics.Raycast(ray, out hit, BOX_Size.z, CollisionLayer))
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
