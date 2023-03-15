using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.VoxelGen.Modify
{
	public class VoxelModifier_V2_Caller : MonoBehaviour
	{
		public bool CallOnStart;
		public bool FetchChildren;
		public List<VoxelModifier_V2> Modifiers = new List<VoxelModifier_V2>();
		

		void Start()
		{
		

			if (CallOnStart)
			{
				ApplyModifiers();
			}
		}

		public void ApplyModifiers()
		{
			if (FetchChildren)
			{
				List<VoxelModifier_V2> modifiers = new List<VoxelModifier_V2>(GetComponentsInChildren<VoxelModifier_V2>());
				Modifiers.AddRange(modifiers.FindAll((a) => Modifiers.Contains(a) == false));
			}

			for (int i = 0; i < Modifiers.Count; i++)
			{
				Modifiers[i].ApplyVoxelModifier(Modifiers[i].transform.position);
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(VoxelModifier_V2_Caller))]
	[CanEditMultipleObjects]
	public class VoxelModifier_V2_CallerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;

			GUIStyle normal = new GUIStyle();
			bold.fontStyle = FontStyle.Normal;
			bold.fontSize = 12;
			bold.richText = true;

			VoxelModifier_V2_Caller myTarget = target as VoxelModifier_V2_Caller;


			DrawDefaultInspector();


			if (GUILayout.Button("Apply"))
			{
				myTarget.ApplyModifiers();
			}

		}


	}



#endif
}
