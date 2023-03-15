using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Fraktalia.Core.FraktaliaAttributes
{
    public static class FraktaliaEditorUtility
    {		
		private const BindingFlags AllBindingFlags = (BindingFlags)(-1);

        /// <summary>
        /// Returns attributes of type <typeparamref name="TAttribute"/> on <paramref name="serializedProperty"/>.
        /// </summary>
        public static TAttribute[] GetAttributes<TAttribute>(this SerializedProperty serializedProperty, bool inherit)
            where TAttribute : Attribute
        {
            if (serializedProperty == null)
            {
                throw new ArgumentNullException(nameof(serializedProperty));
            }

            var targetObjectType = serializedProperty.serializedObject.targetObject.GetType();

            if (targetObjectType == null)
            {
                throw new ArgumentException($"Could not find the {nameof(targetObjectType)} of {nameof(serializedProperty)}");
            }

            foreach (var pathSegment in serializedProperty.propertyPath.Split('.'))
            {
                var fieldInfo = targetObjectType.GetField(pathSegment, AllBindingFlags);
                if (fieldInfo != null)
                {
                    return (TAttribute[])fieldInfo.GetCustomAttributes<TAttribute>(inherit);
                }

                var propertyInfo = targetObjectType.GetProperty(pathSegment, AllBindingFlags);
                if (propertyInfo != null)
                {
                    return (TAttribute[])propertyInfo.GetCustomAttributes<TAttribute>(inherit);
                }
            }

            return new TAttribute[0];
        }

        public static TAttribute GetAttribute<TAttribute>(this SerializedProperty serializedProperty, bool inherit)
            where TAttribute : Attribute
        {
            if (serializedProperty == null)
            {
                throw new ArgumentNullException(nameof(serializedProperty));
            }

            var targetObjectType = serializedProperty.serializedObject.targetObject.GetType();        
         
            while (targetObjectType != null)
            {
                foreach (var pathSegment in serializedProperty.propertyPath.Split('.'))
                {

                    var fieldInfo = targetObjectType.GetField(pathSegment, AllBindingFlags);
                    if (fieldInfo != null)
                    {
                        return (TAttribute)fieldInfo.GetCustomAttribute<TAttribute>(inherit);
                    }

                    var propertyInfo = targetObjectType.GetProperty(pathSegment, AllBindingFlags);
                    if (propertyInfo != null)
                    {
                        return (TAttribute)propertyInfo.GetCustomAttribute<TAttribute>(inherit);
                    }
                }

                targetObjectType = targetObjectType.BaseType;
            }
           
            return null;
        }
    
        public static void CreateEditor(SerializedObject serializedObject)
        {
            SerializedProperty prop = serializedObject.GetIterator();

            bool isfoldout = true;
            int verticalgroups = 0;
            if (prop.NextVisible(true))
            {
                do
                {
                    PropertyKeyAttribute fold = FraktaliaEditorUtility.GetAttribute<PropertyKeyAttribute>(prop, true);
                    if (fold != null)
                    {
                        if (isfoldout)
                        {
                            if (verticalgroups > 0)
                            {
                                verticalgroups--;
                                EditorGUILayout.EndVertical();
                            }
                        }

                        verticalgroups++;
                        EditorGUILayout.BeginVertical(FraktaliaEditorStyles.BoxContent);
                        EditorGUILayout.BeginHorizontal(FraktaliaEditorStyles.GetFoldoutBox(fold.State));
                        isfoldout = EditorGUILayout.Foldout(fold.State, fold.Key, FraktaliaEditorStyles.FoldOut);
                        EditorGUILayout.EndHorizontal();
                        fold.State = isfoldout;

                        if (!isfoldout)
                        {
                            verticalgroups--;
                            EditorGUILayout.EndVertical();
                        }
                    }
                }
                while (prop.NextVisible(false));
            }

            if (verticalgroups > 0)
            {
                verticalgroups--;
                EditorGUILayout.EndVertical();
            }
        }

		private static Dictionary<string, Tuple<List<Type>, string[]>> cachedDerivedTypes = new Dictionary<string, Tuple<List<Type>, string[]>>();
		public static Tuple<List<Type>, string[]> GetDerivedTypesForScriptSelection(Type baseType, string DefaultName)
		{
			if (cachedDerivedTypes.ContainsKey(baseType.Name + DefaultName)) return cachedDerivedTypes[baseType.Name + DefaultName];

			List<Type> derived_types = new List<Type>();
			derived_types.Add(baseType);
			foreach (var domain_assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var assembly_types = domain_assembly.GetTypes()
				  .Where(type => type.IsSubclassOf(baseType) && !type.IsAbstract);

				derived_types.AddRange(assembly_types);
			}

			List<string> types = new List<string>();
			for (int i = 0; i < derived_types.Count; i++)
			{
				types.Add(derived_types[i].Name);
			}
			types[0] = DefaultName;

			cachedDerivedTypes[baseType.Name + DefaultName] = new Tuple<List<Type>, string[]>(derived_types, types.ToArray());
			return cachedDerivedTypes[baseType.Name + DefaultName];
		}

	}
}

#endif
