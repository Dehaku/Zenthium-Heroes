using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.Jobs;
using Fraktalia.Core.FraktaliaAttributes;
using System.Runtime.Serialization.Formatters.Binary;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[System.Serializable]
	public class SaveModule_Region : SaveModule
	{

		public string VoxelName;

		[Range(2, 128)]
		public int WorldRegionSize = 32;

		[Tooltip("Target Chunk of the World.")]
		public Vector3Int WorldChunk;

		[Tooltip("World data created during edit mode will be saved in a subfolder with the scene name. " +
			"The subfolder is created inside the folder of the scene file if required. " +
			"If false, you can set the path manually")]
		public bool WorldToSceneLocation = true;

		[Tooltip("Folder where World Information should be contained")]
		public string WorldFolder;

		[Tooltip("Rule how data should be loaded." +
				"\n AssetOnly: Only data from the AssetDatabase is loaded.\n" +
				"PeristentOnly: Only locally stored data is loaded (For user generated content) \n" +
				"AssetFirst: Data in the asset database is priorized else local data is loaded\n" +
				"PersistentFirst: Data created during gameplay is priorized. If no local data exists, data from asset database is loaded.")]
		[EnumButtons]
		public WorldLoadingRules WorldLoadingRule;

		public Dictionary<Vector3Int, RawVoxelRegionData> WorldInformation = new Dictionary<Vector3Int, RawVoxelRegionData>();

		private FileStream file;
		private BinaryReader br;



		public override string GetInformation()
		{
			return "Info: Region only works with infinite world generation. " +
					"Loading/Saving is region based which prevents extensive read/write operatons to the hard drive. " +
	 "Rules: If saved during Edit mode, map is saved into the AssetDatabase. If saved during gameplay, map is stored into the persistent datapath.";
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

			modifyRegion(data);

			yield break;
		}

		public override IEnumerator Load()
		{
			RawVoxelData DynamicData = fetchDataFromRegion();
			if (!DynamicData.IsValid)
			{

				yield break;
			}

			saveSystem.setupGenerator(saveSystem.nativevoxelengine, DynamicData);


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


			saveSystem.nativevoxelengine.SetAllRegionsDirty();
			yield break;
		}

		public override bool HasSaveData()
		{
			RawVoxelData data = fetchDataFromRegion();

			if (data == null) return false;
			if (data.IsValid) return true;
			return false;
		}

#if UNITY_EDITOR
		public override void DrawInspector(SerializedObject serializedObject)
		{
			base.DrawInspector(serializedObject);

			DirectoryInfo info = new DirectoryInfo(Application.persistentDataPath);
			var files = info.GetFiles();

			if (WorldToSceneLocation)
			{

				string scenepath = SceneManager.GetActiveScene().path;
				string scenename = SceneManager.GetActiveScene().name;

				scenepath = scenepath.Replace("/" + scenename + ".unity", "");
				scenepath += "/" + scenename;
				scenepath += "/" + VoxelName + "/";
				WorldFolder = scenepath;

			}
			else
			{
				if (GUILayout.Button("Select Output Path"))
				{
					string newpath = "";

					newpath = EditorUtility.OpenFolderPanel("World Folder Output", WorldFolder, WorldFolder);
					string absolutepath = Application.dataPath;

					if (newpath != null && newpath != "")
					{
						newpath = newpath.Replace(absolutepath, "Assets");
						WorldFolder = newpath;
					}
				}
			}

			EditorGUILayout.Space();
			if (WorldRegionExists(true))
			{
				if (GUILayout.Button("Destroy AssetDatabase World"))
				{
					if (EditorUtility.DisplayDialog("World Destruction!", "This will destroy world data!", "Destroy It", "Keep it alive"))
					{
						DestroyRegion_AssetDatabase();
					}
				}
			}

			if (WorldRegionExists(false))
			{
				if (GUILayout.Button("Destroy Persistent World"))
				{
					if (EditorUtility.DisplayDialog("World Destruction!", "This will destroy world data!", "Destroy It", "Keep it alive"))
					{
						DestroyRegion_Persistent();
					}
				}
				EditorGUILayout.Space();
			}
		}
#endif
		/// <summary>
		///	Saves as region for infinite world systems. May change with future updates.
		/// </summary>
		public void SaveRegions()
		{
			if (!saveSystem.nativevoxelengine) return;
			if (VoxelName == "") return;

			var world = WorldInformation;

			foreach (var item in world)
			{
				if (item.Value.IsDirty)
				{
					saveSystem.IsDirty = false;

					BinaryFormatter bf = new BinaryFormatter();
					FileStream file = File.Create(WorldRegionPath(!Application.isPlaying, true, item.Value.RegionHash));
					item.Value.Serialize(file);
					file.Close();

				}
			}

		}
		public void LoadRegions(RawVoxelRegionData region)
		{
			if (!saveSystem.nativevoxelengine) return;
			if (VoxelName == "") return;

			Vector3Int regionHash = region.RegionHash;

			string path1 = "";
			string path2 = "";
			switch (WorldLoadingRule)
			{
				case WorldLoadingRules.AssetOnly:
					path1 = WorldRegionPath(true, true, regionHash);
					break;
				case WorldLoadingRules.PersistentOnly:
					path1 = WorldRegionPath(false, true, regionHash);
					break;
				case WorldLoadingRules.AssetFirst:
					path1 = WorldRegionPath(true, true, regionHash);
					path2 = WorldRegionPath(false, true, regionHash);
					break;
				case WorldLoadingRules.PersistentFirst:
					path2 = WorldRegionPath(true, true, regionHash);
					path1 = WorldRegionPath(false, true, regionHash);
					break;
				default:
					break;
			}

			string path = "";
			if (File.Exists(path1))
			{
				path = path1;
			}
			else
			{
				path = path2;
			}

			if (File.Exists(path))
			{
				FileStream file = File.OpenRead(path);
				region.Deserialize(file);
			}



		}


		public void DestroyRegion_AssetDatabase()
		{
			string path = "";
			path = WorldFolder;
			DirectoryInfo info = new DirectoryInfo(path);
			var files = info.GetFiles();

			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].Extension == ".voxelregion")
				{
					File.Delete(files[i].FullName);
				}
			}
		}
		public void DestroyRegion_Persistent()
		{
			string path = "";
			path = Application.persistentDataPath + "/" + VoxelName;
			DirectoryInfo info = new DirectoryInfo(path);
			var files = info.GetFiles();
			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].Extension == ".voxelregion")
				{
					File.Delete(files[i].FullName);
				}
			}
			path = WorldFolder;
		}

		public string WorldRegionPath(bool FromAssetDatabase, bool CreateDirectoryIfMissing, Vector3Int RegionHash)
		{
			string path = "";
			if (!FromAssetDatabase)
			{
				if (CreateDirectoryIfMissing)
				{
					if (!File.Exists(Application.persistentDataPath + "/" + VoxelName))
					{
						Directory.CreateDirectory(Application.persistentDataPath + "/" + VoxelName);
					}
				}

				path = Application.persistentDataPath + "/" + VoxelName + "/REGION_" + RegionHash + ".voxelregion";


			}
			else
			{


				path = WorldFolder + VoxelName + "_REGION_" + RegionHash + ".voxelregion";
			}

			return path;
		}

		public bool WorldRegionExists(bool FromAssetDatabase)
		{
			return File.Exists(WorldRegionPath(FromAssetDatabase, false, Vector3Int.zero));
		}

		private void modifyRegion(RawVoxelData data)
		{
			int size = saveSystem.WorldRegionSize;
			Vector3Int hash = new Vector3Int();

			hash.x = Mathf.FloorToInt((float)saveSystem.WorldChunk.x / size);
			hash.y = Mathf.FloorToInt((float)saveSystem.WorldChunk.y / size);
			hash.z = Mathf.FloorToInt((float)saveSystem.WorldChunk.z / size);

			Vector3Int subhash = new Vector3Int();

			subhash.x = Mathf.FloorToInt(saveSystem.WorldChunk.x % size);
			subhash.y = Mathf.FloorToInt(saveSystem.WorldChunk.y % size);
			subhash.z = Mathf.FloorToInt(saveSystem.WorldChunk.z % size);

			if (subhash.x < 0) subhash.x += size;
			if (subhash.y < 0) subhash.y += size;
			if (subhash.z < 0) subhash.z += size;


			if (WorldInformation == null) WorldInformation = new Dictionary<Vector3Int, RawVoxelRegionData>();
			if (!WorldInformation.ContainsKey(hash))
			{

				RawVoxelRegionData newregion = new RawVoxelRegionData(hash, size);

				saveSystem.ModuleRegion.LoadRegions(newregion);
				WorldInformation[hash] = newregion;



			}

			RawVoxelRegionData region = WorldInformation[hash];
			region.RegionData[subhash.x + subhash.y * size + subhash.z * size * size] = data;
			region.IsDirty = true;

		}

		private RawVoxelData fetchDataFromRegion()
		{
			int size = WorldRegionSize;
			Vector3Int hash = new Vector3Int();

			hash.x = Mathf.FloorToInt((float)WorldChunk.x / size);
			hash.y = Mathf.FloorToInt((float)WorldChunk.y / size);
			hash.z = Mathf.FloorToInt((float)WorldChunk.z / size);

			Vector3Int subhash = new Vector3Int();

			subhash.x = Mathf.FloorToInt(WorldChunk.x % size);
			subhash.y = Mathf.FloorToInt(WorldChunk.y % size);
			subhash.z = Mathf.FloorToInt(WorldChunk.z % size);

			if (subhash.x < 0) subhash.x += size;
			if (subhash.y < 0) subhash.y += size;
			if (subhash.z < 0) subhash.z += size;


			if (WorldInformation == null) WorldInformation = new Dictionary<Vector3Int, RawVoxelRegionData>();
			if (!WorldInformation.ContainsKey(hash))
			{
				RawVoxelRegionData region = new RawVoxelRegionData(hash, size);
				LoadRegions(region);
				WorldInformation[hash] = region;
				WorldRegionSize = region.RegionSize;

			}

			return WorldInformation[hash].RegionData[subhash.x + subhash.y * size + subhash.z * size * size];


		}

	}
}
