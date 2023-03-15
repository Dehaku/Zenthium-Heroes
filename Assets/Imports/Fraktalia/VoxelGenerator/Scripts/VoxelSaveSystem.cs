using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using System.Collections;
using Unity.Jobs;
using System;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.VoxelGen.SaveSystem.Modules;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif




namespace Fraktalia.VoxelGen
{
	public enum EditorVoxelSaveMode
	{
		ScriptableObject,
		Scene,
		PersistentDatapath,
		World,
		Region,
		PersistentDatapath_V2,
		World_V2,
		Region_V2,
		SaveToPath_V2,
		ByteBuffer_V2
	}

	[ExecuteInEditMode]
	public class VoxelSaveSystem : MonoBehaviour
	{
	

		[BeginInfo("VOXELSAVESYSTEM")]
		[InfoTitle("Voxel Save System", "This is the main script to save your voxel maps and provides a variety of saving options. " +
		"When attached to a generator, saving the scene will also save the voxelmap if you have modified inside the editor before.\n\n" +
		"The save system also provides a functionality to load the voxel generator on startup during playmode and to automatically load the map during edit mode when loading the scene file. " +
		"These two features can be activated using the <b>Auto Load in Editor/Load on start</b> toggles.\n\n" +
		"You have the option to save directly into the scene, as scriptable object and as .Voxel file into the persistent data path.", "VOXELSAVESYSTEM")]
		[InfoSection1("Scene:", "This will save the voxel data directly into the unity .scene file. However it increases the scene file which reduces loading time " +
		"Since the voxel map is a serializable container, the inspector will slow down during edit mode which reduces performance (editor only)" +
		"Ingame sculpting is not affected by the slowdown. Saving into the scene is only possible inside the editor.", "VOXELSAVESYSTEM")]
		[InfoSection2("Scriptable Object:", "Saves the voxel map as scriptable objects. Is the ideal saving mode for blocks created inside the editor." +
		"If no scriptable object exists, a new one is automatically created and assigned to it. " +
		"The location is either the default Assets folder or stored inside the Scene subfolder if <b>Save in Scene Subfolder</b> is set.\n\n" +
		"If <b>Duplicate on clone</b> is set, the scriptable object is duplicated when you duplicate the voxel generator. \n\nThe duplicated object gets a new ID and is assigned to the new object. " +
		"Deleting a voxel generator will remove the scriptable object if <b>Remove Voxelmap on Delete</b>. Also scriptable objects saving only works in editor.", "VOXELSAVESYSTEM")]
		[InfoSection3("Persistent Datapath/World/Region:", "Saves the voxel map as .VOXEL file into the persistent datapath." +
		"This is the ideal saving mode to save voxeldata ingame by the user. The .Voxel file is in binary and could be sent over your network if you make a multiplayer game.\n\n" +
		"<color=yellow>Keep in mind that every user has his own persistent datapath. This means that the user will not have the .Voxel file you saved during edit mode.</color>\n\n" +
		"The mode World and Region are identically to the persistent datapath and are used by infinite world systems. " +
			"These saving modes have an additional chunk system in order to be able to know the chunk it belonged to.", "VOXELSAVESYSTEM")]
		[InfoText("Voxel Save System", "VOXELSAVESYSTEM")]
		[InfoVideo("https://www.youtube.com/watch?v=yWqkizPsjUs&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=8", false, "VOXELSAVESYSTEM")]
		[Tooltip("Loads in the editor as soon as the scene is reloaded.")]
		public bool AutoLoad = true;
		[Tooltip("Loads when the object is first instantiated in a scene during play.")]
		public bool LoadOnStart = true;

		#region OLD PARAMETERS	
		public bool __migrated = false;

		[SerializeField]
		private RawVoxelData SceneVoxelData;

		[HideInInspector]
		public EditorVoxelSaveMode EditorSaveMode;
		public int DynamicLoadSpeed = 100;

		[NonSerialized]
		private RawVoxelData DynamicData;
		private RawVoxelData_V2 DynamicData_V2;

		[Tooltip("Voxel Map File is a Scriptable Object stored in the asset database similar to Terrain Map")]
		public VoxelMap _VoxelMap;
		public VoxelGenerator nativevoxelengine;

		[Tooltip("Folder where World Information should be contained")]
		public string WorldFolder;

		[Tooltip("World data created during edit mode will be saved in a subfolder with the scene name. " +
			"The subfolder is created inside the folder of the scene file if required. " +
			"If false, you can set the path manually")]
		public bool WorldToSceneLocation = true;

