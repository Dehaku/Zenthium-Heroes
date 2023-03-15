using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Fraktalia.Core.Math;
using Fraktalia.VoxelGen;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public static class ResultSmoothFilter
	{



		public static void Apply(NativeArray<NativeVoxelModificationData> data, int SizeX, int SizeY, int SizeZ, int BoxWidth)
		{
			ResultSmoothFilter_calculate calculate = new ResultSmoothFilter_calculate();
			calculate.input = data;
			calculate.output = new NativeArray<NativeVoxelModificationData>(data.Length, Allocator.TempJob);
			calculate.Size_X = SizeX;
			calculate.Size_Y = SizeY;
			calculate.Size_Z = SizeZ;
			calculate.BoxWidth = BoxWidth;


			JobHandle handle = calculate.Schedule(data.Length, 100);
			handle.Complete();


			data.CopyFrom(calculate.output);

			calculate.output.Dispose();

		}


	}

	[BurstCompile]
	public struct ResultSmoothFilter_calculate : IJobParallelFor
	{
		public int BoxWidth;
		public int Size_X;
		public int Size_Y;
		public int Size_Z;

		[ReadOnly]
		public NativeArray<NativeVoxelModificationData> input;

		[WriteOnly]
		public NativeArray<NativeVoxelModificationData> output;



		public void Execute(int index)
		{
			Vector3Int position = MathUtilities.Convert1DTo3D(index, Size_X, Size_Y, Size_Z);

			int z = position.z;
			int y = position.y;
			int x = position.x;

			int SumID = 0;




			int count = 0;
			for (int a = -BoxWidth; a <= BoxWidth; a++)
			{
				for (int b = -BoxWidth; b <= BoxWidth; b++)
				{
					for (int c = -BoxWidth; c <= BoxWidth; c++)
					{
						count++;

						int x1 = x + a;
						int y1 = y + b;
						int z1 = z + c;

						if (x1 < 0 || x1 >= Size_X) continue;
						if (y1 < 0 || y1 >= Size_Y) continue;
						if (z1 < 0 || z1 >= Size_Z) continue;

						SumID += input[MathUtilities.Convert3DTo1D(x1, y1, z1, Size_X, Size_Y, Size_Z)].ID;

					}
				}
			}

			SumID /= count;


			NativeVoxelModificationData result = input[index];
			result.ID = SumID;

			output[index] = result;


		}
	}

}