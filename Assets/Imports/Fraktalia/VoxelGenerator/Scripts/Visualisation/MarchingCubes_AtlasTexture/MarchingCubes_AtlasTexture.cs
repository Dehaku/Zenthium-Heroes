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
	public class MarchingCubes_AtlasTexture : UniformGridVisualHull
	{
		

		MarchingCubes_AtlasTexture_Calculation[] calculators;
		
		[Header("Appearance Settings:")]

		[Range(0, 255)]
		public float MinimumID;
		[Range(0, 255)]
		public float MaximumID;

		[Range(1, 16)]
		public int AtlasRows = 2;
		[Range(1,16)][Tooltip("Groups Atlas lookup to set of vertices. 0, 3 and 6 looks good. Everythign else looks bad -,-")]
		public int AtlasModulo = 3;
		
		public float UVPower = 1;
		public float SmoothAngle = 60;

		[Header("Texture Channels")]
		[Range(-1, 4)]
		public int AtlasTextureDimension = -1;

		protected override void initializeCalculators()
		{
			calculators = new MarchingCubes_AtlasTexture_Calculation[NumCores];
			for (int i = 0; i < calculators.Length; i++)
			{
				calculators[i].Init(width);

				calculators[i].Shrink = Shrink;


				calculators[i].VertexOffset = MarchingCubes_Data.GetVertexOffset;
				calculators[i].EdgeConnection = MarchingCubes_Data.GetEdgeConnection;
				calculators[i].EdgeDirection = MarchingCubes_Data.GetEdgeDirection;
				calculators[i].CubeEdgeFlags = MarchingCubes_Data.GetCubeEdgeFlags;
				calculators[i].TriangleConnectionTable = MarchingCubes_Data.GetTriangleConnectionTable;

				calculators[i].data = engine.Data[0];

				if (AtlasTextureDimension != -1 && AtlasTextureDimension < engine.Data.Length)
				{
					calculators[i].texturedata_UV3 = engine.Data[AtlasTextureDimension];
				}
			}
		}

		protected override void beginCalculation(int m, int index, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{
			calculators[m].voxelSize = cellSize / (width);
			calculators[m].cellSize = cellSize;
			calculators[m].positionoffset = new Vector3(startX, startY, startZ);
			calculators[m].SmoothingAngle = SmoothAngle;
			calculators[m].minimumID = MinimumID;
			calculators[m].maximumID = MaximumID;
			calculators[m].UVPower = UVPower;
			calculators[m].AtlasRow = AtlasRows;
			calculators[m].AtlasModulo = AtlasModulo;
			m_JobHandles[m] = calculators[m].Schedule();
		}

		protected override void finishCalculation(int m, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
		{
			MarchingCubes_AtlasTexture_Calculation usedcalculator = calculators[m];
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
		}

		protected override float GetChecksum()
		{
			return base.GetChecksum() + NumCores * 10 + UVPower + SmoothAngle + AtlasModulo;
		}
	}

}
