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
	public struct DataVisualisation_NodesChanged : IJob
	{
		public int MeshDepth;
		public int NumCores;
		public int coreID;
		public NativeVoxelTree Data;

		[ReadOnly]
		public FNativeList<NativeVoxelNode> voxels;

		[WriteOnly]
		public FNativeList<NativeVoxelNode> output;

		public void Execute()
		{

			int step = voxels.Length / NumCores;
			int count = Mathf.Min(step * (coreID + 1), voxels.Length);
			int start = step * coreID;
			output.Clear();

			for (int index = start; index < count; index++)
			{
				NativeVoxelNode node = voxels[index];

				NativeVoxelNode current;
				node.Refresh(out current);
				while (current.IsValid())
				{
					if (current.Depth % MeshDepth == 0)
					{
						NativeVoxelNode leftneighbour = current._LeftNeighbor(ref Data, coreID);
						if (leftneighbour.IsValid())
							NodeDestroyed(ref leftneighbour);
						NativeVoxelNode rightneighbour = current._RightNeighbor(ref Data, coreID);
						if (rightneighbour.IsValid())
							NodeDestroyed(ref rightneighbour);
						NativeVoxelNode downneighbour = current._DownNeighbor(ref Data, coreID);
						if (downneighbour.IsValid())
							NodeDestroyed(ref downneighbour);
						NativeVoxelNode upneighbour = current._UpNeighbor(ref Data, coreID);
						if (upneighbour.IsValid())
							NodeDestroyed(ref upneighbour);
						NativeVoxelNode frontneighbour = current._FrontNeighbor(ref Data, coreID);
						if (frontneighbour.IsValid())
							NodeDestroyed(ref frontneighbour);
						NativeVoxelNode backneighbour = current._BackNeighbor(ref Data, coreID);
						if (backneighbour.IsValid())
							NodeDestroyed(ref backneighbour);

						NodeDestroyed(ref current);
					}
					current.GetParent(out current);
				}
			}
		}

		public void NodeDestroyed(ref NativeVoxelNode node)
		{
			if (node.Depth % MeshDepth == 0)
			{
				output.Add(node);
			}
		}

		[BurstDiscard]
		public void CleanUp()
		{
			if (output.IsCreated) output.Dispose();
		}
	}
}
