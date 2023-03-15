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
	public class ProceduralUV_Oscillation : ProceduralUV
	{
		[Header("Fractal Settings")]
		public int Octaves;
		public float Lacunarity;
		public float Gain;

		[Header("Sinus/Oscillation Settings")]
		public float SinusPower;
		public float SinusFrequenz;
		public Vector3 Offset;

		public int StartValue = 0;


		ProceduralUV_Oscillation_Calculate calculate;

		protected override void Algorithm(ref NativeArray<Vector3> positionData, ref NativeArray<Vector2> uvData)
		{
			calculate.positionData = positionData;
			calculate.uvData = uvData;
			calculate.ApplyMode = ApplyFunctionPointer;
			calculate.StartValue = StartValue;
			calculate.PositionOffset = Offset;
			calculate.SinusPower = SinusPower;
			calculate.SinusFrequenz = SinusFrequenz/100;
			calculate.Octaves = Octaves;
			calculate.Lacunarity = Lacunarity;
			calculate.Gain = Gain;
			calculate.Schedule(positionData.Length, 10000).Complete();
		}
	}

	
	
	[BurstCompile]
	public struct ProceduralUV_Oscillation_Calculate : IJobParallelFor
	{
		public float SinusPower;
		public float SinusFrequenz;

		public int Octaves;
		public float Lacunarity;
		public float Gain;

		public byte Depth;
		public float VoxelSize;
		public int Width;
		public int Blocks;
		public Vector3 PositionOffset;

		public int StartValue;

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

			float value = StartValue;
			float frequency = SinusFrequenz;
			float amplitude = SinusPower;
			for (int i = 0; i < Octaves; i++)
			{
				value += Mathf.Sin(Worldpos_X * frequency) * Mathf.Sin(Worldpos_Z * frequency) * amplitude;
				frequency *= Lacunarity;
				amplitude *= Gain;
			}

			Vector2 data = uvData[index];
			data.x = ApplyMode.Invoke(data.x, value);
			uvData[index] = data;
		}	
	}
}
