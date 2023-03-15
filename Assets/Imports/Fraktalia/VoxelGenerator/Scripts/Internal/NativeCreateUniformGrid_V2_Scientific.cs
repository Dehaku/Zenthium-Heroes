using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Fraktalia.Core.Math;

namespace Fraktalia.VoxelGen.Visualisation
{
	[BurstCompile]
	public unsafe struct NativeCreateUniformGrid_V2_Scientific : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

		public Vector3Int positionoffset;
		public int voxelSizeBitPosition;

		public int MaxBlocks;
		public int Width;

		public int Shrink;

		[ReadOnly]
		public NativeArray<float> Histogramm;


		[WriteOnly]
		public NativeArray<float> UniformGridResult;
		public Bounds VisibleBoundary;

		public void Execute(int index)
		{
			Vector3Int position = MathUtilities.Convert1DTo3D(index, Width, Width, Width);

			int fx = positionoffset.x + ((position.x - 1) << voxelSizeBitPosition);
			int fy = positionoffset.y + ((position.y - 1) << voxelSizeBitPosition);
			int fz = positionoffset.z + ((position.z - 1) << voxelSizeBitPosition);
			int rawvalue = data._PeekVoxelId_InnerCoordinate(fx, fy, fz, 10, Shrink);

			float result = 0;
			if (!(fx < VisibleBoundary.min.x * NativeVoxelTree.INNERWIDTH || fy < VisibleBoundary.min.y * NativeVoxelTree.INNERWIDTH || fz < VisibleBoundary.min.z * NativeVoxelTree.INNERWIDTH || fx > VisibleBoundary.max.x * NativeVoxelTree.INNERWIDTH || fy > VisibleBoundary.max.y * NativeVoxelTree.INNERWIDTH || fz > VisibleBoundary.max.z * NativeVoxelTree.INNERWIDTH))
			{
				int histogrammvalue = data._PeekVoxelId_InnerCoordinate(fx, fy, fz, 10, Shrink);
				result = (Histogramm[histogrammvalue]);
				result = Mathf.Clamp(result, 0, 256);
			}
			


			UniformGridResult[index] = result;
		}
	}



}
