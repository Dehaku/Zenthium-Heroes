using Fraktalia.Core.Collections;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify
{
	

	public class VoxelShape_Base : MonoBehaviour
	{

		public bool ApplyObjectRotation;
		
		
		public FNativeList<NativeVoxelModificationData_Inner> ModifierTemplateData;

		protected Vector3 displacement;
		public int boundaryExtension = 1;

		protected float checksum = 0;

		public virtual void DrawEditorPreview(bool isSafe, Vector3 worldPosition, Vector3 worldNormal)
		{

		}

		public virtual Vector3 GetGameIndicatorSize()
		{
			return Vector3.one;
		}

		public void CalculateDisplacement(Vector3 worldPosition, VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			float voxelsize = target.GetVoxelSize(modifier.Depth);

			Vector3 localcoord = target.transform.worldToLocalMatrix.MultiplyPoint(worldPosition);	
			displacement = new Vector3(localcoord.x % voxelsize, localcoord.y % voxelsize, localcoord.z % voxelsize);

		}

		public void CreateModifierTemplate(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			if (!ModifierTemplateData.IsCreated) ModifierTemplateData = new FNativeList<NativeVoxelModificationData_Inner>(Allocator.Persistent);
			ModifierTemplateData.Clear();

			calculateTemplateData(modifier, target);

		}

		protected virtual void calculateTemplateData(VoxelModifier_V2 modifier, VoxelGenerator target)
		{

		}

		public virtual void SetGeneratorDirty(VoxelModifier_V2 modifier, VoxelGenerator target, Vector3 worldPosition)
		{

		}

		public virtual Vector3 GetOffset(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			return Vector3.zero;
		}

		public virtual int GetVoxelModificationCount(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			return 0;
		}

		public void CleanUp()
		{
			checksum = float.MaxValue;
			if (ModifierTemplateData.IsCreated) ModifierTemplateData.Dispose();
		
		}

		public virtual bool RequiresRecalculation(VoxelModifier_V2 modifier, VoxelGenerator target)
		{			
			return true;
		}
		
		protected virtual float getCheckSum()
		{
			return 0;
		}

		protected Quaternion calculateRotation(Vector3 eulerRotation)
		{
			Quaternion rot = Quaternion.Euler(eulerRotation);
			if (ApplyObjectRotation)
			{
				rot *= transform.rotation;
			}
			return rot;
		}
	}
}
