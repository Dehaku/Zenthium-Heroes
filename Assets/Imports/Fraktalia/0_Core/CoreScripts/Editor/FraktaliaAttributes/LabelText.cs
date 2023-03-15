using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Core.FraktaliaAttributes
{	
	public class LabelTextAttribute : PropertyAttribute
	{
		public string NewName;
		public LabelTextAttribute(string NewName)
		{
			this.NewName = NewName;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(LabelTextAttribute))]
	public class NameOverrideDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.Vector3)
			{
				
				if (!EditorGUIUtility.wideMode)
				{

					return base.GetPropertyHeight(property, label) + 18;
				}
			}
			
				return base.GetPropertyHeight(property, label);
			
		}

		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// First get the attribute since it contains the range for the slider
			LabelTextAttribute labeltext = attribute as LabelTextAttribute;
			label.text = labeltext.NewName;


			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PropertyField(position, property, label);
			EditorGUI.EndProperty();

		}
	}
#endif

}
