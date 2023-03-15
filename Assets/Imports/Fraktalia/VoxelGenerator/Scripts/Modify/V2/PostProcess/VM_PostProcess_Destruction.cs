using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen.Modify
{
	public class VM_PostProcess_Destruction : VM_PostProcess
	{
		public Transform DestructionPrefab;

		public override void FinalizeModification(FNativeList<NativeVoxelModificationData_Inner> modifierData, FNativeList<NativeVoxelModificationData_Inner> preVoxelData, FNativeList<NativeVoxelModificationData_Inner> postVoxelData, VoxelGenerator generator, VoxelModifier_V2 modifier)
		{
			
			for (int i = 0; i < preVoxelData.Length; i++)
			{
				if(preVoxelData[i].ID >= 128)
				{
					var data = postVoxelData[i];
					if(data.ID < 128)
					{
						Transform newobject = Instantiate<Transform>(DestructionPrefab);
						newobject.transform.position = generator.ConvertInnerToWorld(new Vector3Int(data.X, data.Y, data.Z), generator.RootSize);
						newobject.transform.localScale = Vector3.one * generator.GetVoxelSize(data.Depth);
					}
				}
			}



		}
	}



}
