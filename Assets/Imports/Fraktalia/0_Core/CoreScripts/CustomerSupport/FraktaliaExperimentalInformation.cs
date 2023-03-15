using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.Core
{
	public class FraktaliaExperimentalInformation : MonoBehaviour
	{
		[System.Serializable]
		public class InformationEntry
		{
			public string Title;
			[Multiline]
			public string Text;
		}

		public InformationEntry[] Entries = new InformationEntry[0];
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(FraktaliaExperimentalInformation))]
	public class FraktaliaExperimentalInformationEditor : Editor
	{
		bool modify;
		Texture tex;
		public override void OnInspectorGUI()
		{
			

			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 16;
			title.richText = true;
			title.alignment = TextAnchor.MiddleCenter;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;
			bold.alignment = TextAnchor.MiddleCenter;

			EditorStyles.textField.wordWrap = true;
			EditorGUILayout.Space();
			
			if (tex == null)
			{
				tex = Resources.Load<Texture>("banner");
			}

			Rect rect = EditorGUILayout.GetControlRect(false, 125);
			Rect logoposition = rect;
			logoposition.x = rect.x;
			logoposition.y = rect.y;

			FraktaliaExperimentalInformation info = target as FraktaliaExperimentalInformation;
			Rect smallrect = logoposition;
			smallrect.width = 20;
			smallrect.height = 20;
			smallrect.x = logoposition.center.x;
			smallrect.y = logoposition.center.y;

			if (GUI.Button(smallrect, new GUIContent()))
			{
				modify = true;
			}
			GUI.DrawTexture(logoposition, tex, ScaleMode.ScaleToFit);
			


			GUIStyle style = new GUIStyle();
			style.alignment = TextAnchor.MiddleCenter;
			style.fontSize = 20;
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.white;
			style.wordWrap = true;

			GUIStyle normalstyle = new GUIStyle();
			normalstyle.alignment = TextAnchor.MiddleCenter;
			normalstyle.fontSize = 12;
			normalstyle.fontStyle = FontStyle.Normal;			
			normalstyle.wordWrap = true;

			

			GUI.TextField(logoposition, "EXPERIMENTAL CONTENT", style);
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			
			for (int i = 0; i < info.Entries.Length; i++)
			{
				EditorGUILayout.LabelField(info.Entries[i].Title, title);
				EditorGUILayout.LabelField(info.Entries[i].Text, normalstyle);
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if(modify)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("Entries"), true);
			}

			if (GUI.changed)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}



		}
	}
#endif



}
