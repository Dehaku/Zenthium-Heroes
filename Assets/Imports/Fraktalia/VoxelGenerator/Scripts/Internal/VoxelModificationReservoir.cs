using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Fraktalia.VoxelGen
{
	public class VoxelModificationReservoir
	{
		private VoxelGenerator generator;
		private NativeArray<NativeVoxelModificationData_Inner>[] DepthContainers;

		public VoxelModificationReservoir(VoxelGenerator generator)
		{
			this.generator = generator;
			DepthContainers = new NativeArray<NativeVoxelModificationData_Inner>[NativeVoxelTree.MaxDepth];
		}

		public NativeArray<NativeVoxelModificationData_Inner> GetDataArray(int targetdepth)
		{
			if(!DepthContainers[targetdepth].IsCreated)
			{
				int width = generator.GetBlockWidth(targetdepth);
				int blocks = width * width * width;
				DepthContainers[targetdepth] = new NativeArray<NativeVoxelModificationData_Inner>(blocks, Allocator.Persistent);
			}
			return DepthContainers[targetdepth];
			
			
		}

		public void CleanData()
		{
			for (int i = 0; i < DepthContainers.Length; i++)
			{
				if(DepthContainers[i].IsCreated)
				{
					DepthContainers[i].Dispose();
				}
			}
		}
	}
}
