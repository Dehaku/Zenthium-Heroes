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
	public class VM_PostProcess_ShapeFill : VM_PostProcess
	{
		public int DensityDimension = 0;
		public int ShapeDefiningDimension=1;
		public bool Inverted;
		
		public override void ApplyPostprocess(FNativeList<NativeVoxelModificationData_Inner> modifierData, VoxelGenerator generator, VoxelModifier_V2 modifier)
		{


			ShapeFillJob job = new ShapeFillJob();
			
			job.modifierData = modifierData;
			job.data = generator.Data[DensityDimension];
			job.shapedata = generator.Data[ShapeDefiningDimension];
			
			job.Schedule(modifierData.Length, modifierData.Length / SystemInfo.processorCount).Complete();
		}	
	}

	[BurstCompile]
	public struct ShapeFillJob : IJobParallelFor
	{

		public NativeVoxelTree data;
		public NativeVoxelTree shapedata;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> modifierData;

		
		

		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner modifier = modifierData[index];
			
			int Value = data._PeekVoxelId_InnerCoordinate(modifier.X, modifier.Y, modifier.Z, 20, 0, 128);
			int ShapeValue = shapedata._PeekVoxelId_InnerCoordinate(modifier.X, modifier.Y, modifier.Z, 20, 0, 128);

			int expectedValue = Value + modifier.ID;
			if (expectedValue > ShapeValue)
			{


				modifier.ID = Mathf.Max(0, ShapeValue - Value); 
			}

			
			modifierData[index] = modifier;
		}
	}

	


}
