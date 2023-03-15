using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;

namespace Fraktalia.VoxelGen.Modify.Filter
{
	public class BaseVoxelFilter
	{
		public NativeArray<NativeVoxelModificationData_Inner> output;

		protected int Modifications;
			
		protected float Size_X;
		protected float Size_Y;
		protected float Size_Z;

		protected float RadiusCircle_X;
		protected float RadiusCircle_Y;
		protected float RadiusCircle_Z;
		protected float RadiusSphere;


		public void CalculateFilter(VoxelGenerator engine, Vector3 center,Quaternion rotation, Vector3 size, Vector4 circleradius, int depth)
		{
			if (!engine.IsInitialized) return;
			if (depth >= NativeVoxelTree.MaxDepth) return;

			float Nodesize = engine.Data[0].SizeTable[depth];

			Vector3 voxelwidth = size/Nodesize;
		
			Size_X = (int)voxelwidth.x;
			Size_Y = (int)voxelwidth.y;
			Size_Z = (int)voxelwidth.z;

			RadiusCircle_X = circleradius.x/ Nodesize;
			RadiusCircle_Y = circleradius.y/ Nodesize;
			RadiusCircle_Z = circleradius.z/ Nodesize;
			RadiusSphere = circleradius.w / Nodesize;

			Modifications = (int)(Size_X * Size_Y * Size_Z);

			if (!output.IsCreated)
			{
				output = new NativeArray<NativeVoxelModificationData_Inner>(Modifications, Allocator.Persistent);
			}

			if (output.Length != Modifications)
			{
				output.Dispose();
				output = new NativeArray<NativeVoxelModificationData_Inner>(Modifications, Allocator.Persistent);
			}


			calculation(engine, center,rotation, depth);


		}

		protected virtual void calculation(VoxelGenerator engine, Vector3 center, Quaternion rotation, int depth)
		{

		}

		public void CleanUp()
		{
			if (output.IsCreated) output.Dispose();
		}
	}
}
