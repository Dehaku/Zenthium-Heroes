using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Core.FraktaliaAttributes
{

	public class ReadonlyTextAttribute : PropertyAttribute
	{	
		public ReadonlyTextAttribute()
		{
			
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(ReadonlyTextAttribute))]
	public class ReadonlyTextDrawer : PropertyDrawer
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

		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
		
			ReadonlyTextAttribute labeltext = attribute as ReadonlyTextAttribute;
					
			if (property.propertyType == SerializedPropertyType.Float)
				EditorGUI.LabelField(position, property.name + " = " + property.floatValue);
			else if (property.propertyType == SerializedPropertyType.Integer)
				EditorGUI.LabelField(position, property.name + " = " + property.intValue);
			else if( property.propertyType == SerializedPropertyType.String)
				EditorGUI.LabelField(position, property.name + " = " + property.stringValue);

		}
	}
#endif

}
