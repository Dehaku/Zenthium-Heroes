using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.Jobs;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.VoxelGen.SaveSystem.Modules;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[System.Serializable]
	public class SaveModule_World : SaveModule
	{

		public string VoxelName;

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


		private FileStream file;
		private BinaryReader br;

		public override string GetInformation()
		{
			return "Info: World is used for infinite world generation but Region mode is much faster!. " +
							"Loading/Saving is extremely fast since chunks should be loaded as quickly as possible." +
			 "Rules: If saved during Edit mode, map is saved into the AssetDatabase. If saved during gameplay, map is stored into the persistent datapath.";
		}

		public void SetWorldFolderToScene()
		{
#if UNITY_EDITOR
			if (WorldToSceneLocation)
			{
				string scenepath = SceneManager.GetActiveScene().path;
				string scenename = SceneManager.GetActiveScene().name;

				scenepath = scenepath.Replace("/" + scenename + ".unity", "");


				if (!AssetDatabase.IsValidFolder(scenepath + "/" + scenename))
				{
					AssetDatabase.CreateFolder(scenepath, scenename);
				}

				scenepath += "/" + scenename;

				if (!AssetDatabase.IsValidFolder(scenepath + "/" + VoxelName))
				{
					AssetDatabase.CreateFolder(scenepath, VoxelName);
				}


				scenepath += "/" + VoxelName + "/";
				WorldFolder = scenepath;
			}
#endif
		}

		public override IEnumerator Save()
		{
			SetWorldFolderToScene();

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

			string path = WorldPath(!Application.isPlaying, true);

			if (File.Exists(path))
			{
				file = File.Create(path);
			}
			else
			{
				file = File.OpenWrite(path);
			}

			data.Serialize(file);
			file.Close();
			data.VoxelData = new NativeVoxelModificationData[0];

			yield break;
		}

		public override IEnumerator Load()
		{
			string path;
			string path1 = "";
			string path2 = "";
			switch (WorldLoadingRule)
			{
				case WorldLoadingRules.AssetOnly:
					path1 = WorldPath(true, true);
					break;
				case WorldLoadingRules.PersistentOnly:
					path1 = WorldPath(false, true);
					break;
				case WorldLoadingRules.AssetFirst:
					path1 = WorldPath(true, true);
					path2 = WorldPath(false, true);
					break;
				case WorldLoadingRules.PersistentFirst:
					path2 = WorldPath(true, true);
					path1 = WorldPath(false, true);
					break;
				default:
					break;
			}

			if (File.Exists(path1))
			{
				path = path1;
			}
			else
			{
				path = path2;
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
			switch (WorldLoadingRule)
			{



				case WorldLoadingRules.AssetOnly:
					return File.Exists(WorldPath(true, false));

				case WorldLoadingRules.PersistentOnly:
					return File.Exists(WorldPath(false, false));

				case WorldLoadingRules.AssetFirst:
					return File.Exists(WorldPath(true, false)) || File.Exists(WorldPath(false, false));

				case WorldLoadingRules.PersistentFirst:
					return File.Exists(WorldPath(true, false)) || File.Exists(WorldPath(false, false));


			}
			return false;
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

		public void DestroyWorld_AssetDatabase()
		{
			string path = "";
			path = WorldFolder;
			DirectoryInfo info = new DirectoryInfo(path);
			var files = info.GetFiles();

			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].Extension == ".voxelworld")
				{
					File.Delete(files[i].FullName);
				}
			}
		}
		public void DestroyWorld_Persistent()
		{
			string path = "";
			path = Application.persistentDataPath + "/" + VoxelName;
			DirectoryInfo info = new DirectoryInfo(path);
			var files = info.GetFiles();
			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].Extension == ".voxelworld")
				{
					File.Delete(files[i].FullName);
				}
			}
			path = WorldFolder;
		}

		public void RemoveWorldChunk(bool fromAssetDatabase)
		{
			if (!saveSystem.nativevoxelengine) return;
			if (VoxelName == "") return;

			if (File.Exists(WorldPath(fromAssetDatabase, true)))
			{
				File.Delete(WorldPath(fromAssetDatabase, true));
			}
		}


		public string WorldPath(bool FromAssetDatabase, bool CreateDirectoryIfMissing)
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
				path = Application.persistentDataPath + "/" + VoxelName + "/CHUNK_" + WorldChunk + ".voxelworld";
			}
			else
			{
				path = WorldFolder + VoxelName + "_CHUNK_" + WorldChunk + ".voxelworld";
			}
			return path;
		}

		public bool WorldChunkExists(bool FromAssetDatabase)
		{
			return File.Exists(WorldPath(FromAssetDatabase, false));
		}

#if UNITY_EDITOR
		public override void DrawInspector(SerializedObject serializedObject)
		{

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

			if (HasSaveData())
			{


				EditorGUILayout.Space();

				if (GUILayout.Button("Destroy World"))
				{
					if (EditorUtility.DisplayDialog("World Destruction!", "This will destroy the world data!", "Destroy It", "Keep it alive"))
					{
						DestroyWorld_AssetDatabase();
						DestroyWorld_Persistent();
					}
				}
				EditorGUILayout.Space();

				if (WorldChunkExists(false))
				{
					if (GUILayout.Button("Remove chunk from persistent Folder"))
					{
						RemoveWorldChunk(false);
					}

					if (GUILayout.Button("Destroy Persistent World Data"))
					{
						if (EditorUtility.DisplayDialog("World Destruction!", "This will destroy the world created during play mode!", "Destroy It", "Keep it alive"))
						{

							DestroyWorld_Persistent();
						}
					}
					EditorGUILayout.Space();
				}

				if (WorldChunkExists(true))
				{
					if (GUILayout.Button("Remove chunk from Asset Folder"))
					{
						RemoveWorldChunk(true);
					}

					if (GUILayout.Button("Destroy Asset World Data"))
					{
						if (EditorUtility.DisplayDialog("World Destruction!", "This will destroy the world data created during edit mode!", "Destroy It", "Keep it alive"))
						{
							DestroyWorld_AssetDatabase();

						}
					}
				}
			}
		}
#endif
	}
}
