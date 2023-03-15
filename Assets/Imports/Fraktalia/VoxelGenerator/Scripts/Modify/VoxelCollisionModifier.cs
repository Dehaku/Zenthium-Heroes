using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fraktalia.Core.FraktaliaAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.VoxelGen.Modify
{
	public class VoxelCollisionModifier : MonoBehaviour
	{
		public enum VoxelModifyPositionMode
		{
			Center,
			CollisionPoint
		}

		public float CarveRadius = 0.5f;
		public int Depth = 7;
		public int ID = 2;
		public int MaxContacts = 10;

		[EnumButtons]
		public VoxelModifyMode mode;
		public VoxelModifyPositionMode positionmode;
		public bool UseWhiteList = false;


		public List<VoxelGenerator> WhiteList;
		
		public UnityEvent onModifyVoxel;




		private bool Modified = false;

		private void OnDrawGizmosSelected()
		{
			var oldcolor = Gizmos.color;
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, CarveRadius);
			Gizmos.color = oldcolor;


		}

		private void OnCollisionEnter(Collision collision)
		{					
			if (Modified) return;
			Modified = true;

			VoxelGenerator nativevoxelEngine = collision.collider.gameObject.GetComponentInParent<VoxelGenerator>();
			if (nativevoxelEngine)
			{
				if (UseWhiteList)
				{
					if (!WhiteList.Contains(nativevoxelEngine)) return;
				}

				if (VoxelUtility.IsSafe(nativevoxelEngine, CarveRadius, Depth))
				{
					if(positionmode == VoxelModifyPositionMode.Center)
					{
						VoxelUtility.ModifyVoxelsSphere(nativevoxelEngine, transform.position, transform.rotation,
						CarveRadius, Depth, ID, mode);
					}
					else
					{
						VoxelUtility.ModifyVoxelsSphere(nativevoxelEngine, collision.contacts[0].point, transform.rotation,
						CarveRadius, Depth, ID, mode);
					}
				}
			}
		}

		private void FixedUpdate()
		{
			Modified = false;
		}



	}

#if UNITY_EDITOR
	[CustomEditor(typeof(VoxelCollisionModifier))]
	[CanEditMultipleObjects]
	public class VoxelCollisionModifierEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			VoxelCollisionModifier mytarget = target as VoxelCollisionModifier;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 14;
			bold.richText = true;




			EditorGUI.BeginChangeCheck();

			EditorGUILayout.Space();

			
			EditorGUILayout.PropertyField(serializedObject.FindProperty("positionmode"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("CarveRadius"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("Depth"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ID"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxContacts"));

			var color = GUI.backgroundColor;
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=green>Mode:</color>", bold);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));


			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("UseWhiteList"));
			if (mytarget.UseWhiteList)
			{

				EditorGUILayout.PropertyField(serializedObject.FindProperty("WhiteList"), true);
			}


			EditorGUILayout.PropertyField(serializedObject.FindProperty("onModifyVoxel"));



			if (GUI.changed)
			{
				serializedObject.ApplyModifiedProperties();
			}
		}




	}

#endif

}
