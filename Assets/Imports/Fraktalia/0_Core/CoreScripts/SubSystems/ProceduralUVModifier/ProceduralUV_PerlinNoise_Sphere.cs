using Fraktalia.Core.Math;
using Fraktalia.Utility.NativeNoise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace Fraktalia.Core.ProceduralUVCreator
{
	public class ProceduralUV_PerlinNoise_Sphere : ProceduralUV
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
		public Vector3 Center;
		public float Radius;
		public float Falloff;
		
		public int StartValue = 255;

		[Tooltip("Prevents overhangs caused by 3D noise. Usually planets have this set to true. If false, result looks like Covid-19")]
		public bool Normalized = false;
		ProceduralUV_PerlinNoise_Sphere_Calculate2D calculate;

		protected override void Algorithm(ref NativeArray<Vector3> positionData, ref NativeArray<Vector2> uvData)
		{
			calculate.positionData = positionData;
			calculate.uvData = uvData;

			calculate.StartValue = StartValue;
			calculate.Center = Center;
			calculate.Frequency = Frequency;
			calculate.Amplitude = Amplitude;
			calculate.Radius = Radius;
			calculate.Falloff = Falloff;
			calculate.ApplyMode = ApplyFunctionPointer;			
			calculate.Octaves = Octaves;
			calculate.Lacunarity = Lacunarity;
			calculate.Gain = Gain;
			calculate.PermutationTable = Generator.PermutationTable;
			calculate.Normalized = Normalized ? 1 : 0;
			calculate.Schedule(positionData.Length, 10000).Complete();

		}

		private void OnDrawGizmosSelected()
		{
			if(Generator.Container)
			Gizmos.DrawWireSphere(Generator.Container.localToWorldMatrix.MultiplyPoint3x4(Center), Radius);
		}
	}

	
	
	[BurstCompile]
	public struct ProceduralUV_PerlinNoise_Sphere_Calculate2D : IJobParallelFor
	{
		[ReadOnly]
		public PermutationTable_Native PermutationTable;

		public int Octaves;
		public float Lacunarity;
		public float Gain;

		public float   Frequency;
		public float   Amplitude;

		public int StartValue;

		public byte Depth;
		public float VoxelSize;
		public int Width;
		
		public Vector3 Center;
		public float Radius;

		[ReadOnly]
		public NativeArray<Vector3> positionData;
		public NativeArray<Vector2> uvData;
		public FunctionPointer<DataApplyModeDelegate> ApplyMode;

		public float Falloff;

		public int Normalized;
		
		public void Execute(int index)
		{

			Vector3 vertexposition = positionData[index];


			float Worldpos_X = Center.x - vertexposition.x;
			float Worldpos_Y = Center.y - vertexposition.y;
			float Worldpos_Z = Center.z - vertexposition.z;

			float distsqr = Worldpos_X * Worldpos_X + Worldpos_Y * Worldpos_Y + Worldpos_Z * Worldpos_Z;
			float dist = Mathf.Sqrt(distsqr);

			
			if(Normalized == 1)
			{
				float phi = Mathf.Atan2(Worldpos_Y, Worldpos_X);
				float theta = (float)Mathf.Acos(Worldpos_Z / dist);

				Worldpos_X = Radius * Mathf.Sin(theta) * Mathf.Cos(phi);
				Worldpos_Y = Radius * Mathf.Sin(theta) * Mathf.Sin(phi);
				Worldpos_Z = Radius * Mathf.Cos(theta);
			}

			Vector2 data = uvData[index];
			
			float height = Radius;
			float frequency = Frequency;
			float amplitude = Amplitude;

			for (int i = 0; i < Octaves; i++)
			{
				height += PerlinNoise_Native.Sample3D(Worldpos_X, Worldpos_Y, Worldpos_Z, frequency, amplitude, ref PermutationTable);
				frequency *= Lacunarity;
				amplitude *= Gain;

			}

			int value;
			if (dist < height)
			{
				value = StartValue;
			}
			else
			{
				float rest = dist - height;
				value = (int)(StartValue - rest * Falloff);
			}
		
			data.x = ApplyMode.Invoke(data.x, value);
			uvData[index] = data;
		}
	}

}
