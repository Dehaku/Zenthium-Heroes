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
	public class VM_PostProcess_Threshold : VM_PostProcess
	{
		public int Threshold = 0;
		public int LeftID = 255;
		public int RightID = 255;

		public bool DiscardLeft;
		public bool DiscardRight;


		public override void ApplyPostprocess(FNativeList<NativeVoxelModificationData_Inner> modifierData, VoxelGenerator generator, VoxelModifier_V2 modifier)
		{
			ThresholdJob job = new ThresholdJob();	
			job.modifierData = modifierData;
			job.data = generator.Data[modifier.TargetDimension];	
			job.mode = modifier.Mode;
			job.Threshold = Threshold;
			job.LeftID = LeftID;
			job.RightID = RightID;

			if (DiscardLeft) job.LeftID = int.MinValue;
			if (DiscardRight) job.RightID = int.MinValue;

			job.Schedule(modifierData.Length, modifierData.Length / SystemInfo.processorCount).Complete();
		}	
	}

	[BurstCompile]
	public struct ThresholdJob : IJobParallelFor
	{
		public int Threshold;
		public int LeftID;
		public int RightID;
		public NativeVoxelTree data;
	

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> modifierData;
		internal VoxelModifierMode mode;

		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner modifier = modifierData[index];

			int modifierID = Mathf.Abs(modifier.ID);
			if (modifierID > Threshold)
			{

				modifierID = RightID;
				
			}
			else
			{
				modifierID = LeftID;
			}

			if(modifier.ID < 0)
			{
				modifierID = -modifierID;
			}

			modifier.ID = modifierID;
			modifierData[index] = modifier;
		}
	}

	


}
