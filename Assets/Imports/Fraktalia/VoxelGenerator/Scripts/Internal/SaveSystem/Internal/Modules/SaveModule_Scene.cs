using Fraktalia.VoxelGen;
using Fraktalia.VoxelGen.SaveSystem.Modules;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


namespace Fraktalia.VoxelGen.SaveSystem.Modules
{
	[System.Serializable]
	public class SaveModule_Scene : SaveModule
	{
		[SerializeField][HideInInspector]
		public RawVoxelData SceneVoxelData;

		public override string GetInformation()
		{
			return "Info: Scene mode directly saves the data into the unity scene file. " +
				"Is the slowest but most convenient saving mode. " +
				"However causes slowdowns in editor when high resolution voxelmaps are used";
		}

		public override IEnumerator Save()
		{
#if UNITY_EDITOR
			SceneVoxelData = VoxelSaveSystem.ConvertRaw(saveSystem.nativevoxelengine);
			EditorUtility.SetDirty(saveSystem.nativevoxelengine);
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
#endif

			yield break;
		}

		public override IEnumerator Load()
		{
			if (!SceneVoxelData.IsValid) yield break;

			saveSystem.nativevoxelengine.SubdivisionPower = SceneVoxelData.SubdivisionPower;
			saveSystem.nativevoxelengine.RootSize = SceneVoxelData.RootSize;
			saveSystem.nativevoxelengine.InitialValue = 0;
			saveSystem.nativevoxelengine.GenerateBlock();

			NativeVoxelModificationData[] data = SceneVoxelData.VoxelData;
			saveSystem.nativevoxelengine._SetVoxels(data, 0);

			yield return new WaitForEndOfFrame();
			if (SceneVoxelData.AdditionalData != null)
			{
				for (int i = 0; i < SceneVoxelData.AdditionalData.Length; i++)
				{
					saveSystem.nativevoxelengine._SetVoxels(SceneVoxelData.AdditionalData[i].VoxelData, i + 1);

					yield return new WaitForEndOfFrame();
				}
			}

			saveSystem.nativevoxelengine.SetAllRegionsDirty();
			yield break;
		}

#if UNITY_EDITOR
		public override void DrawInspector(SerializedObject serializedObject)
		{
			base.DrawInspector(serializedObject);

			if (HasSaveData())
			{
				if (GUILayout.Button("Destroy Scene Voxeldata"))
				{
					DestroySaveData();
				}
			}
		}
#endif
		public override bool HasSaveData()
		{
			return SceneVoxelData.IsValid;
		}

		public override void DestroySaveData()
		{
			SceneVoxelData = new RawVoxelData();
		}
	}
}
