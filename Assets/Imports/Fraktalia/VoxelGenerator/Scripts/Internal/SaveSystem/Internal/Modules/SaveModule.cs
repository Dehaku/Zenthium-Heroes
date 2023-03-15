using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[Serializable]
	public class SaveModule
	{

		public virtual string GetInformation()
		{
			return "Selected module is not implemented yet!";
		}

		[HideInInspector]
		public VoxelSaveSystem saveSystem;



		public virtual void Init(VoxelSaveSystem saveSystem)
		{
			this.saveSystem = saveSystem;
		}

		public virtual void CleanUp()
		{

		}

		public virtual void CleanStatics()
		{

		}


		public virtual IEnumerator Save()
		{
			return null;
		}


		public virtual IEnumerator Load()
		{
			return null;
		}

		public virtual void DestroySaveData()
		{

		}

		public virtual bool HasSaveData()
		{
			return false;
		}

#if UNITY_EDITOR

		public virtual void DrawInspector(SerializedObject serializedObject)
		{
			EditorGUILayout.TextArea(GetInformation());



		}
#endif

	}
}
