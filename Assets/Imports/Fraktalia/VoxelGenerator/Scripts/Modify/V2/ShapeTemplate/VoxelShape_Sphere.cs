using Fraktalia.Core.Collections;
using Fraktalia.Core.Math;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify
{


	public class VoxelShape_Sphere : VoxelShape_Base
	{
		public int InitialID = 255;
		public float Radius;	
			
		private float offset;
		private SphereCalculationJob job;

		public override void DrawEditorPreview(bool isSafe, Vector3 worldPosition, Vector3 normal)
		{
			if (!isSafe)
			{
				Gizmos.color = Color.red;
			}
			Gizmos.color = new Color32(150, 150, 255, 200);

			for (int i = 0; i < 8; i++)
            {
				Gizmos.matrix = Matrix4x4.TRS(worldPosition, Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, (360/8/4 * i), 0), Vector3.one);
				Gizmos.DrawWireCube(Vector3.zero, new Vector3(Radius * 2, 0, Radius * 2));
				Gizmos.matrix = Matrix4x4.identity;
			}
			
		
			Gizmos.DrawWireSphere(worldPosition, Radius);			
		}

		public override Vector3 GetGameIndicatorSize()
		{
			return new Vector3(Radius*2,Radius*2,Radius*2);
		}

		protected override void calculateTemplateData(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			
			float voxelsize = target.GetVoxelSize(modifier.Depth);
			int rows = (int)(Radius * 2 / voxelsize) + boundaryExtension;
			rows += rows % 2;
			offset = rows * voxelsize *0.5f;
			float maddition = 0;
			if (modifier.MarchingCubesOffset)
			{
				maddition = voxelsize * 0.5f;
			}
			Vector3 calculationOffset = Vector3.one * (offset - voxelsize * 0.5f + maddition) + displacement;


			
				
			job.radius = Radius;		
			job.rows = rows;	
			job.voxelsize = voxelsize;
			job.innervoxelsize = target.GetInnerVoxelSize(modifier.Depth);
			job.initialID = InitialID;
			job.offset = calculationOffset;
			job.template = ModifierTemplateData;

			int totalvoxels = rows * rows * rows;
			if (ModifierTemplateData.Length != totalvoxels)
			{
				ModifierTemplateData.Resize(totalvoxels, NativeArrayOptions.UninitializedMemory);
			}
			
			job.Schedule(totalvoxels, rows).Complete();	
		}

		public override void SetGeneratorDirty(VoxelModifier_V2 modifier, VoxelGenerator target, Vector3 worldPosition)
		{
			float voxelsize = target.GetVoxelSize(modifier.Depth);

			target.SetRegionsDirty(target.transform.worldToLocalMatrix.MultiplyPoint3x4(worldPosition), Vector3.one * (Radius + voxelsize * boundaryExtension/2), Vector3.one * (Radius + voxelsize * boundaryExtension / 2), modifier.TargetDimension);
		}

		public override Vector3 GetOffset(VoxelModifier_V2 modifier, VoxelGenerator target)
		{	
			return -Vector3.one * (offset); 
		}

		public override int GetVoxelModificationCount(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			float voxelsize = target.GetVoxelSize(modifier.Depth);	
			int rows = (int)(Radius * 2 / voxelsize) + boundaryExtension;

			return rows * rows * rows;
		}
	}

	public struct SphereCalculationJob : IJobParallelFor
	{
		public float radius;		
		public int rows;
		public Vector3 offset;
		public int innervoxelsize;
		public float voxelsize;
		public int initialID;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> template;
		

		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner result = template[index];
			Vector3Int position = MathUtilities.Convert1DTo3D(index, rows, rows, rows);
			result.X = position.x * innervoxelsize;
			result.Y = position.y * innervoxelsize;
			result.Z = position.z * innervoxelsize;

		
			Vector3 p = new Vector3(position.x * voxelsize, position.y * voxelsize, position.z * voxelsize);

			float Pos_X = p.x;
			float Pos_Y = p.y;
			float Pos_Z = p.z;

			

			float Dist_X = Pos_X - offset.x;
			float Dist_Y = Pos_Y - offset.y;
			float Dist_Z = Pos_Z - offset.z;

			float distsquared = Dist_X * Dist_X + Dist_Y * Dist_Y + Dist_Z * Dist_Z;
			
			float metavalue = (distsquared) / (radius*radius);
			

			int ID = (int)(initialID - initialID * (metavalue));
			result.ID = Mathf.Clamp(ID, 0, 255);

			template[index] = result;
		}
	}
}