		[Tooltip("Target Chunk of the World.")]
		public Vector3Int WorldChunk;
		
		[Range(2, 128)]
		public int WorldRegionSize = 32;

		[Tooltip("Rule how data should be loaded." +
			"\n AssetOnly: Only data from the AssetDatabase is loaded.\n" +
			"PeristentOnly: Only locally stored data is loaded (For user generated content) \n" +
			"AssetFirst: Data in the asset database is priorized else local data is loaded\n" +
			"PersistentFirst: Data created during gameplay is priorized. If no local data exists, data from asset database is loaded.")]
		[EnumButtons]
		public WorldLoadingRules WorldLoadingRule;

		public string VoxelName;
		

		[Tooltip("If true, new Voxelmap is created when this GameObject is duplicated inside the editor. " +
			"Location is the folder of the original Voxelmap.")]
		public bool DuplicateMapOnClone;

		[Tooltip("Automatic created Voxelmap will be saved in a subfolder with the scene name. " +
			"The subfolder is created inside the folder of the scene file if required. " +
			"If false, location of new maps is simply the root (Assets) folder")]
		public bool SaveInSceneSubFolder = true;

		[Tooltip("Removes the Voxelmap file when the GameObject is deleted during edit mode inside the editor. " +
			"Note: This process is not undoable since the voxelmap is deleteted permanently")]
		public bool RemoveVoxelmapOnDelete = false;
		#endregion

		[HideInInspector]
		public bool IsDirty;

		public IEnumerator ActiveCoroutine;
		[NonSerialized]
		public bool IsWorking;
		[NonSerialized]
		public bool IsSaving;
		[NonSerialized]
		public bool IsLoading;



		[NonSerialized]
		private bool hasloaded = false;
		private FileStream file;
		private BinaryReader br;

		public static NativeVoxelConverter converter;
		public static NativeVoxelConverter_V2 converter_v2;

		public SaveModule_Scene ModuleScene;
		public SaveModule_ScriptableObject ModuleScriptableObject;
		public SaveModule_PersistentDatapath ModulePersistent;
		public SaveModule_World ModuleWorld;
		public SaveModule_Region ModuleRegion;

		public SaveModule_PersistentDatapath_V2 ModulePersistent_V2;
		public SaveModule_World_V2 ModuleWorld_V2;
		public SaveModule_SaveToPath_V2 ModulePath_V2;
		public SaveModule_ByteBuffer_V2 ModuleByteBuffer_V2;

		public SaveModule SelectedModule {
			get
			{
				SaveModule module = null;
				switch (EditorSaveMode)
				{
					case EditorVoxelSaveMode.ScriptableObject:
						module = ModuleScriptableObject;
						break;
					case EditorVoxelSaveMode.Scene:
						module = ModuleScene;
						break;
					case EditorVoxelSaveMode.PersistentDatapath:
						module = ModulePersistent;
						break;
					case EditorVoxelSaveMode.World:
						module = ModuleWorld;
						break;
					case EditorVoxelSaveMode.Region:
						module = ModuleRegion;
						break;
					case EditorVoxelSaveMode.PersistentDatapath_V2:
						module = ModulePersistent_V2;
						break;
					case EditorVoxelSaveMode.World_V2:
						module = ModuleWorld_V2;
						break;
					case EditorVoxelSaveMode.Region_V2:
						break;
					case EditorVoxelSaveMode.SaveToPath_V2:
						module = ModulePath_V2;
						break;
					case EditorVoxelSaveMode.ByteBuffer_V2:
						module = ModuleByteBuffer_V2;
						break;
					default:
						break;
				}

				if(module == null)
				{
					module = new SaveModule();
				}

				module.saveSystem = this;
				return module;
			}
		}

		public bool eventsfoldout;
		public UnityEvent OnDataSaved;
		public UnityEvent OnDataLoaded;


		private void OnDrawGizmos()
		{
			if (!__migrated)
			{
				Migrate();
			}

#if UNITY_EDITOR	
			if (EditorApplication.isCompiling)
			{	
				return;
			}
#endif

			if (!Application.isPlaying)
			{
				if (!nativevoxelengine) nativevoxelengine = GetComponent<VoxelGenerator>();

				if (hasloaded) return;


				if (AutoLoad && enabled)
				{
					if (!nativevoxelengine) return;

					if (nativevoxelengine.HullGeneratorsWorking) return;
					if (!nativevoxelengine.IsInitialized)
					{
						hasloaded = true;
						Load();
					}
				}
			}


		}

