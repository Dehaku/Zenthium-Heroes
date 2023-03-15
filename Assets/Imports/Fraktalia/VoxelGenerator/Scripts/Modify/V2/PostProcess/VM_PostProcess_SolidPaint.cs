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
	public class VM_PostProcess_SolidPaint : VM_PostProcess
	{
		public int Threshold = 0;
		public int LeftID = 255;
		public bool DiscardLeft;
	
		public override void ApplyPostprocess(FNativeList<NativeVoxelModificationData_Inner> modifierData, VoxelGenerator generator, VoxelModifier_V2 modifier)
		{
			SolidPaintJob job = new SolidPaintJob();	
			job.modifierData = modifierData;
			job.data = generator.Data[modifier.TargetDimension];	
			job.mode = modifier.Mode;
			job.Threshold = Threshold;
			job.LeftID = LeftID;
			

			if (DiscardLeft) job.LeftID = int.MinValue;
			
			job.Schedule(modifierData.Length, modifierData.Length / SystemInfo.processorCount).Complete();
		}	
	}

	[BurstCompile]
	public struct SolidPaintJob : IJobParallelFor
	{
		public int Threshold;
		public int LeftID;
		public NativeVoxelTree data;
	

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> modifierData;
		internal VoxelModifierMode mode;

		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner modifier = modifierData[index];

			if (mode == VoxelModifierMode.Set)
			{
				if (modifier.ID == 0)
				{
					modifier.ID = int.MinValue;
				}
			}


			int Value = data._PeekVoxelId_InnerCoordinate(modifier.X, modifier.Y, modifier.Z, 20, 0, 128);
			if (Value < Threshold)
			{
				modifier.ID = LeftID;
			}
			else
			{
				if (mode == VoxelModifierMode.Subtractive)
				{
					int modifierID = modifier.ID;
					int difference = Value + modifierID;
					if (difference < Threshold)
					{
						modifier.ID = modifierID - difference + Threshold;
						modifier.ID = Mathf.Min(0, modifier.ID);
					}
				}
			}




			modifierData[index] = modifier;

		}
	}

	


}
