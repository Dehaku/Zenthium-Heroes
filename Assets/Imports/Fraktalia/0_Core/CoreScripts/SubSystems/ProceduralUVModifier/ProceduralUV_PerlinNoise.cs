using Fraktalia.Core.Math;
using Fraktalia.Utility.NativeNoise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.Core.ProceduralUVCreator
{
	public class ProceduralUV_PerlinNoise : ProceduralUV
	{
		public enum NoiseMode
		{
			Noise1D,
			Noise2D,
			Noise3D
		}


		[Header("Fractal Settings")]
		public int Octaves;
		public float Lacunarity;
		public float Gain;

		[Header("Perlin Noise Settings")]
		public float Frequency;
		public float Amplitude;
		public Vector3 Offset;
		public NoiseMode Mode;

		public int StartValue = 0;

		ProceduralUV_PerlinNoise_Calculate2D calculate_2D;
		ProceduralUV_PerlinNoise_Calculate3D calculate_3D;

		protected override void Algorithm(ref NativeArray<Vector3> positionData, ref NativeArray<Vector2> uvData)
		{					
			calculate_2D.positionData = positionData;
			calculate_3D.positionData = positionData;
			calculate_2D.uvData = uvData;
			calculate_3D.uvData = uvData;

			calculate_2D.ApplyMode = ApplyFunctionPointer;
			calculate_3D.ApplyMode = ApplyFunctionPointer;
			
			switch (Mode)
			{
				case NoiseMode.Noise1D:
					break;
				case NoiseMode.Noise2D:
					calculate_2D.PositionOffset = Offset;
					calculate_2D.Frequency = Frequency/100;
					calculate_2D.Amplitude = Amplitude;
					calculate_2D.StartValue = StartValue;

					calculate_2D.Octaves = Octaves;
					calculate_2D.Lacunarity = Lacunarity;
					calculate_2D.Gain = Gain;
					calculate_2D.PermutationTable = Generator.PermutationTable;
					calculate_2D.Schedule(positionData.Length, 10000).Complete();
					break;
				case NoiseMode.Noise3D:
					calculate_3D.PositionOffset = Offset;
					calculate_3D.Frequency = Frequency/100;
					calculate_3D.Amplitude = Amplitude;
					calculate_3D.StartValue = StartValue;

					calculate_3D.Octaves = Octaves;
					calculate_3D.Lacunarity = Lacunarity;
					calculate_3D.Gain = Gain;
					calculate_3D.PermutationTable = Generator.PermutationTable;
					calculate_3D.Schedule(positionData.Length, 10000).Complete();
					break;
				default:
					break;
			}

			

		}
	}

	
	
	[BurstCompile]
	public struct ProceduralUV_PerlinNoise_Calculate2D : IJobParallelFor
	{
		[ReadOnly]
		public PermutationTable_Native PermutationTable;

		public int Octaves;
		public float Lacunarity;
		public float Gain;

		public float   Frequency;
		public float   Amplitude;
		public int StartValue;
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


			Vector2 data = uvData[index];

			float height = StartValue;
			float frequency = Frequency;
			float amplitude = Amplitude;
			for (int i = 0; i < Octaves; i++)
			{
				height += PerlinNoise_Native.Sample2D(Worldpos_X, Worldpos_Z, frequency, amplitude, ref PermutationTable);
				frequency *= Lacunarity;
				amplitude *= Gain;

			}			

			data.x = ApplyMode.Invoke(data.x, height);
			uvData[index] = data;
		}
	}
	[BurstCompile]
	public struct ProceduralUV_PerlinNoise_Calculate3D : IJobParallelFor
	{
		[ReadOnly]
		public PermutationTable_Native PermutationTable;

		public int Octaves;
		public float Lacunarity;
		public float Gain;

		public float Frequency;
		public float Amplitude;

		public int StartValue;

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


			Vector2 data = uvData[index];

			float height = StartValue;

			float frequency = Frequency;
			float amplitude = Amplitude;
			for (int i = 0; i < Octaves; i++)
			{
				height += PerlinNoise_Native.Sample3D(Worldpos_X, Worldpos_Y, Worldpos_Z, frequency, amplitude, ref PermutationTable);
				frequency *= Lacunarity;
				amplitude *= Gain;

			}			
			
			data.x = ApplyMode.Invoke(data.x, height);
			uvData[index] = data;

		}
	}
}
