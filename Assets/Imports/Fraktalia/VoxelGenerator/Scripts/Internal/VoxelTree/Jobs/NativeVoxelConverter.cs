using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using Fraktalia.Utility;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen
{
	[BurstCompile]
	public struct NativeVoxelConverter : IJob
	{
		public NativeVoxelTree data;
		public FNativeList<NativeVoxelModificationData> output;
		public FNativeList<NativeVoxelNode> leafvoxels;

		public void Init()
		{
			if (!leafvoxels.IsCreated)
				leafvoxels = new FNativeList<NativeVoxelNode>(Allocator.Persistent);
			if (!output.IsCreated)
				output = new FNativeList<NativeVoxelModificationData>(10000, Allocator.Persistent);
		}

		public void Execute()
		{
			leafvoxels.Clear();
			output.Clear();

			NativeVoxelNode root;
			NativeVoxelNode.PointerToNode(data._ROOT, out root);

			data._GetAllLeafVoxel(root, ref leafvoxels);
			for (int i = 0; i < leafvoxels.Length; i++)
			{

				NativeVoxelModificationData nodedata;
				NativeVoxelNode node = leafvoxels[i];

				nodedata.X = NativeVoxelTree.ConvertInnerToLocal(node.X, data.RootSize);
				nodedata.Y = NativeVoxelTree.ConvertInnerToLocal(node.Y, data.RootSize);
				nodedata.Z = NativeVoxelTree.ConvertInnerToLocal(node.Z, data.RootSize);

				nodedata.Depth = node.Depth;
				nodedata.ID = node._voxelID;
				output.Add(nodedata);
			}

		}

		public void CleanUp()
		{	
			if (leafvoxels.IsCreated) leafvoxels.Dispose();
			if (output.IsCreated) output.Dispose();
		}

	}

}
