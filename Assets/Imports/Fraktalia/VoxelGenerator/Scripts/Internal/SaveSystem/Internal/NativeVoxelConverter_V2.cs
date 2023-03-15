using Fraktalia.Core.Collections;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Fraktalia.VoxelGen
{
	[BurstCompile]
	public unsafe struct NativeVoxelConverter_V2 : IJob
	{
		public NativeVoxelTree data;
		public FNativeList<byte> output;
		public FNativeList<NativeVoxelNode> leafvoxels;
		
		public void Init()
		{
			
			if(!output.IsCreated) output = new FNativeList<byte>(Allocator.Persistent);
			if(!leafvoxels.IsCreated) leafvoxels = new FNativeList<NativeVoxelNode>(Allocator.Persistent);
			


		}


		public void Execute()
		{
			leafvoxels.Clear();
			output.Clear();

			NativeVoxelNode root;
			NativeVoxelNode.PointerToNode(data._ROOT, out root);

			data._GetAllLeafVoxel(root, ref leafvoxels);

			NativeVoxelModificationData nodedata;
			NativeVoxelNode node;

			//in byte
			int elementSize = UnsafeUtility.SizeOf<NativeVoxelModificationData>();
			int leafcount = leafvoxels.Length;

			output.Resize(elementSize * leafcount, NativeArrayOptions.UninitializedMemory);
			IntPtr arrayPointer = (IntPtr)output.GetUnsafePtr<byte>();

			for (int i = 0; i < leafcount; i++)
			{
				IntPtr location = IntPtr.Add(arrayPointer, i * elementSize);

				node = leafvoxels[i];

				nodedata.X = NativeVoxelTree.ConvertInnerToLocal(node.X, data.RootSize);
				nodedata.Y = NativeVoxelTree.ConvertInnerToLocal(node.Y, data.RootSize);
				nodedata.Z = NativeVoxelTree.ConvertInnerToLocal(node.Z, data.RootSize);

				nodedata.Depth = node.Depth;
				nodedata.ID = node._voxelID;

				UnsafeUtility.CopyStructureToPtr(ref nodedata, location.ToPointer());			
			}

		}

		public void CleanUp()
		{
			if (output.IsCreated) output.Dispose();
			if (leafvoxels.IsCreated) leafvoxels.Dispose();
		}
	}

}