        private void Start()
		{
			if (!Application.isPlaying) return;

			if (LoadOnStart)
			{
				if (!nativevoxelengine.IsExtension)
				{
					
					Load();
					

					nativevoxelengine.Forcefinish = true;
				}
			}
		}

		public static void CleanStatics()
		{
			converter.CleanUp();
			converter_v2.CleanUp();
		}


		public void Migrate()
		{
#if UNITY_EDITOR
			__migrated = true;
			ModuleScene.SceneVoxelData = SceneVoxelData;

			ModuleScriptableObject._VoxelMap = _VoxelMap;
			ModuleScriptableObject.SaveInSceneSubFolder = SaveInSceneSubFolder;
			ModuleScriptableObject.RemoveVoxelmapOnDelete = RemoveVoxelmapOnDelete;
			ModuleScriptableObject.DuplicateMapOnClone = DuplicateMapOnClone;


			ModulePersistent.VoxelName = VoxelName;

			ModuleWorld.VoxelName = VoxelName;
			ModuleWorld.WorldFolder = WorldFolder;
			ModuleWorld.WorldChunk = WorldChunk;
			ModuleWorld.WorldToSceneLocation = WorldToSceneLocation;
			ModuleWorld.WorldLoadingRule = WorldLoadingRule;

			ModuleRegion.WorldRegionSize = WorldRegionSize;
			ModuleRegion.VoxelName = VoxelName;
			ModuleRegion.WorldFolder = WorldFolder;
			ModuleRegion.WorldChunk = WorldChunk;
			ModuleRegion.WorldToSceneLocation = WorldToSceneLocation;
			ModuleRegion.WorldLoadingRule = WorldLoadingRule;


			EditorUtility.SetDirty(this);
#endif
		}

		/// <summary>
		/// Instant saving method. Causes performance spike as saving completes instantly.
		/// </summary>
		public void Save()
		{
			if (!IsDirty) return;		
			if (!nativevoxelengine) return;
			if (!nativevoxelengine.IsInitialized) return;
			if (IsWorking) return;

			nativevoxelengine.Locked = true;
			IsDirty = false;
			IsWorking = true;
			IsSaving = true;
			ActiveCoroutine = SelectedModule.Save();
		}

		/// <summary>
		/// Applies RawVoxelData to the target VoxelGenerator 
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="data"></param>
		public static void ApplyRawVoxelData(VoxelGenerator engine, RawVoxelData data)
		{
			if (!data.IsValid) return;

			bool requiresinitialisation = false;
			if (engine.IsInitialized)
			{
				if (engine.SubdivisionPower != data.SubdivisionPower ||
					engine.RootSize != data.RootSize ||
					engine.DimensionCount < Mathf.Clamp(data.DimensionCount, 1, 5))
				{
					requiresinitialisation = true;
				}

			}
			else
			{
				requiresinitialisation = true;
			}

			if (requiresinitialisation)
			{
				engine.CleanUp();
				engine.SubdivisionPower = data.SubdivisionPower;
				engine.RootSize = data.RootSize;
				engine.DimensionCount = Mathf.Max(engine.DimensionCount, Mathf.Clamp(data.DimensionCount, 1, 5));
				engine.GenerateBlock();
			}


			if (!engine.IsInitialized) return;
			if (requiresinitialisation)
			{
				engine.SupressPostProcess = true;
			}
			else
			{
				engine._SetVoxel(new Vector3(0, 0, 0), 0, (byte)engine.InitialValue, 0);

				for (int i = 1; i < engine.DimensionCount; i++)
				{
					engine._SetVoxel(new Vector3(0, 0, 0), 0, 0, i);
				}

			}

			engine._SetVoxels(data.VoxelData, 0);

			if (data.AdditionalData != null)
			{
				for (int i = 0; i < data.AdditionalData.Length; i++)
				{
					engine._SetVoxels(data.AdditionalData[i].VoxelData, i + 1);
				}
			}

			engine.SetAllRegionsDirty();
		}

		public bool SaveDataExists
		{
			get
			{
				return SelectedModule.HasSaveData();
			}
		}

		public void Load()
		{
			
			if (!nativevoxelengine) return;
			if (nativevoxelengine.Locked) return;			
			if (IsWorking) return;
			IsWorking = true;
			IsLoading = true;
			nativevoxelengine.Locked = true;


			ActiveCoroutine = SelectedModule.Load();
			UpdateRoutines();
		}

