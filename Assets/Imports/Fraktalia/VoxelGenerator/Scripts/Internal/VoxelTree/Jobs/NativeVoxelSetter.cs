using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using System.Runtime.InteropServices;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen
{
	

	[BurstCompile]
	public struct UpdateVoxelTree_Job : IJob
	{
		public NativeVoxelTree data;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData> changedata_additive;
		public FNativeList<NativeVoxelModificationData> changedata_additive_confirmed;
		public FNativeList<NativeVoxelNode> result_final;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData> changedata_set;
		public FNativeList<NativeVoxelModificationData> changedata_set_confirmed;
		public FNativeList<NativeVoxelNode> result_set;


		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> changedata_additive_inner;
		public FNativeList<NativeVoxelModificationData_Inner> changedata_additive_confirmed_inner;
		public FNativeList<NativeVoxelNode> result_final_inner;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> changedata_set_inner;
		public FNativeList<NativeVoxelModificationData_Inner> changedata_set_confirmed_inner;
		public FNativeList<NativeVoxelNode> result_set_inner;



		public byte MemoryOptimized;

		[BurstDiscard]
		public void Init(VoxelGenerator generator)
		{
			MemoryOptimized = (byte)(generator.MemoryOptimized ? 1 : 0);

			changedata_additive = new FNativeList<NativeVoxelModificationData>(Allocator.Persistent);
			changedata_additive_confirmed = new FNativeList<NativeVoxelModificationData>(Allocator.Persistent);
			result_final = new FNativeList<NativeVoxelNode>(Allocator.Persistent);

			changedata_set = new FNativeList<NativeVoxelModificationData>(Allocator.Persistent);
			result_set = new FNativeList<NativeVoxelNode>(Allocator.Persistent);
			changedata_set_confirmed = new FNativeList<NativeVoxelModificationData>(Allocator.Persistent);

			changedata_additive_inner = new FNativeList<NativeVoxelModificationData_Inner>(Allocator.Persistent);
			changedata_additive_confirmed_inner = new FNativeList<NativeVoxelModificationData_Inner>(Allocator.Persistent);
			result_final_inner = new FNativeList<NativeVoxelNode>(Allocator.Persistent);

			changedata_set_inner = new FNativeList<NativeVoxelModificationData_Inner>(Allocator.Persistent);
			result_set_inner = new FNativeList<NativeVoxelNode>(Allocator.Persistent);
			changedata_set_confirmed_inner = new FNativeList<NativeVoxelModificationData_Inner>(Allocator.Persistent);
		}

		public void Prepare()
		{
			changedata_additive_confirmed.AddRange(changedata_additive);
			changedata_additive.Clear();
			
			changedata_set_confirmed.AddRange(changedata_set);
			changedata_set.Clear();

			changedata_additive_confirmed_inner.AddRange(changedata_additive_inner);
			changedata_additive_inner.Clear();

			changedata_set_confirmed_inner.AddRange(changedata_set_inner);
			changedata_set_inner.Clear();



			if (MemoryOptimized == 1)
			{
				changedata_set.Capacity = VoxelGenerator.MINCAPACITY;
				changedata_additive.Capacity = VoxelGenerator.MINCAPACITY;
				changedata_set_inner.Capacity = VoxelGenerator.MINCAPACITY;
				changedata_additive_inner.Capacity = VoxelGenerator.MINCAPACITY;
			}
		}

		public void Execute()
		{
			ExecuteAdditive();
			ExecuteAdditive_Inner();
			ExecuteSet();
			ExecuteSet_Inner();

			result_final.AddRange(result_set);
			result_final.AddRange(result_final_inner);
			result_final.AddRange(result_set_inner);
		}

		public void ExecuteAdditive()
		{
			int TotalVoxels = changedata_additive_confirmed.Length;
			NativeVoxelNode voxel;

			for (int i = 0; i < TotalVoxels; i++)
			{
				NativeVoxelModificationData change = changedata_additive_confirmed[i];

				if (change.ID == int.MinValue || change.ID == 0) continue;

				if (change.X < 0 || change.X >= data.RootSize ||
					change.Y < 0 || change.Y >= data.RootSize ||
					change.Z < 0 || change.Z >= data.RootSize) continue;

				data._GetVoxel(change.X, change.Y, change.Z, change.Depth, out voxel);
				voxel.SetVoxelAdditive(ref data, change.ID);
				result_final.Add(voxel);
			}

			int resultcount = result_final.Length;
			for (int i = 0; i < resultcount; i++)
			{
				result_final[i].Refresh(out voxel);
				if (!voxel.IsDestroyed())
				{
					data.UpwardMerge(voxel, out voxel);
				}
			}

			changedata_additive_confirmed.Clear();
			if (MemoryOptimized == 1)
			{
				changedata_additive_confirmed.Capacity = VoxelGenerator.MINCAPACITY;
			}
		}

		public void ExecuteAdditive_Inner()
		{
			int TotalVoxels = changedata_additive_confirmed_inner.Length;
			NativeVoxelNode voxel;

			for (int i = 0; i < TotalVoxels; i++)
			{
				NativeVoxelModificationData_Inner change = changedata_additive_confirmed_inner[i];

				if (change.ID == int.MinValue || change.ID == 0) continue;

				if (change.X < 0 || change.X >= NativeVoxelTree.INNERWIDTH ||
					change.Y < 0 || change.Y >= NativeVoxelTree.INNERWIDTH ||
					change.Z < 0 || change.Z >= NativeVoxelTree.INNERWIDTH) continue;

				data._GetVoxel_InnerCoordinate(change.X, change.Y, change.Z, change.Depth, out voxel);
				voxel.SetVoxelAdditive(ref data, change.ID);
				result_final_inner.Add(voxel);
			}

			int resultcount = result_final_inner.Length;
			for (int i = 0; i < resultcount; i++)
			{
				result_final_inner[i].Refresh(out voxel);
				if (!voxel.IsDestroyed())
				{
					data.UpwardMerge(voxel, out voxel);
				}
			}

			changedata_additive_confirmed_inner.Clear();
			if (MemoryOptimized == 1)
			{
				changedata_additive_confirmed_inner.Capacity = VoxelGenerator.MINCAPACITY;
			}
		}


		public void ExecuteSet()
		{
			int TotalVoxels = changedata_set_confirmed.Length;
			NativeVoxelNode voxel;

			for (int i = 0; i < TotalVoxels; i++)
			{
				NativeVoxelModificationData change = changedata_set_confirmed[i];

				if (change.ID == int.MinValue) continue;

				if (change.X < 0 || change.X >= data.RootSize ||
					change.Y < 0 || change.Y >= data.RootSize ||
					change.Z < 0 || change.Z >= data.RootSize) continue;

				data._GetVoxel(change.X, change.Y, change.Z, change.Depth, out voxel);
				voxel.SetVoxel(ref data, (byte)change.ID);
				result_set.Add(voxel);
			}

			int resultcount = result_set.Length;
			for (int i = 0; i < resultcount; i++)
			{
				result_set[i].Refresh(out voxel);
				if (!voxel.IsDestroyed() && voxel.IsValid())
				{
					data.UpwardMerge(voxel, out voxel);
				}
			}

			changedata_set_confirmed.Clear();
			if (MemoryOptimized == 1)
			{
				changedata_set_confirmed.Capacity = VoxelGenerator.MINCAPACITY;
			}
		}

		public void ExecuteSet_Inner()
		{
			int TotalVoxels = changedata_set_confirmed_inner.Length;
			NativeVoxelNode voxel;

			for (int i = 0; i < TotalVoxels; i++)
			{
				NativeVoxelModificationData_Inner change = changedata_set_confirmed_inner[i];

				if (change.ID == int.MinValue) continue;

				if (change.X < 0 || change.X >= NativeVoxelTree.INNERWIDTH ||
					change.Y < 0 || change.Y >= NativeVoxelTree.INNERWIDTH ||
					change.Z < 0 || change.Z >= NativeVoxelTree.INNERWIDTH) continue;

				data._GetVoxel_InnerCoordinate(change.X, change.Y, change.Z, change.Depth, out voxel);
				voxel.SetVoxel(ref data, (byte)change.ID);
				result_set_inner.Add(voxel);
			}

			int resultcount = result_set_inner.Length;
			for (int i = 0; i < resultcount; i++)
			{
				result_set_inner[i].Refresh(out voxel);
				if (!voxel.IsDestroyed() && voxel.IsValid())
				{
					data.UpwardMerge(voxel, out voxel);
				}
			}

			changedata_set_confirmed_inner.Clear();
			if (MemoryOptimized == 1)
			{
				changedata_set_confirmed_inner.Capacity = VoxelGenerator.MINCAPACITY;
			}
		}

		[BurstDiscard]
		public void CleanUp()
		{
			if (result_final.IsCreated) result_final.Dispose();
			if (changedata_additive.IsCreated) changedata_additive.Dispose();
			if (changedata_additive_confirmed.IsCreated) changedata_additive_confirmed.Dispose();

			if (result_set.IsCreated) result_set.Dispose();
			if (changedata_set.IsCreated) changedata_set.Dispose();
			if (changedata_set_confirmed.IsCreated) changedata_set_confirmed.Dispose();

			if (result_final_inner.IsCreated) result_final_inner.Dispose();
			if (changedata_additive_inner.IsCreated) changedata_additive_inner.Dispose();
			if (changedata_additive_confirmed_inner.IsCreated) changedata_additive_confirmed_inner.Dispose();

			if (result_set_inner.IsCreated) result_set_inner.Dispose();
			if (changedata_set_inner.IsCreated) changedata_set_inner.Dispose();
			if (changedata_set_confirmed_inner.IsCreated) changedata_set_confirmed_inner.Dispose();
		}

		public bool HasWork()
		{
			return changedata_additive.Length > 0 || changedata_set.Length > 0 || changedata_additive_inner.Length > 0 || changedata_set_inner.Length > 0;
		}


		internal void SetCapacity(int count)
		{
			changedata_additive.Capacity = count;
			changedata_additive_confirmed.Capacity = count;
			result_final.Capacity = count;

			changedata_set.Capacity = count;
			changedata_set_confirmed.Capacity = count;
			result_set.Capacity = count;

			changedata_additive_inner.Capacity = count;
			changedata_additive_confirmed_inner.Capacity = count;
			result_final_inner.Capacity = count;

			changedata_set_inner.Capacity = count;
			changedata_set_confirmed_inner.Capacity = count;
			result_set_inner.Capacity = count;
		}
	}


	[BurstCompile]
	public struct NativeVoxelReset_Job : IJob
	{
		public NativeVoxelTree data;
		public int InitialID;

		public void Execute()
		{
			NativeVoxelNode voxel;
			NativeVoxelNode.PointerToNode(data._ROOT, out voxel);
			voxel.SetVoxel(ref data, (byte)InitialID);
		}
	}



	[System.Serializable]
	public struct NativeVoxelModificationData
	{
		public float X;
		public float Y;
		public float Z;
		public int ID;
		public byte Depth;		
	}

	[System.Serializable]
	public struct NativeVoxelModificationData_Inner
	{
		public int X;
		public int Y;
		public int Z;

		public int ID;
		public byte Depth;
	}
}
