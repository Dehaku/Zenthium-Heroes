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
	public class FraktaliaContactInformation : MonoBehaviour
	{
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(FraktaliaContactInformation))]
	public class FraktaliaContactInformationEditor : Editor
	{

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
			Rect discordposition = rect;
			discordposition.x = rect.x;
			discordposition.y = rect.y;
			
			

			GUI.DrawTexture(discordposition, tex, ScaleMode.ScaleToFit);

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

			

			GUI.TextField(discordposition, "FRAKTALIA CONTACT\nINFORMATION", style);
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("FRAKTALIA HOMEPAGE", title);
			if (GUILayout.Button("Fraktalia.org"))
			{
				Application.OpenURL("http://fraktalia.org/");
			}

			
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("DISCORD SERVER", title);
			EditorGUILayout.LabelField("Get into contact with other customers on Discord. Ask questions, discuss and share your ideas. Get feedback for your project.", normalstyle);
			EditorGUILayout.DelayedTextField("https://discord.gg/UtwHQ4k", bold);
			if (GUILayout.Button("Open Discord"))
			{
				Application.OpenURL("https://discord.gg/UtwHQ4k");
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("DIRECT SUPPORT", title);
			EditorGUILayout.LabelField("Having issues with the product, bugs to report, suggestions or just general things, contact Fraktalia directly. However contact using discord is preferred.", normalstyle);
			EditorGUILayout.Space();

			EditorGUILayout.DelayedTextField("m.hartl@fraktalia.org", bold);
			if (GUILayout.Button("Open Contact Form"))
			{
				Application.OpenURL("http://fraktalia.org/press-website/");
			}


			EditorGUILayout.Space();
			EditorGUILayout.Space();


			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}
		}
	}
#endif



}
