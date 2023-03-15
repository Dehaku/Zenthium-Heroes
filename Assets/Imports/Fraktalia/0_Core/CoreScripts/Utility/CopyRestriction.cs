using UnityEngine;
using Fraktalia.Core.FraktaliaAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.Utility
{
	[ExecuteInEditMode]
	public class CopyRestriction : MonoBehaviour
	{
		public string Reason;

		[HideInInspector]
		public int instanceID;

#if (UNITY_EDITOR)
		private void OnDrawGizmosSelected()
		{
			
			
		}

		[ExecuteInEditMode]
		private void Update()
		{
			OnDuplicate();
		}

		public void OnDuplicate()
		{

			if (!Application.isPlaying)//if in the editor
			{

				//if the instance ID doesnt match then this was copied!
				if (instanceID != gameObject.GetInstanceID())
				{
					UnityEngine.Object orig = EditorUtility.InstanceIDToObject(instanceID);

					Selection.activeObject = null;

					DestroyImmediate(gameObject);
				}
			}

		}
#endif

		[ExecuteInEditMode]
		public void Initialize(string reason)
		{
			instanceID = gameObject.GetInstanceID();
			Reason = reason;
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(CopyRestriction))]
	public class CopyRestrictionEditor : Editor
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

			CopyRestriction myTarget = target as CopyRestriction;

			if (tex == null)
			{
				tex = Resources.Load<Texture>("banner");
			}

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

			EditorGUILayout.LabelField("CLONING FORBIDDEN!", title);
			

			EditorGUILayout.Space();
			EditorGUILayout.LabelField(myTarget.Reason, normalstyle);
			

			
		}
	}
#endif
}
