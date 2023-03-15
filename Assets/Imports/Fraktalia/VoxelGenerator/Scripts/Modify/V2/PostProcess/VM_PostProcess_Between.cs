using Fraktalia.Core.Collections;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify
{
	public class VM_PostProcess_Between : VM_PostProcess
	{
		public int MinimumID = 0;
		public int MaximumID = 255;
		
		public override void ApplyPostprocess(FNativeList<NativeVoxelModificationData_Inner> modifierData, VoxelGenerator generator, VoxelModifier_V2 modifier)
		{
			BetweenJob job = new BetweenJob();	
			job.modifierData = modifierData;
			job.data = generator.Data[modifier.TargetDimension];	
			job.mode = modifier.Mode;
			job.MinimumID = MinimumID;
			job.MaximumID = MaximumID;
			job.Schedule(modifierData.Length, modifierData.Length / SystemInfo.processorCount).Complete();
		}	
	}

	[BurstCompile]
	public struct BetweenJob : IJobParallelFor
	{
		public int MinimumID;
		public int MaximumID;
		public NativeVoxelTree data;
	

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> modifierData;
		internal VoxelModifierMode mode;

		public void Execute(int index)
		{
			
			NativeVoxelModificationData_Inner modifier = modifierData[index];

			if (modifier.ID != 0)
			{
				int Value = data._PeekVoxelId_InnerCoordinate(modifier.X, modifier.Y, modifier.Z, 20, 0, 128);

				if (mode == VoxelModifierMode.Subtractive)
				{
					int modifierID = modifier.ID;
					int difference = Value + modifierID;
					if (difference < MinimumID)
					{
						modifier.ID = modifierID - difference + MinimumID;
						//modifier.ID = Mathf.Min(0, modifier.ID);

					}
				}

				if (mode == VoxelModifierMode.Additive)
				{
					int modifierID = modifier.ID;
					int difference = Value + modifierID;
					if (difference >= MaximumID)
					{
						modifier.ID = modifierID - difference + MaximumID;
						//modifier.ID = Mathf.Max(0, modifier.ID);

					}
				}
			}



			modifierData[index] = modifier;
		}
	}

	


}