		public void UpdateRoutines()
		{
			if(ActiveCoroutine != null)
			{
				if(!ActiveCoroutine.MoveNext())
				{
					if(IsWorking)
					{
						IsWorking = false;
						nativevoxelengine.Locked = false;

						if (IsSaving)
						{
							OnDataSaved.Invoke();
							IsSaving = false;
						}

						if (IsLoading)
						{
							OnDataLoaded.Invoke();
							IsLoading = false;
						}
					}
				}		
			}
			else
			{
				IsWorking = false;
				
			}


		}

		private void OnDestroy()
		{
			StopAllCoroutines();


#if UNITY_EDITOR
			
			if ((Application.isPlaying == false) && (Application.isEditor == true))
			{
				if (Time.frameCount > 0 && Time.renderedFrameCount > 0)
				{
					if(EditorSaveMode == EditorVoxelSaveMode.ScriptableObject)
					{
						if(_VoxelMap && RemoveVoxelmapOnDelete)
						{
							string path = AssetDatabase.GetAssetPath(_VoxelMap);
							AssetDatabase.DeleteAsset(path);
						}

					}					
				}
			}

#endif
		}

		/// <summary>
		/// Converts the voxel data of a initialized VoxelGenerator into a format which can be stored.
		/// Shrinks the data by gathering the leaves only.
		/// </summary>
		/// <param name="generator"></param>
		/// <returns>Returns RawVoxelData which can be saved or applied to an existing VoxelGenerator.</returns>
		public static RawVoxelData ConvertRaw(VoxelGenerator generator)
		{
			RawVoxelData output = new RawVoxelData();
			output.SubdivisionPower = generator.SubdivisionPower;
			output.RootSize = generator.RootSize;
			output.VoxelData = new NativeVoxelModificationData[0];
			if (!generator.IsInitialized) return output;

			converter.Init();
			converter.data = generator.Data[0];		
			converter.Schedule().Complete();

			output.VoxelData = converter.output.ToArray();
			output.VoxelCount = output.VoxelData.Length;
			
			output.IsValid = true;

			output.DimensionCount = generator.Data.Length;
			output.AdditionalData = new AdditionaVoxelData[output.DimensionCount - 1];

			for (int i = 0; i < output.AdditionalData.Length; i++)
			{
				output.AdditionalData[i] = new AdditionaVoxelData();

				converter.data = generator.Data[i+1];

				JobHandle additional = converter.Schedule();
				additional.Complete();
				output.AdditionalData[i].VoxelData = converter.output.ToArray();
				output.AdditionalData[i].VoxelCount = output.AdditionalData[i].VoxelData.Length;

			}

			return output;
		}

		public static void ConvertRaw_V2(VoxelGenerator generator, ref RawVoxelData_V2 output)
		{
			if (!generator.IsInitialized)
			{
				output.IsValid = false;
				return;
			}

			output.Version = 2;
			output.SubdivisionPower = generator.SubdivisionPower;
			output.RootSize = generator.RootSize;

			output.DimensionCount = generator.DimensionCount;	
			if (generator.DimensionCount != output.BytedVoxelData.Count)
			{
				output.VoxelCount = new int[output.DimensionCount];
				output.BytedVoxelData.Clear();
				for (int i = 0; i < output.DimensionCount; i++)
				{
					output.BytedVoxelData.Add(new byte[0]);
				}
			}
			
			converter_v2.Init();

			for (int i = 0; i < output.DimensionCount; i++)
			{
				converter_v2.data = generator.Data[i];
				converter_v2.Schedule().Complete();
				output.VoxelCount[i] = converter_v2.leafvoxels.Length;
				output.BytedVoxelData[i] = converter_v2.output.ToArray();
			}

			output.IsValid = true;		
		}

		public void SetDirty()
		{
#if UNITY_EDITOR
			if(nativevoxelengine)
			{
				EditorUtility.SetDirty(nativevoxelengine);
			}
			
#endif
			IsDirty = true;
		}

		
		#region Setup Generator		
		public void setupGenerator(VoxelGenerator engine, RawVoxelData data)
		{
			bool requiresinitialisation = !engine.IsInitialized;


			if (engine.SubdivisionPower != data.SubdivisionPower ||
				engine.RootSize != data.RootSize ||
				engine.DimensionCount < Mathf.Clamp(data.DimensionCount, 1, 5))
			{
				requiresinitialisation = true;
			}

			
			
			if (requiresinitialisation)
			{
				engine.CleanUp();
				engine.SubdivisionPower = data.SubdivisionPower;
				engine.RootSize = data.RootSize;
				engine.DimensionCount = Mathf.Max(engine.DimensionCount, Mathf.Clamp(data.DimensionCount, 1, 5));
				engine.GenerateBlock();
			}
			else
			{
				engine.ResetData();
			}
		}

