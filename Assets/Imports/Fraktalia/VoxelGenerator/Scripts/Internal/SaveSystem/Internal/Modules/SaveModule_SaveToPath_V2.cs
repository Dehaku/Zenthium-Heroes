using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.Jobs;
using Fraktalia.VoxelGen.SaveSystem.Modules;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[System.Serializable]
	public class SaveModule_SaveToPath_V2 : SaveModule
	{
		public string CurrentPath = "";

		private RawVoxelData_V2 data = new RawVoxelData_V2();

		
		private FileStream file;
		private BinaryReader br;

		public override string GetInformation()
		{
			return "Info: SaveToPath_V2 is similar to PersistentDatapath_V2 and is designed for real time saving and loading. " +
				"The difference is that you can set the path yourself. Therefore the main usage are for desktop apps which allow you to save/load a voxel file using the file browser. " +
				"There are many file dialog system out there but I am not allowed to include them due to legal issues.";
		}

		public override IEnumerator Save()
		{
			if (CurrentPath == "") yield break;

			VoxelSaveSystem.ConvertRaw_V2(saveSystem.nativevoxelengine, ref data);

			if (File.Exists(CurrentPath))
			{
				file = File.Create(CurrentPath);
			}
			else
			{
				file = File.OpenWrite(CurrentPath);
			}

			data.Serialize(file);
			file.Close();

			yield break;
		}

		public override IEnumerator Load()
		{
			string path = CurrentPath;

			if (!File.Exists(path))
			{
				yield break;
			}


			file = File.OpenRead(path);
			br = new BinaryReader(file);

			data.Version = br.ReadInt32();
			data.IsValid = br.ReadBoolean();
			data.SubdivisionPower = br.ReadInt32();
			data.RootSize = br.ReadSingle();
			data.DimensionCount = br.ReadInt32();

			saveSystem.setupGenerator(saveSystem.nativevoxelengine, data);


			IEnumerator deserialize = data.DeserializeDynamic(file, br);
			while (deserialize.MoveNext())
			{
				yield return null;
			}

			br.Close();
			file.Close();

			if (!data.IsValid)
			{
				yield break;
			}

			for (int i = 0; i < data.DimensionCount; i++)
			{
				saveSystem.nativevoxelengine._SetVoxels(data.BytedVoxelData[i], data.VoxelCount[i], i);
				yield return new WaitForEndOfFrame();
			}

			data.IsValid = false;

			saveSystem.nativevoxelengine.Locked = false;
			saveSystem.nativevoxelengine.SetAllRegionsDirty();


			yield break;

		}

		public override bool HasSaveData()
		{
			return File.Exists(CurrentPath);
		}

		public override void DestroySaveData()
		{
			if (!saveSystem.nativevoxelengine) return;
			
			if (File.Exists(CurrentPath + ".voxel_v2"))
			{
				File.Delete(CurrentPath + ".voxel_v2");
			}
		}

#if UNITY_EDITOR
		public override void DrawInspector(SerializedObject serializedObject)
		{
			base.DrawInspector(serializedObject);

			if (GUILayout.Button("Save File"))
			{
				
				CurrentPath = EditorUtility.SaveFilePanel("Save Voxel File", "", "VoxelFile", "voxel_v2");
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(serializedObject.targetObject);
				saveSystem.IsDirty = true;
				saveSystem.Save();
			}

			if (GUILayout.Button("Open File"))
			{
				CurrentPath = EditorUtility.OpenFilePanel("Open Voxel File", "", "voxel_v2");
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(serializedObject.targetObject);
				saveSystem.Load();
			}


		}
#endif
	}
}
