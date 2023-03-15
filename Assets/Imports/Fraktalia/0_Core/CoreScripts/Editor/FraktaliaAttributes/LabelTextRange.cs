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

	public class LabelTextRangeAttribute : PropertyAttribute
	{
		public string NewName;
		public float min;
		public float max;

		public LabelTextRangeAttribute(string NewName, float min, float max)
		{
			this.NewName = NewName;
			this.min = min;
			this.max = max;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(LabelTextRangeAttribute))]
	public class NameRangeAttributeDrawer : PropertyDrawer
	{
		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// First get the attribute since it contains the range for the slider
			LabelTextRangeAttribute range = attribute as LabelTextRangeAttribute;
			label.text = range.NewName;

			

			// Now draw the property as a Slider or an IntSlider based on whether it's a float or integer.
			if (property.propertyType == SerializedPropertyType.Float)
				EditorGUI.Slider(position, property, range.min, range.max, label);
			else if (property.propertyType == SerializedPropertyType.Integer)
				EditorGUI.IntSlider(position, property, Convert.ToInt32(range.min), Convert.ToInt32(range.max), label);
			else
				EditorGUI.LabelField(position, label.text, "Use Range with float or int.");
		}
	}
#endif

}
