using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.World
{
	public class WorldAlgorithm_Oscillation : WorldAlgorithm
	{
		[Header("Fractal Settings")]
		public int Octaves;
		public float Lacunarity;
		public float Gain;

		[Header("Sinus/Oscillation Settings")]
		public float SinusPower;
		public float SinusFrequenz;
		public Vector3 Offset;




		WorldAlgorithm_Oscillation_Calculate calculate;
		

		public override void Initialize(VoxelGenerator template)
		{
			int width = template.GetBlockWidth(Depth);
			int blocks = width * width * width;
			
			calculate.Width = width;
			calculate.Blocks = blocks;
			calculate.Depth = (byte)Depth;
				
		}

		public override JobHandle Apply(Vector3 hash, VoxelGenerator targetGenerator, ref JobHandle handle)
		{
			calculate.VoxelSize = targetGenerator.GetVoxelSize(Depth);
			calculate.RootSize = targetGenerator.RootSize;
			calculate.voxeldata = worldGenerator.modificationReservoir.GetDataArray(Depth);
			calculate.ApplyMode = ApplyFunctionPointer;
			calculate.PostProcessFunctionPointer = PostProcessFunctionPointer;
			calculate.PositionOffset = hash * targetGenerator.RootSize + Offset * scale;
			calculate.SinusPower = SinusPower * scale;
			calculate.SinusFrequenz = SinusFrequenz/ scale;
			calculate.FallOff = (10 * 40) / scale;
			calculate.Octaves = Octaves;
			calculate.Lacunarity = Lacunarity;
			calculate.Gain = Gain;


			return calculate.Schedule(calculate.Blocks, 64, handle);	
		}	
	}

	
	
	[BurstCompile]
	public struct WorldAlgorithm_Oscillation_Calculate : IJobParallelFor
	{
		public float SinusPower;
		public float SinusFrequenz;

		public int Octaves;
		public float Lacunarity;
		public float Gain;

		public byte Depth;
		public float VoxelSize;
		public float RootSize;
		public int Width;
		public int Blocks;
		public Vector3 PositionOffset;
		public float FallOff;
		
		public NativeArray<NativeVoxelModificationData_Inner> voxeldata;
		public FunctionPointer<WorldAlgorithm_Mode> ApplyMode;
		public FunctionPointer<WorldAlgorithm_PostProcess> PostProcessFunctionPointer;

		public void Execute(int index)
		{

			int x = index % Width;
			int y = (index - x) / Width % Width;
			int z = ((index - x) / Width - y) / Width;

			float Voxelpos_X = x * VoxelSize;
			float Voxelpos_Y = y * VoxelSize;
			float Voxelpos_Z = z * VoxelSize;
			Voxelpos_X += VoxelSize / 2;
			Voxelpos_Y += VoxelSize / 2;
			Voxelpos_Z += VoxelSize / 2;


			float Worldpos_X = PositionOffset.x + Voxelpos_X;
			float Worldpos_Y = PositionOffset.y + Voxelpos_Y;
			float Worldpos_Z = PositionOffset.z + Voxelpos_Z;

			NativeVoxelModificationData_Inner info = voxeldata[index];
			info.Depth = Depth;
			info.X = VoxelGenerator.ConvertLocalToInner(Voxelpos_X, RootSize);
			info.Y = VoxelGenerator.ConvertLocalToInner(Voxelpos_Y, RootSize);
			info.Z = VoxelGenerator.ConvertLocalToInner(Voxelpos_Z, RootSize);
			float height = 0;

			float frequency = SinusFrequenz;
			float amplitude = SinusPower;
			for (int i = 0; i < Octaves; i++)
			{
				height += Mathf.Sin(Worldpos_X * frequency) * Mathf.Sin(Worldpos_Z * frequency) * amplitude;
				frequency *= Lacunarity;
				amplitude *= Gain;

			}



			int value = 0;
			if (Worldpos_Y < height)
			{
				value = 255;
			}
			else
			{
				float rest = Worldpos_Y - height;


				value = (int)(255 - rest * FallOff);
			}
			value = PostProcessFunctionPointer.Invoke(value);
			ApplyMode.Invoke(ref info, value);
			info.ID = Mathf.Clamp(info.ID, 0, 255);

			voxeldata[index] = info;

		}

		public void Dispose()
		{
			if (voxeldata.IsCreated) voxeldata.Dispose();
		}
	}
}
