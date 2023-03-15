using UnityEngine;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.VoxelGen.Modify.Positioning
{
    public class VoxelModifyPosition_Spherical : VoxelModifyPosition
    {
        public bool ApplyLocalRotation;
        public Vector2 HitAngle_MIN = new Vector2(0, 0);
        public Vector2 HitAngle_MAX = new Vector2(360, 360);

        public float Boundary = 50;
        public float Spread = 0;

        public SphericalType PositionType = SphericalType.Outward;
        
        public override void Preview()
        {
            Gizmos.DrawWireSphere(transform.position, Boundary);
            Vector3 point = calculatePosition();
            Gizmos.DrawSphere(transform.position + point, ModifyRadius);

            Gizmos.color = new Color32(128, 200, 0, 128);
            for (int i = 0; i < 60; i++)
            {
                float angleX = i * 6;
                for (int y = 0; y < 60; y++)
                {

                    if (angleX >= HitAngle_MIN.x && angleX <= HitAngle_MAX.x)
                    {
                        float angleY = y * 6;
                        if (angleY >= HitAngle_MIN.y && angleY <= HitAngle_MAX.y)
                        {
                            Vector3 direction = VoxelMath.spherePosition(angleX, angleY) * Boundary;
                            Gizmos.DrawLine(transform.position - direction / 20, transform.position);
                        }
                    }

                }
            }
            Gizmos.color = Color.white;

            if (PositionType == SphericalType.Invard)
            {


                Gizmos.DrawLine(transform.position + GetDirection(), transform.position + calculatePosition());
            }

            if (PositionType == SphericalType.Outward)
            {
                Gizmos.DrawLine(transform.position, transform.position + point);
            }
        }

        public Vector3 GetDirection()
        {
            float angleX = Random.Range(HitAngle_MIN.x, HitAngle_MAX.x);
            float angleY = Random.Range(HitAngle_MIN.y, HitAngle_MAX.y);



            Vector3 direction = VoxelMath.spherePosition(angleX, angleY);
            if (ApplyLocalRotation)
            {
                return transform.TransformDirection(direction) * Boundary;
            }
            else
            {
                return direction * Boundary;
            }
        }

		protected override Vector3 calculatePosition()
		{
			Vector3 direction = GetDirection();

			Ray ray = new Ray();
			if (PositionType == SphericalType.Invard)
			{
				ray = new Ray(transform.position + direction, -direction * 10);
			}

			if (PositionType == SphericalType.Outward)
			{
				ray = new Ray(transform.position, -direction * 10);
			}

			Vector3 point = new Vector3();

			RaycastHit hit = new RaycastHit();


			if (!Physics.Raycast(ray, out hit, Boundary, CollisionLayer))
			{
				NoTargetFound = true;
				return Vector3.zero;
			}
			point = hit.point - transform.position;


			point += new Vector3(Random.Range(-Spread, Spread), Random.Range(-Spread, Spread), Random.Range(-Spread, Spread));

			return point;
		}
    }
    
    public enum SphericalType
    {
        Invard,
        Outward
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VoxelModifyPosition_Spherical))][CanEditMultipleObjects]
    public class VoxelModifyPosition_SphericalEditor : Editor
    {


        public override void OnInspectorGUI()
        {



            GUIStyle bold = new GUIStyle();
            bold.fontStyle = FontStyle.Bold;
            bold.fontSize = 14;
            bold.richText = true;




            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<color=green>Spherical Positioning:</color>", bold);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ApplyLocalRotation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HitAngle_MIN"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HitAngle_MAX"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Boundary"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Spread"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PositionType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CollisionLayer"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ModifyRadius_Min"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ModifyRadius_Max"));


			EditorGUILayout.Space();
            EditorGUILayout.LabelField("<color=green>Angle Presets:</color>", bold);
            EditorGUILayout.Space();

            if (GUILayout.Button("Disc YZ"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
					VoxelModifyPosition_Spherical curtarget = targets[i] as VoxelModifyPosition_Spherical;
                    curtarget.HitAngle_MIN.x = 0;
                    curtarget.HitAngle_MIN.y = 0;
                    curtarget.HitAngle_MAX.x = 0;
                    curtarget.HitAngle_MAX.y = 360;
                }
            }

            if (GUILayout.Button("Disc XY(Normal 2D)"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
					VoxelModifyPosition_Spherical curtarget = targets[i] as VoxelModifyPosition_Spherical;
                    curtarget.HitAngle_MIN.x = 90;
                    curtarget.HitAngle_MIN.y = 0;
                    curtarget.HitAngle_MAX.x = 90;
                    curtarget.HitAngle_MAX.y = 360;
                }
            }

            if (GUILayout.Button("Disc XZ"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
					VoxelModifyPosition_Spherical curtarget = targets[i] as VoxelModifyPosition_Spherical;
                    curtarget.HitAngle_MIN.x = 0;
                    curtarget.HitAngle_MIN.y = 90;
                    curtarget.HitAngle_MAX.x = 360;
                    curtarget.HitAngle_MAX.y = 90;
                }
            }

            if (GUILayout.Button("Spherical"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
					VoxelModifyPosition_Spherical curtarget = targets[i] as VoxelModifyPosition_Spherical;
                    curtarget.HitAngle_MIN.x = 0;
                    curtarget.HitAngle_MIN.y = 20;
                    curtarget.HitAngle_MAX.x = 360;
                    curtarget.HitAngle_MAX.y = 160;
                }
            }

            if (GUILayout.Button("Rhombic"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
					VoxelModifyPosition_Spherical curtarget = targets[i] as VoxelModifyPosition_Spherical;
                    curtarget.HitAngle_MIN.x = 0;
                    curtarget.HitAngle_MIN.y = 0;
                    curtarget.HitAngle_MAX.x = 360;
                    curtarget.HitAngle_MAX.y = 360;
                }
            }


            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
            }
        }




    }
#endif
}
