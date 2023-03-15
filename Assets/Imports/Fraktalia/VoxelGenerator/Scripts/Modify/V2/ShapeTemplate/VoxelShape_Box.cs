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


	public class VoxelShape_Box : VoxelShape_Base
	{
		public Vector3 Rotation;
	

		public int InitialID = 255;
		public Vector3 Bounds;
		public float Radials;

		private Vector3 offset;
		private int innervoxelsize;
		public override void DrawEditorPreview(bool isSafe, Vector3 worldPosition, Vector3 normal)
		{
			if (!isSafe)
			{
				Gizmos.color = Color.red;
			}

			Quaternion rot = calculateRotation(Rotation);

			Vector3 rotated = MathUtilities.RotateBoundary(Bounds, rot);
			Gizmos.DrawWireCube(worldPosition , rotated);
			Gizmos.matrix = Matrix4x4.TRS(worldPosition, rot, Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, Bounds);
		}
	
		protected override void calculateTemplateData(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			

			innervoxelsize = target.GetInnerVoxelSize(modifier.Depth);
			float voxelsize = target.GetVoxelSize(modifier.Depth);

			Quaternion rot = calculateRotation(Rotation);

			Vector3 rotated = MathUtilities.RotateBoundary(Bounds, rot);
			
			int rowsX = (int)((rotated.x / voxelsize))+boundaryExtension;
			rowsX += rowsX % 2;
			int rowsY = (int)((rotated.y / voxelsize))+boundaryExtension;
			rowsY += rowsY % 2;
			int rowsZ = (int)((rotated.z / voxelsize))+boundaryExtension;
			rowsZ += rowsZ % 2;

			offset = new Vector3(rowsX, rowsY, rowsZ) * voxelsize * 0.5f;
			float maddition = 0;
			if (modifier.MarchingCubesOffset)
			{
				maddition = voxelsize * 0.5f;
			}
			Vector3 calculationOffset = (offset - Vector3.one * voxelsize * 0.5f + Vector3.one * maddition) + displacement;




			BoxCalculationJob job = new BoxCalculationJob();
			job.Rotation = Quaternion.Inverse(rot);
			job.BoxSize = Bounds/2;
			job.Radius_X = Radials;
			job.Radius_Y = Radials;
			job.Radius_Z = Radials;
			job.Offset = calculationOffset;
			job.rows = new Vector3Int(rowsX, rowsY, rowsZ);	
			job.innervoxelsize = innervoxelsize;
			job.voxelsize = voxelsize;
			job.initialID = InitialID;
			


			job.template = ModifierTemplateData;

			int totalvoxels = rowsX * rowsY * rowsZ;
			if (ModifierTemplateData.Length != totalvoxels)
			{
				ModifierTemplateData.Resize(totalvoxels, NativeArrayOptions.UninitializedMemory);
			}
			

			job.Schedule(totalvoxels, totalvoxels).Complete();
			
		}

		public override void SetGeneratorDirty(VoxelModifier_V2 modifier, VoxelGenerator target, Vector3 worldPosition)
		{
			Vector3 rotated = MathUtilities.RotateBoundary(Bounds, Quaternion.Euler(Rotation));
			float voxelsize = target.GetVoxelSize(modifier.Depth);

			Vector3 size = rotated * 0.6f + Vector3.one * voxelsize * boundaryExtension;

			target.SetRegionsDirty(target.transform.worldToLocalMatrix.MultiplyPoint3x4(worldPosition), size, size, modifier.TargetDimension);
		}

		public override Vector3 GetOffset(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			
			return -(offset);

		}

		public override int GetVoxelModificationCount(VoxelModifier_V2 modifier, VoxelGenerator target)
		{		
			float voxelsize = target.GetVoxelSize(modifier.Depth);
			Vector3 rotated = MathUtilities.RotateBoundary(Bounds, Quaternion.Euler(Rotation));

			int rowsX = (int)((rotated.x / voxelsize) + boundaryExtension);
			int rowsY = (int)((rotated.y / voxelsize) + boundaryExtension);
			int rowsZ = (int)((rotated.z / voxelsize) + boundaryExtension);
			return rowsX * rowsY * rowsZ;
		}
	}

	public struct BoxCalculationJob : IJobParallelFor
	{
		public Quaternion Rotation;
		public Vector3 BoxSize;
		public Vector3 Offset;
		public float Radius_X;
		public float Radius_Y;
		public float Radius_Z;

		public Vector3Int rows;
		public int innervoxelsize;
		public float voxelsize;
		public int initialID;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> template;

	
		
	
		

		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner result = template[index];
			Vector3Int position = MathUtilities.Convert1DTo3D(index, rows.x, rows.y, rows.z);		
			result.X = position.x * innervoxelsize;
			result.Y = position.y * innervoxelsize;
			result.Z = position.z * innervoxelsize;
	
			Vector3 p = new Vector3( position.x * voxelsize, position.y * voxelsize, position.z * voxelsize) - Offset;
			p = Rotation * p;
			


			float metavalue = EvaluateCellValue(p.x, p.y,p.z);
			int ID = (int)(initialID - initialID * (metavalue));
			result.ID = Mathf.Clamp(ID, 0, 255);

			template[index] = result;
		}

		public float EvaluateCellValue(float Pos_X, float Pos_Y, float Pos_Z)
		{
			float Dist_X = Mathf.Abs(Pos_X);
			float Dist_Y = Mathf.Abs(Pos_Y);
			float Dist_Z = Mathf.Abs(Pos_Z);

			

			float distx = Mathf.Max(0, Dist_X - (BoxSize.x - Radius_X));
			float disty = Mathf.Max(0, Dist_Y - (BoxSize.y - Radius_Y));
			float distz = Mathf.Max(0, Dist_Z - (BoxSize.z - Radius_Z));

			float distx2 = distx * distx;
			float disty2 = disty * disty;
			float distz2 = distz * distz;
			float rx2 = Radius_X * Radius_X;
			float ry2 = Radius_Y * Radius_Y;
			float rz2 = Radius_Z * Radius_Z;


			return (distx2 / rx2 + disty2 / ry2 + distz2 / rz2);
			


		}
	}
}
