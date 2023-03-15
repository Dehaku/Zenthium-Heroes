using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Core.FraktaliaAttributes
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public class PropertyKeyAttribute : PropertyAttribute
	{
		public static Dictionary<string, bool> PropertyKeyStates = new Dictionary<string, bool>();

		public string Key;

		private bool state;
		public bool State
        {
            get
            {
				if (!PropertyKeyStates.ContainsKey(Key))
				{
#if UNITY_EDITOR
					if (EditorPrefs.HasKey(Key))
					{
						return EditorPrefs.GetBool(Key);
					}
#endif
					return false;
				}

				return PropertyKeyStates[Key];
            }
            set
            {
				if (value != state)
				{
					PropertyKeyStates[Key] = value;
#if UNITY_EDITOR
					EditorPrefs.SetBool(Key, value);
#endif
				}
				state = value;
			}
        }

		public PropertyKeyAttribute(string key, bool defaultstate = false)
		{
			Key = key;
			

			if(!PropertyKeyStates.ContainsKey(key))
            {
				bool initialstate = defaultstate;
#if UNITY_EDITOR
				if (EditorPrefs.HasKey(key)){
					initialstate = EditorPrefs.GetBool(key);
                }
#endif
				State = initialstate;				
			}
            else
            {
				State = PropertyKeyStates[key];
			}	
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public class PropertyModuleAttribute : PropertyAttribute
	{
		public string type;

		public PropertyModuleAttribute(string type = ""){
			this.type = type;
		}
	}

}
