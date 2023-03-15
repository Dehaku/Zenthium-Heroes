using Fraktalia.Core.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.Core.ProceduralUVCreator
{
	public class ProceduralUV_Gradient : ProceduralUV
	{
		public Vector3 Gradient_Min;
		public Vector3 Gradient_Max;
		public float GradientPower;
		public Vector3 Offset;

		ProceduralUV_Gradient_Calculate calculate;
			
		protected override void Algorithm(ref NativeArray<Vector3> positionData, ref NativeArray<Vector2> uvData)
		{
			calculate.positionData = positionData;
			calculate.uvData = uvData;
		
			calculate.PositionOffset = Offset;
			calculate.ApplyMode = ApplyFunctionPointer;
			
			calculate.GradientPower = GradientPower;
			calculate.Gradient_Min = Gradient_Min;
			calculate.Gradient_Max = Gradient_Max;

			calculate.Schedule(positionData.Length, 10000).Complete();
	
		}
	}

	
	
	[BurstCompile]
	public struct ProceduralUV_Gradient_Calculate : IJobParallelFor
	{
		public Vector3 Gradient_Min;
		public Vector3 Gradient_Max;
		public float GradientPower;


		public byte Depth;
		public float VoxelSize;
		public int Width;
		public int Blocks;
		public Vector3 PositionOffset;


		[ReadOnly]
		public NativeArray<Vector3> positionData;
		public NativeArray<Vector2> uvData;
		public FunctionPointer<DataApplyModeDelegate> ApplyMode;

		public void Execute(int index)
		{

			Vector3 vertexposition = positionData[index];
			float Worldpos_X = PositionOffset.x + vertexposition.x;
			float Worldpos_Y = PositionOffset.y + vertexposition.y;
			float Worldpos_Z = PositionOffset.z + vertexposition.z;

			float gradient_X = Mathf.InverseLerp(Gradient_Min.x, Gradient_Max.x, Worldpos_X);
			float gradient_Y = Mathf.InverseLerp(Gradient_Min.y, Gradient_Max.y, Worldpos_Y);
			float gradient_Z = Mathf.InverseLerp(Gradient_Min.z, Gradient_Max.z, Worldpos_Z);

			float value = (gradient_X + gradient_Y + gradient_Z) * GradientPower;

			Vector2 data = uvData[index];

			data.x = ApplyMode.Invoke(data.x, value);
			uvData[index] = data;

		}
	}
}
