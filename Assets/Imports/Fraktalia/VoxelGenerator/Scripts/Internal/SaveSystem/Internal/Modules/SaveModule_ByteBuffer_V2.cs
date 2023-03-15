using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.Jobs;
using Fraktalia.VoxelGen.SaveSystem.Modules;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[System.Serializable]
	public class SaveModule_ByteBuffer_V2 : SaveModule
	{
		public string Key = "";

		MemoryStream file;
		BinaryReader br;
		public static Dictionary<string, byte[]> VoxelDictionary = new Dictionary<string, byte[]>();
		public static Action<string, byte[]> OnDataBufferSaved;
		public static Action<string, VoxelGenerator> OnDataBufferLoaded;
		private RawVoxelData_V2 data = new RawVoxelData_V2();



		public override string GetInformation()
		{
			return "Info: ByteData_V2 will store the data in a dictionary as byte buffer. The dictionary is static and the content is not persistent. " +
				"It is up to you what you do with the resulting byte[] buffer. You can send it to other clients using your networking solution. " +
				"The OnDataBufferSaved event is fired when saving is complete and ready to be sent to anywhere.";
		}

		public override IEnumerator Save()
		{
			if (Key == null) yield break;

			VoxelSaveSystem.ConvertRaw_V2(saveSystem.nativevoxelengine, ref data);

			VoxelDictionary[Key] = new byte[data.GetByteSize()];
			file = new MemoryStream(VoxelDictionary[Key]);
			data.Serialize(file);

			file.Close();

			OnDataBufferSaved?.Invoke(Key, VoxelDictionary[Key]);
			yield break;
		}

		public override IEnumerator Load()
		{
		

			if(!VoxelDictionary.ContainsKey(Key) || VoxelDictionary[Key] == null)
			{
				yield break;
			}


			file = new MemoryStream(VoxelDictionary[Key]);
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

			OnDataBufferLoaded?.Invoke(Key, saveSystem.nativevoxelengine);

			yield break;

		}

		public override bool HasSaveData()
		{
			if (!VoxelDictionary.ContainsKey(Key) || VoxelDictionary[Key] == null)
			{
				return false;
			}

			return true;
		}

		public override void DestroySaveData()
		{
			if (!saveSystem.nativevoxelengine) return;

			if (VoxelDictionary.ContainsKey(Key))
			{
				VoxelDictionary.Remove(Key);		
			}
		}

#if UNITY_EDITOR
		public override void DrawInspector(SerializedObject serializedObject)
		{
			base.DrawInspector(serializedObject);
		}
#endif
	}
}
