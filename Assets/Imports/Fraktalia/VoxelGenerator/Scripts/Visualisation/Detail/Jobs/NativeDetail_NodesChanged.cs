using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	[BurstCompile]
	public struct NativeDetail_NodesChanged : IJobParallelFor
	{
		[ReadOnly]
		public FNativeList<NativeVoxelNode> voxels;


		[NativeDisableParallelForRestriction]
		public NativeArray<byte> hashindexmap;

		[NativeDisableParallelForRestriction]
		public NativeArray<float> SizeTable;

		[NativeDisableParallelForRestriction]
		public int Cell_Subdivision;
		[NativeDisableParallelForRestriction]
		public float RootSize;
		[NativeDisableParallelForRestriction]
		public int NumVoxelMeshes;

		[NativeDisableParallelForRestriction]
		public NativeArray<int> output;

		[BurstDiscard]
		public void Init(Detail visualhull)
		{

			Cell_Subdivision = visualhull.Cell_Subdivision;
			RootSize = visualhull.engine.RootSize;
			NumVoxelMeshes = visualhull.Cell_Subdivision * visualhull.Cell_Subdivision * visualhull.Cell_Subdivision;

			SizeTable = new NativeArray<float>(visualhull.engine.Data[0].SizeTable.Length, Allocator.Persistent);
			for (int i = 0; i < visualhull.engine.Data[0].SizeTable.Length; i++)
			{
				SizeTable[i] = (visualhull.engine.Data[0].SizeTable[i] * RootSize);
			}
			



			hashindexmap = new NativeArray<byte>(Cell_Subdivision * Cell_Subdivision * Cell_Subdivision, Allocator.Persistent);

			output = new NativeArray<int>(Cell_Subdivision * Cell_Subdivision * Cell_Subdivision, Allocator.Persistent);
		}

		public void Execute(int index)
		{
			NativeVoxelNode node = voxels[index];

			float cellSize = RootSize / Cell_Subdivision;
			float nodesize = SizeTable[node.Depth];
			float cellextent = nodesize;

			int cellRanges_minX = (int)((node.X - cellextent) / cellSize);
			int cellRanges_minY = (int)((node.Y - cellextent) / cellSize);
			int cellRanges_minZ = (int)((node.Z - cellextent) / cellSize);

			int cellRanges_maxX = (int)((node.X + cellextent) / cellSize);
			int cellRanges_maxY = (int)((node.Y + cellextent) / cellSize);
			int cellRanges_maxZ = (int)((node.Z + cellextent) / cellSize);

			int lenght = NumVoxelMeshes;
			for (int x = cellRanges_minX; x <= cellRanges_maxX; x++)
			{
				for (int y = cellRanges_minY; y <= cellRanges_maxY; y++)
				{
					for (int z = cellRanges_minZ; z <= cellRanges_maxZ; z++)
					{
						int hashindex = x + Cell_Subdivision * (y + Cell_Subdivision * z);
						if (hashindex >= 0 && hashindex < lenght)
						{
							output[hashindex] = 1;
						}
					}
				}
			}

		}

		[BurstDiscard]
		public void CleanUp()
		{
			if (hashindexmap.IsCreated) hashindexmap.Dispose();
			if (output.IsCreated) output.Dispose();
			if (SizeTable.IsCreated) SizeTable.Dispose();
		}
	}
}
