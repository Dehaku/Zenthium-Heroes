using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Core.FraktaliaAttributes
{

	public class EnumButtonsAttribute : PropertyAttribute
	{
		

		public EnumButtonsAttribute()
		{
			
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(EnumButtonsAttribute))]
	public class EnumButtonsDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.Enum)
			{

				if (!EditorGUIUtility.wideMode)
				{

					return base.GetPropertyHeight(property, label) + 18 + 40;
				}
			}

			return base.GetPropertyHeight(property, label) + 40;

		}
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if (property.propertyType == SerializedPropertyType.Enum)
			{

				string[] names = property.enumNames;

				EditorGUI.LabelField(position, property.displayName);
				

				var color = GUI.backgroundColor;

				position = EditorGUI.IndentedRect(position);

				float width = position.width / 4;

				float height = (position.height-18)/2;
				float heightoffset = 0;

				

				for (int i = 0; i < (int)names.Length; i++)
				{
					var amountRect = new Rect(position.x + width * (i % 4), position.y + 18 + height * heightoffset, width, height);


					if (i == (int)property.enumValueIndex)
					{
						GUI.backgroundColor = Color.yellow;
					}
					if (GUI.Button(amountRect,  names[i]))
					{
						property.enumValueIndex = i;
					}
					GUI.backgroundColor = color;

					if (i % 4 == 3)
					{
						heightoffset++;
					}
				}
			
				
			}
			EditorGUI.EndProperty();
		}
	}
#endif

}
