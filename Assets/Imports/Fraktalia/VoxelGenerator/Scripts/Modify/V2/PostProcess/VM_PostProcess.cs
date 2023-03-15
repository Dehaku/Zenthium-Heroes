using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Modify
{
	public class VM_PostProcess : MonoBehaviour
	{
		public bool Enabled = true;
		public virtual void ApplyPostprocess(FNativeList<NativeVoxelModificationData_Inner> modifierData, VoxelGenerator generator, VoxelModifier_V2 modifier)
		{

		}

		public virtual void FinalizeModification(FNativeList<NativeVoxelModificationData_Inner> modifierData,
			FNativeList<NativeVoxelModificationData_Inner> preVoxelData,
			FNativeList<NativeVoxelModificationData_Inner> postVoxelData, VoxelGenerator generator, VoxelModifier_V2 modifier)
		{

		}
	}
}
