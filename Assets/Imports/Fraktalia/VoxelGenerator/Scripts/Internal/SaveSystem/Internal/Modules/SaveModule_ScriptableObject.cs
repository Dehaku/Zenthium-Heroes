using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fraktalia.VoxelGen.SaveSystem.Modules;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[System.Serializable]
	public class SaveModule_ScriptableObject : SaveModule
	{
		[Tooltip("Voxel Map File is a Scriptable Object stored in the asset database similar to Terrain Map")]
		public VoxelMap _VoxelMap;

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

		public override string GetInformation()
		{
			return "Info: Scriptable Object saves the data into the asset database " +
						"which is faster, does not slowdown the editor and easier for version control systems (git)";
		}

		public override IEnumerator Save()
		{
#if UNITY_EDITOR
			if (!_VoxelMap) CreateVoxelMap();
			_VoxelMap.VoxelData = VoxelSaveSystem.ConvertRaw(saveSystem.nativevoxelengine);
			EditorUtility.SetDirty(_VoxelMap);
#endif
			yield break;
		}

		public override IEnumerator Load()
		{
			if (_VoxelMap)
			{
				if (!_VoxelMap.VoxelData.IsValid) yield break;

				saveSystem.nativevoxelengine.SubdivisionPower = _VoxelMap.VoxelData.SubdivisionPower;
				saveSystem.nativevoxelengine.RootSize = _VoxelMap.VoxelData.RootSize;
				saveSystem.nativevoxelengine.InitialValue = 0;

				saveSystem.setupGenerator(saveSystem.nativevoxelengine, _VoxelMap.VoxelData);
				yield return new WaitForEndOfFrame();

				NativeVoxelModificationData[] data = _VoxelMap.VoxelData.VoxelData;
				saveSystem.nativevoxelengine._SetVoxels(data, 0);

				yield return new WaitForEndOfFrame();
				if (_VoxelMap.VoxelData.AdditionalData != null)
				{
					for (int i = 0; i < _VoxelMap.VoxelData.AdditionalData.Length; i++)
					{
						saveSystem.nativevoxelengine._SetVoxels(_VoxelMap.VoxelData.AdditionalData[i].VoxelData, i + 1);

						yield return new WaitForEndOfFrame();
					}
				}

				saveSystem.nativevoxelengine.SetAllRegionsDirty();
				yield break;
			}

			yield break;

		}

		public override bool HasSaveData()
		{
			if (!_VoxelMap) return false;
			return _VoxelMap.VoxelData.IsValid;
		}

#if UNITY_EDITOR
		public void CreateVoxelMap()
		{
			_VoxelMap = ScriptableObject.CreateInstance<VoxelMap>();
			_VoxelMap.name = "VoxelMap_" + saveSystem.GetInstanceID();

			if (SaveInSceneSubFolder)
			{
				string path = SceneManager.GetActiveScene().path;
				string scenename = SceneManager.GetActiveScene().name;

				path = path.Replace("/" + scenename + ".unity", "");

				if (!AssetDatabase.IsValidFolder(path + "/" + scenename))
				{
					AssetDatabase.CreateFolder(path, scenename);
				}

				string voxelpath = path + "/" + scenename + "/";

				AssetDatabase.CreateAsset(_VoxelMap, voxelpath + "VoxelMap_" + saveSystem.GetInstanceID() + ".asset");
			}
			else
			{
				AssetDatabase.CreateAsset(_VoxelMap, "Assets/VoxelMap_" + saveSystem.GetInstanceID() + ".asset");
			}
		}
#endif

		public override void DestroySaveData()
		{
#if UNITY_EDITOR
			if ((Application.isPlaying == false) && (Application.isEditor == true))
			{
				if (Time.frameCount > 0 && Time.renderedFrameCount > 0)
				{
					if (saveSystem.EditorSaveMode == EditorVoxelSaveMode.ScriptableObject)
					{
						if (_VoxelMap && RemoveVoxelmapOnDelete)
						{
							string path = AssetDatabase.GetAssetPath(_VoxelMap);
							AssetDatabase.DeleteAsset(path);
						}
					}
				}
			}
#endif
		}
	}
}
