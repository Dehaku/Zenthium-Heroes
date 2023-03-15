using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Fraktalia.VoxelGen.Modify.Filter
{
	
	public class SetID : BaseVoxelFilter
	{
		public int TargetID;
		SetID_calculate calculate;

		protected override void calculation(VoxelGenerator engine, Vector3 center, Quaternion rotation, int depth)
		{
			calculate.Rotation = rotation;
			calculate.Size_X = Size_X;
			calculate.Size_Y = Size_Y;
			calculate.Size_Z = Size_Z;
			calculate.Size_X_half = Size_X / 2.0f;
			calculate.Size_Y_half = Size_Y / 2.0f;
			calculate.Size_Z_half = Size_Z / 2.0f;
			calculate.RadiusCircle_X = RadiusCircle_X;
			calculate.RadiusCircle_Y = RadiusCircle_Y;
			calculate.RadiusCircle_Z = RadiusCircle_Z;
			calculate.RadiusSphere = RadiusSphere * RadiusSphere;

			calculate.RadiusCircle_X2 = RadiusCircle_X * RadiusCircle_X;
			calculate.RadiusCircle_Y2 = RadiusCircle_Y * RadiusCircle_Y;
			calculate.RadiusCircle_Z2 = RadiusCircle_Z * RadiusCircle_Z;
			float VoxelSize = calculate.VoxelSize = engine.Data[0].SizeTable[depth];
			calculate.RootSize = engine.RootSize;
			calculate.StartPos_X = center.x - (VoxelSize * Size_X / 2);
			calculate.StartPos_Y = center.y - (VoxelSize * Size_Y / 2);
			calculate.StartPos_Z = center.z - (VoxelSize * Size_Z / 2);

			calculate.output = output;
			calculate.Init(engine, Modifications, depth);
			calculate.TargetID = TargetID;
			JobHandle handle = calculate.Schedule(Modifications, 100);
			handle.Complete();
		}


	}

	[BurstCompile]
	public struct SetID_calculate : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

		public Quaternion Rotation;
		public float Size_X;
		public float Size_Y;
		public float Size_Z;
		public float Size_X_half;
		public float Size_Y_half;
		public float Size_Z_half;
		public float RadiusCircle_X;
		public float RadiusCircle_Y;
		public float RadiusCircle_Z;
		public float RadiusCircle_X2;
		public float RadiusCircle_Y2;
		public float RadiusCircle_Z2;
		public float RadiusSphere;
		public float StartPos_X;
		public float StartPos_Y;
		public float StartPos_Z;


		public int TotalVoxelCount;
		public float VoxelSize;		
		public byte Depth;

		public NativeArray<NativeVoxelModificationData_Inner> output;

		public float r;
		public float r2;
		public int TargetID;
        public float RootSize;

        public void Init(VoxelGenerator engine, int totalcount, int depth, int dimension = 0)
		{
			data = engine.Data[dimension];			
			Depth = (byte)depth;
			TotalVoxelCount = totalcount;						
		}

		public void Execute(int index)
		{
			float z = index % Size_Z;
			float y = (index / Size_Z) % Size_Y;
			float x = index / (Size_Y * Size_Z);

			int PeekedID = 0;




			

			float X_Center = x - Size_X_half;
			float Y_Center = y - Size_Y_half;
			float Z_Center = z - Size_Z_half;

			float X2 = X_Center * X_Center;
			float Y2 = Y_Center * Y_Center;
			float Z2 = Z_Center * Z_Center;

			float Dist_XY = X2 + Y2;
			float Dist_YZ = Y2 + Z2;
			float Dist_ZX = Z2 + X2;
			float Dist_All = Dist_XY + Z2;

			if (Dist_All > RadiusSphere || Dist_XY > RadiusCircle_Z2 || Dist_YZ > RadiusCircle_X2 || Dist_ZX > RadiusCircle_Y2)
			{
				PeekedID = int.MinValue;
			}
			else
			{
				PeekedID = TargetID;
			}	

			Vector3 rotation = Rotation * new Vector3(X_Center, Y_Center, Z_Center);

			NativeVoxelModificationData_Inner result;
			result.X = VoxelGenerator.ConvertLocalToInner( (rotation.x+ Size_X_half) * VoxelSize + StartPos_X, RootSize);
			result.Y = VoxelGenerator.ConvertLocalToInner((rotation.y+ Size_Y_half) * VoxelSize + StartPos_Y, RootSize);
			result.Z = VoxelGenerator.ConvertLocalToInner((rotation.z+ Size_Z_half) * VoxelSize + StartPos_Z, RootSize);
			result.ID = PeekedID;
			result.Depth = Depth;

			output[index] = result;


		}
	}
}
