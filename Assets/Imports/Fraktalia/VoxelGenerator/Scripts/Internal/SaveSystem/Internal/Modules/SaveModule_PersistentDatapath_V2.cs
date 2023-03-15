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
	public class SaveModule_PersistentDatapath_V2 : SaveModule
	{
		private RawVoxelData_V2 data = new RawVoxelData_V2();

		public string VoxelName;
		private FileStream file;
		private BinaryReader br;

		public override string GetInformation()
		{
			return "Info: PersistentDatapath V2 is similar to PersistentDatapath and is designed for real time saving and loading. " +
						"Loading/Saving is extremely fast but V2 is even faster than V2. Data is located on a local mashine only." +
		 "Can only be fetched by name only and is not handled by version controll systems as the data is inside the persistent data path.\n\n" +
		 "Note: File ending is .voxel_v2 as the data layout is different from the normal PesistentDatapath mode in order to improve efficiency. " +
		 "Therefore .voxel is not compatible with .voxel_v2. You can bridge between versions by simply loading the data and re-save it using the v2 saving mode.";
		}

		public override IEnumerator Save()
		{

			VoxelSaveSystem.ConvertRaw_V2(saveSystem.nativevoxelengine, ref data);

			if (File.Exists(Application.persistentDataPath + "/" + VoxelName + ".voxel_v2"))
			{
				file = File.Create(Application.persistentDataPath + "/" + VoxelName + ".voxel_v2");
			}
			else
			{
				file = File.OpenWrite(Application.persistentDataPath + "/" + VoxelName + ".voxel_v2");
			}

			data.Serialize(file);
			file.Close();

			yield break;
		}

		public override IEnumerator Load()
		{
			string path = Application.persistentDataPath + "/" + VoxelName + ".voxel_v2";

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
			return File.Exists(Application.persistentDataPath + "/" + VoxelName + ".voxel_v2");
		}

		public override void DestroySaveData()
		{
			if (!saveSystem.nativevoxelengine) return;
			if (VoxelName == "") return;

			if (File.Exists(Application.persistentDataPath + "/" + VoxelName + ".voxel_v2"))
			{
				File.Delete(Application.persistentDataPath + "/" + VoxelName + ".voxel_v2");
			}
		}

#if UNITY_EDITOR
		public override void DrawInspector(SerializedObject serializedObject)
		{
			base.DrawInspector(serializedObject);

			DirectoryInfo info = new DirectoryInfo(Application.persistentDataPath);
			var files = info.GetFiles();

			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].Extension == ".voxel_v2")
				{
					EditorGUILayout.BeginHorizontal();

					string filename = files[i].Name.Replace(".voxel_v2", "");

					if (GUILayout.Button(filename))
					{
						VoxelName = filename;
						serializedObject.ApplyModifiedProperties();
						saveSystem.Load();
					}


					if (GUILayout.Button("Remove"))
					{
						VoxelName = filename;
						serializedObject.ApplyModifiedProperties();
						DestroySaveData();
					}
					EditorGUILayout.EndHorizontal();

				}

			}
		}
#endif
	}
}
