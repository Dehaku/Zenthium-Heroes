using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.Jobs;
using Fraktalia.VoxelGen.SaveSystem.Modules;
using Fraktalia.Core.FraktaliaAttributes;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[System.Serializable]
	public class SaveModule_World_V2 : SaveModule
	{
		private RawVoxelData_V2 data = new RawVoxelData_V2();


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
			 "Rules: If saved during Edit mode, map is saved into the AssetDatabase. If saved during gameplay, map is stored into the persistent datapath. " +
			 "\n " +
			 "V2: Faster, Better, Stronger.";
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

			VoxelSaveSystem.ConvertRaw_V2(saveSystem.nativevoxelengine, ref data);
			string path = WorldPath(!Application.isPlaying, true);
			if (!File.Exists(path))
			{
				file = File.Create(path);
			}
			else
			{
				file = File.OpenWrite(path);
			}

			data.Serialize(file);
			file.Close();

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

		public void DestroyWorld_AssetDatabase()
		{
			string path = "";
			path = WorldFolder;
			DirectoryInfo info = new DirectoryInfo(path);
			var files = info.GetFiles();

			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].Extension == ".voxelworld_v2")
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
				if (files[i].Extension == ".voxelworld_v2")
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
				path = Application.persistentDataPath + "/" + VoxelName + "/CHUNK_" + WorldChunk + ".voxelworld_v2";
			}
			else
			{
				path = WorldFolder + VoxelName + "_CHUNK_" + WorldChunk + ".voxelworld_v2";
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
