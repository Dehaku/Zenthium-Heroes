using Fraktalia.Core.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Fraktalia.VoxelGen
{
	public unsafe struct NativeVoxelReservoir
	{
		public int NodeChildrenCount;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<IntPtr> NodePool;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<int> Information;

		public int GarbageSize {
			get
			{
				if (!Information.IsCreated) return 0;
				return Information[0];
			}
			private set
			{
				Information[0] = value;
			}
		}
		public void Initialize(VoxelGenerator generator)
		{
			NodePool = new FNativeList<IntPtr>(Allocator.Persistent);
			Information = new NativeArray<int>(2, Allocator.Persistent);

			NodeChildrenCount = generator.SubdivisionPower * generator.SubdivisionPower * generator.SubdivisionPower;
			GarbageSize = 0;
			allocateNodes(1000);
		}

		public IntPtr ObtainNodeAddress()
		{
			if (GarbageSize == 0)
			{
				allocateNodes(100);
			}
			GarbageSize--;
			IntPtr address = NodePool[GarbageSize];
			
			return address;
		}

		public void AddGarbage(IntPtr address)
		{
			if (GarbageSize >= NodePool.Length)
			{
				NodePool.Add(address);
			}
			else
			{
				NodePool[GarbageSize] = address;
			}


			GarbageSize++;
		}

		public void CleanUp()
		{
			if (NodePool.IsCreated)
			{
				FreeMemory();
				NodePool.Dispose();
				Information.Dispose();
			}
		}

		private void allocateNodes(int amount)
		{
			int garbagesize = GarbageSize;
			for (int i = 0; i < amount; i++)
			{
				int length = NodeChildrenCount;
				int elementSize = UnsafeUtility.SizeOf<NativeVoxelNode>();
				
				IntPtr address = (IntPtr)UnsafeUtility.Malloc(length * elementSize, UnsafeUtility.AlignOf<NativeVoxelNode>(), Allocator.Persistent);
				if (garbagesize >= NodePool.Length)
				{
					NodePool.Add(address);
				}
				else
				{
					NodePool[garbagesize] = address;
				}
				garbagesize++;				
			}
			GarbageSize = garbagesize;
		}

		public void FreeMemory()
		{
			for (int i = 0; i < GarbageSize; i++)
			{			
				IntPtr pointer = NodePool[i];

#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (pointer.ToPointer() == null || pointer == IntPtr.Zero)
				{
					throw new NullReferenceException();
				}
#endif

				UnsafeUtility.Free(pointer.ToPointer(), Allocator.Persistent);
				
			}

			GarbageSize = 0;
			NodePool.Clear();
		}

		/// <summary>
		/// Clears Memory and sets capacity of NodePool to minimum.
		/// </summary>
		public void ResetGarbage()
		{
			int garbagecount = GarbageSize;
			if(garbagecount > VoxelGenerator.MINCAPACITY)
			{
				FreeMemory();
				NodePool.Capacity = VoxelGenerator.MINCAPACITY;
			}
		}	
	}
}
