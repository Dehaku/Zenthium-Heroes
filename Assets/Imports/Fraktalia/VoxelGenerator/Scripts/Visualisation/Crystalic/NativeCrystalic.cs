using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	public class NativeCrystalic : UniformGridVisualHull
	{
		public NativeArray<Vector3> Permutations;

		[Header("Crystal Shape Settings")]
		public Mesh CrystalMesh;
		
		public Vector3 Offset_min = new Vector3(0, 0, 0);
		public Vector3 Offset_max = new Vector3(0, 0, 0);
		public Vector3 Scale_min = new Vector3(1, 1, 1);
		public Vector3 Scale_max = new Vector3(1, 1, 1);
		public float ScaleFactor_min = 1;
		public float ScaleFactor_max = 1;

		public Vector3 Rotation_min;
		public Vector3 Rotation_max;

		[Range(0, 1)]
		public float Probability;
		public int Seed = 0;

		[Header("UV Settings")]
		public bool UseBoxUV;
		public float UVPower;

		[Header("Surface Settings:")]
		[Range(0, 255)]
		public float MinimumID;
		[Range(0, 255)]
		public float MaximumID;

		NativeCrystalic_Calculation[] calculators;

		protected override void initializeCalculators()
		{
			calculators = new NativeCrystalic_Calculation[NumCores];
			var oldseed = UnityEngine.Random.state;
			UnityEngine.Random.InitState(Seed);
			Permutations = new NativeArray<Vector3>(10000, Allocator.Persistent);
			for (int i = 0; i < 10000; i++)
			{
				Permutations[i] = UnityEngine.Random.insideUnitSphere;
			}

			for (int i = 0; i < calculators.Length; i++)
			{

				calculators[i].Shrink = Shrink;


				calculators[i].Init(CrystalMesh, width);
				calculators[i].Permutations = Permutations;


				calculators[i].VertexOffset = MarchingCubes_Data.GetVertexOffset;
				calculators[i].EdgeConnection = MarchingCubes_Data.GetEdgeConnection;
				calculators[i].EdgeDirection = MarchingCubes_Data.GetEdgeDirection;
				calculators[i].CubeEdgeFlags = MarchingCubes_Data.GetCubeEdgeFlags;
				calculators[i].TriangleConnectionTable = MarchingCubes_Data.GetTriangleConnectionTable;

				calculators[i].data = engine.Data[0];

				UnityEngine.Random.InitState(Seed + 10);

			}

			UnityEngine.Random.state = oldseed;
		}

		protected override void beginCalculation(int m, int index, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{
			calculators[m].voxelSize = cellSize / (width);
			calculators[m].cellSize = cellSize;
			calculators[m].positionoffset = new Vector3(startX, startY, startZ);

			calculators[m].Offset_min = Offset_min;
			calculators[m].Offset_max = Offset_max;
			calculators[m].ScaleMin = Scale_min;
			calculators[m].ScaleMax = Scale_max;
			calculators[m].ScaleFactor_min = ScaleFactor_min;
			calculators[m].ScaleFactor_max = ScaleFactor_max;

			int i = (int)(startX / cellSize);
			int j = (int)(startY / cellSize);
			int k = (int)(startZ / cellSize);

			calculators[m].RandomIndexOffset = i * 10 + j * 100 + k * 1000;
			calculators[m].Probability = Probability * 2 - 1;

			calculators[m].rotation_min = Rotation_min;
			calculators[m].rotation_max = Rotation_max;

			calculators[m].UseBoxUV = UseBoxUV ? 1 : 0;
			calculators[m].UVPower = UVPower;

			calculators[m].minimumID = MinimumID;
			calculators[m].maximumID = MaximumID;



			m_JobHandles[m] = calculators[m].Schedule();
		}

		protected override void finishCalculation(int m, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
		{
			NativeCrystalic_Calculation usedcalculator = calculators[m];
			if (usedcalculator.verticeArray.Length != 0)
			{
				piece.SetVertices(usedcalculator.verticeArray);
				piece.SetTriangles(usedcalculator.triangleArray);
				piece.SetNormals(usedcalculator.normalArray);
				piece.SetTangents(usedcalculator.tangentsArray);
				piece.SetUVs(0, usedcalculator.uvArray);
			}

			vertices = usedcalculator.verticeArray;
			triangles = usedcalculator.triangleArray;
			normals = usedcalculator.normalArray;
		}

		protected override void cleanUpCalculation()
		{
			if (calculators != null)
			{
				for (int i = 0; i < calculators.Length; i++)
				{
					m_JobHandles[i].Complete();
					calculators[i].CleanUp();
				}
			}
			if (Permutations.IsCreated) Permutations.Dispose();
		}

		protected override float GetChecksum()
		{
			float sum = Seed;
			sum += Offset_min.sqrMagnitude * 10000;
			sum += Offset_max.sqrMagnitude * 10000;
			sum += Scale_min.sqrMagnitude * 10000;
			sum += Scale_max.sqrMagnitude * 10000;
			sum += Rotation_min.sqrMagnitude * 10000;
			sum += Rotation_max.sqrMagnitude * 10000;
			sum += (ScaleFactor_min * 10000);
			sum += (ScaleFactor_max * 10000);
			sum += (Probability * 10000);

			return (int)sum;
		}
	}
}