		public void setupGenerator(VoxelGenerator engine, RawVoxelData_V2 data)
		{
			bool requiresinitialisation = !engine.IsInitialized;
			if (engine.SubdivisionPower != data.SubdivisionPower ||
				engine.RootSize != data.RootSize ||
				engine.DimensionCount < Mathf.Clamp(data.DimensionCount, 1, 5))
			{
				requiresinitialisation = true;
			}
			if (requiresinitialisation)
			{
				engine.CleanUp();
				engine.SubdivisionPower = data.SubdivisionPower;
				engine.RootSize = data.RootSize;
				engine.DimensionCount = Mathf.Max(engine.DimensionCount, Mathf.Clamp(data.DimensionCount, 1, 5));
				engine.GenerateBlock();
			}
			else
			{
				engine.ResetData();
			}
		}
		#endregion
	}


#if UNITY_EDITOR
	public class VoxelSaveSystemSaver : UnityEditor.AssetModificationProcessor
	{
		static string[] OnWillSaveAssets(string[] paths)
		{

			VoxelSaveSystem[] voxelengines = Resources.FindObjectsOfTypeAll<VoxelSaveSystem>();
			for (int i = 0; i < voxelengines.Length; i++)
			{
				if (!voxelengines[i].nativevoxelengine) continue;

				if (EditorUtility.IsDirty(voxelengines[i].nativevoxelengine.GetInstanceID()) || EditorUtility.IsDirty(voxelengines[i].GetInstanceID()))
				{
					voxelengines[i].Save();
				}
			}
			return paths;
		}
	}

	[CanEditMultipleObjects]
	[CustomEditor(typeof(VoxelSaveSystem))]
	public class VoxelSaveSystemEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			VoxelSaveSystem myTarget = (VoxelSaveSystem)target;

			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;


			EditorStyles.textField.wordWrap = true;

			EditorGUILayout.LabelField("Is Working: " + myTarget.IsWorking);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("AutoLoad"), new GUIContent("Auto load in editor"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("LoadOnStart"), new GUIContent("Load on start"));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=green>Saving Mode:</color>", title);


			EditorGUILayout.PropertyField(serializedObject.FindProperty("EditorSaveMode"), new GUIContent("Saving Mode"));

			EditorGUILayout.Space();

			

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=green>Module Parameters:</color>", title);

			switch (myTarget.EditorSaveMode)
			{
				case EditorVoxelSaveMode.ScriptableObject:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModuleScriptableObject"), true);
					break;
				case EditorVoxelSaveMode.Scene:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModuleScene"), true);
					break;
				case EditorVoxelSaveMode.PersistentDatapath:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModulePersistent"), true);
					break;
				case EditorVoxelSaveMode.World:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModuleWorld"), true);
					break;
				case EditorVoxelSaveMode.Region:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModuleRegion"), true);
					break;
				case EditorVoxelSaveMode.PersistentDatapath_V2:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModulePersistent_V2"), true);
					break;
				case EditorVoxelSaveMode.World_V2:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModuleWorld_V2"), true);
					break;
				case EditorVoxelSaveMode.Region_V2:
					break;
				case EditorVoxelSaveMode.SaveToPath_V2:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModulePath_V2"), true);
					break;
				case EditorVoxelSaveMode.ByteBuffer_V2:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ModuleByteBuffer_V2"), true);
					break;
				default:
					break;
			}


			EditorGUILayout.Space();
			EditorGUILayout.Space();


			EditorGUILayout.LabelField("<color=green>Module Options:</color>", title);

			myTarget.SelectedModule.DrawInspector(serializedObject);

			EditorGUILayout.Space();
			if (GUILayout.Button("Save"))
			{
				myTarget.IsDirty = true;
				myTarget.Save();
			}

			if (GUILayout.Button("Load"))
			{
				myTarget.Load();
			}

			EditorGUILayout.Space();
			if (GUILayout.Button("Open Persistent Datapath"))
			{
				EditorUtility.RevealInFinder(Application.persistentDataPath);
			}

			EditorGUILayout.Space();
			myTarget.eventsfoldout = EditorGUILayout.Foldout(myTarget.eventsfoldout,"Events");
			if (myTarget.eventsfoldout)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("OnDataSaved"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("OnDataLoaded"));
			}

			if (GUI.changed)
			{
				serializedObject.ApplyModifiedProperties();
			    myTarget.SetDirty();
			}


		}


	}
#endif
}
