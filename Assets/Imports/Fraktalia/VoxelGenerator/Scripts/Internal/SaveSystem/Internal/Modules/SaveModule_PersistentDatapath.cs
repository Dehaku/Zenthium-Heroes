using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.Jobs;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[System.Serializable]
	public class SaveModule_PersistentDatapath : SaveModule
	{

		public string VoxelName;
		private FileStream file;
		private BinaryReader br;

		public override string GetInformation()
		{
			return "Info: PersistentDatapath is used for real time persistence. " +
						"Loading/Saving is extremely fast but is located on a local mashine only. " +
		 "Can only be fetched by name only and is not handled by version controll systems";
		}

		public override IEnumerator Save()
		{


			RawVoxelData data = new RawVoxelData();
			data.SubdivisionPower = saveSystem.nativevoxelengine.SubdivisionPower;
			data.RootSize = saveSystem.nativevoxelengine.RootSize;
			data.VoxelData = new NativeVoxelModificationData[0];

			VoxelSaveSystem.converter.Init();
			VoxelSaveSystem.converter.data = saveSystem.nativevoxelengine.Data[0];
			VoxelSaveSystem.converter.Schedule().Complete();

			data.VoxelData = VoxelSaveSystem.converter.output.ToArray();
			data.VoxelCount = data.VoxelData.Length;

			data.IsValid = true;

			data.DimensionCount = saveSystem.nativevoxelengine.DimensionCount;
			data.AdditionalData = new AdditionaVoxelData[saveSystem.nativevoxelengine.DimensionCount - 1];
			for (int i = 0; i < data.AdditionalData.Length; i++)
			{
				VoxelSaveSystem.converter.data = saveSystem.nativevoxelengine.Data[i + 1];
				VoxelSaveSystem.converter.Schedule().Complete();

				AdditionaVoxelData adddata = new AdditionaVoxelData();
				adddata.VoxelCount = VoxelSaveSystem.converter.output.Length;
				adddata.VoxelData = VoxelSaveSystem.converter.output.ToArray();

				data.AdditionalData[i] = adddata;
			}

			if (File.Exists(Application.persistentDataPath + "/" + VoxelName + ".voxel"))
			{
				file = File.Create(Application.persistentDataPath + "/" + VoxelName + ".voxel");
			}
			else
			{
				file = File.OpenWrite(Application.persistentDataPath + "/" + VoxelName + ".voxel");
			}


			data.Serialize(file);
			file.Close();
			data.VoxelData = new NativeVoxelModificationData[0];

			yield break;
		}

		public override IEnumerator Load()
		{
			string path = Application.persistentDataPath + "/" + VoxelName + ".voxel";
			if (!File.Exists(path))
			{
				yield break;
			}
			RawVoxelData DynamicData = new RawVoxelData();

			file = File.OpenRead(path);
			br = new BinaryReader(file);
			DynamicData.IsValid = br.ReadBoolean();
			DynamicData.SubdivisionPower = br.ReadByte();
			DynamicData.RootSize = br.ReadSingle();

			saveSystem.setupGenerator(saveSystem.nativevoxelengine, DynamicData);

			yield return null;

			IEnumerator deserialize = DynamicData.DeserializeDynamic(file, br, 5000);
			while (deserialize.MoveNext())
			{
				yield return null;
			}

			br.Close();
			file.Close();

			if (!DynamicData.IsValid)
			{
				yield break;
			}

			yield return new WaitForEndOfFrame();

			NativeVoxelModificationData[] data = DynamicData.VoxelData;

			saveSystem.nativevoxelengine._SetVoxels(data, 0);

			yield return new WaitForEndOfFrame();
			if (DynamicData.AdditionalData != null)
			{
				for (int i = 0; i < DynamicData.AdditionalData.Length; i++)
				{
					saveSystem.nativevoxelengine._SetVoxels(DynamicData.AdditionalData[i].VoxelData, i + 1);

					yield return new WaitForEndOfFrame();
				}
			}

			DynamicData.IsValid = false;
			saveSystem.nativevoxelengine.SetAllRegionsDirty();

			yield break;
		}

		public override bool HasSaveData()
		{
			return File.Exists(Application.persistentDataPath + "/" + VoxelName + ".voxel");
		}

		public override void DestroySaveData()
		{
			if (!saveSystem.nativevoxelengine) return;
			if (VoxelName == "") return;

			if (File.Exists(Application.persistentDataPath + "/" + VoxelName + ".voxel"))
			{
				File.Delete(Application.persistentDataPath + "/" + VoxelName + ".voxel");
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
				if (files[i].Extension == ".voxel")
				{
					EditorGUILayout.BeginHorizontal();

					string filename = files[i].Name.Replace(".voxel", "");

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
