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
	public class MarchingCubes_MultiTexture : UniformGridVisualHull
	{	
	
		
		[Header("Appearance Settings:")]

		[Range(0, 255)]
		public float MinimumID;
		[Range(0, 255)]
		public float MaximumID;

		public float UVPower = 1;
		public float SmoothAngle = 60;

		[Header("Texture Channels")]
		[Range(-1, 4)]
		public int TextureDimensionUV3 = -1;
		
		[Range(-1, 4)]
		public int TextureDimensionUV4 = -1;
	
		[Range(-1, 4)]
		public int TextureDimensionUV5 = -1;
		
		[Range(-1, 4)]
		public int TextureDimensionUV6 = -1;
		[Space]
		public float TexturePowerUV3 = 1;
		public float TexturePowerUV4 = 1;
		public float TexturePowerUV5 = 1;
		public float TexturePowerUV6 = 1;
		[Space]
		[Tooltip("Bakes barycentric colors into the mesh. Is required for seamless tesselation.")]
		public bool AddTesselationColors = true;

		[Header("Version Settings:")]
		[Tooltip("Uses improved marching cubes calculator which also extracts normals from the Voxel data. The normals are perfectly seamless!")]
		public bool UseNewCalculator;

		MarchingCubes_MultiTexture_Calculation[] calculators;

		MarchingCubes_MultiTexture_Calculation_Improved[] calculators_v2;

		protected override void initializeCalculators()
		{
			if(UseNewCalculator)
			{
				initializeV2Calculators();
				return;
			}



			calculators = new MarchingCubes_MultiTexture_Calculation[NumCores];
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

				if (TextureDimensionUV3 != -1 && TextureDimensionUV3 < engine.Data.Length)
				{
					calculators[i].texturedata_UV3 = engine.Data[TextureDimensionUV3];
				}

				if (TextureDimensionUV4 != -1 && TextureDimensionUV4 < engine.Data.Length)
				{
					calculators[i].texturedata_UV4 = engine.Data[TextureDimensionUV4];
				}

				if (TextureDimensionUV5 != -1 && TextureDimensionUV5 < engine.Data.Length)
				{
					calculators[i].texturedata_UV5 = engine.Data[TextureDimensionUV5];
				}

				if (TextureDimensionUV6 != -1 && TextureDimensionUV6 < engine.Data.Length)
				{
					calculators[i].texturedata_UV6 = engine.Data[TextureDimensionUV6];
				}
			}
		}

		private void initializeV2Calculators()
		{
			calculators_v2 = new MarchingCubes_MultiTexture_Calculation_Improved[NumCores];
			for (int i = 0; i < calculators_v2.Length; i++)
			{
				calculators_v2[i].Init(width);

				calculators_v2[i].Shrink = Shrink;


				calculators_v2[i].VertexOffset = MarchingCubes_Data.GetVertexOffset;
				calculators_v2[i].EdgeConnection = MarchingCubes_Data.GetEdgeConnection;
				calculators_v2[i].EdgeDirection = MarchingCubes_Data.GetEdgeDirection;
				calculators_v2[i].CubeEdgeFlags = MarchingCubes_Data.GetCubeEdgeFlags;
				calculators_v2[i].TriangleConnectionTable = MarchingCubes_Data.GetTriangleConnectionTable;

				calculators_v2[i].data = engine.Data[0];

				if (TextureDimensionUV3 != -1 && TextureDimensionUV3 < engine.Data.Length)
				{
					calculators_v2[i].texturedata_UV3 = engine.Data[TextureDimensionUV3];
				}

				if (TextureDimensionUV4 != -1 && TextureDimensionUV4 < engine.Data.Length)
				{
					calculators_v2[i].texturedata_UV4 = engine.Data[TextureDimensionUV4];
				}

				if (TextureDimensionUV5 != -1 && TextureDimensionUV5 < engine.Data.Length)
				{
					calculators_v2[i].texturedata_UV5 = engine.Data[TextureDimensionUV5];
				}

				if (TextureDimensionUV6 != -1 && TextureDimensionUV6 < engine.Data.Length)
				{
					calculators_v2[i].texturedata_UV6 = engine.Data[TextureDimensionUV6];
				}
			}
		}



		protected override void beginCalculation(int m, int index, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{
			if (UseNewCalculator)
			{
				calculators_v2[m].voxelSize = cellSize / (width);
				calculators_v2[m].cellSize = cellSize;
				calculators_v2[m].positionoffset = new Vector3(startX, startY, startZ);
				calculators_v2[m].SmoothingAngle = SmoothAngle;
				calculators_v2[m].minimumID = MinimumID;
				calculators_v2[m].maximumID = MaximumID;
				calculators_v2[m].UVPower = UVPower;
				calculators_v2[m].TexturePowerUV3 = TexturePowerUV3;
				calculators_v2[m].TexturePowerUV4 = TexturePowerUV4;
				calculators_v2[m].TexturePowerUV5 = TexturePowerUV5;
				calculators_v2[m].TexturePowerUV6 = TexturePowerUV6;
				m_JobHandles[m] = calculators_v2[m].Schedule();
				return;
			}


			calculators[m].voxelSize = cellSize / (width);
			calculators[m].cellSize = cellSize;
			calculators[m].positionoffset = new Vector3(startX, startY, startZ);
			calculators[m].SmoothingAngle = SmoothAngle;
			calculators[m].minimumID = MinimumID;
			calculators[m].maximumID = MaximumID;
			calculators[m].UVPower = UVPower;
			calculators[m].TexturePowerUV3 = TexturePowerUV3;
			calculators[m].TexturePowerUV4 = TexturePowerUV4;
			calculators[m].TexturePowerUV5 = TexturePowerUV5;
			calculators[m].TexturePowerUV6 = TexturePowerUV6;

			m_JobHandles[m] = calculators[m].Schedule();
		}

		protected override void finishCalculation(int m, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
		{
			if (UseNewCalculator)
			{
				var usedcalculatorv2 = calculators_v2[m];
				if (usedcalculatorv2.verticeArray.Length != 0)
				{
					piece.SetVertices(usedcalculatorv2.verticeArray);
					piece.SetTriangles(usedcalculatorv2.triangleArray);
					piece.SetNormals(usedcalculatorv2.normalArray);
					piece.SetTangents(usedcalculatorv2.tangentsArray);
					piece.SetUVs(0, usedcalculatorv2.uvArray);

					if (TextureDimensionUV3 != -1)
						piece.SetUVs(2, usedcalculatorv2.uv3Array);

					if (TextureDimensionUV4 != -1)
						piece.SetUVs(3, usedcalculatorv2.uv4Array);

					if (TextureDimensionUV5 != -1)
						piece.SetUVs(4, usedcalculatorv2.uv5Array);

					if (TextureDimensionUV6 != -1)
						piece.SetUVs(5, usedcalculatorv2.uv6Array);

					if (AddTesselationColors)
					{
						piece.SetColors(calculators_v2[m].colorArray);
					}
				}

				vertices = usedcalculatorv2.verticeArray;
				triangles = usedcalculatorv2.triangleArray;
				normals = usedcalculatorv2.normalArray;
				return;
			}

			var usedcalculator = calculators[m];
			if (usedcalculator.verticeArray.Length != 0)
			{
				piece.SetVertices(usedcalculator.verticeArray);
				piece.SetTriangles(usedcalculator.triangleArray);
				piece.SetNormals(usedcalculator.normalArray);
				piece.SetTangents(usedcalculator.tangentsArray);
				piece.SetUVs(0, usedcalculator.uvArray);

				if (TextureDimensionUV3 != -1)
					piece.SetUVs(2, usedcalculator.uv3Array);

				if (TextureDimensionUV4 != -1)
					piece.SetUVs(3, usedcalculator.uv4Array);

				if (TextureDimensionUV5 != -1)
					piece.SetUVs(4, usedcalculator.uv5Array);

				if (TextureDimensionUV6 != -1)
					piece.SetUVs(5, usedcalculator.uv6Array);

				if (AddTesselationColors)
				{
					piece.SetColors(calculators[m].colorArray);
				}
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

			if (calculators_v2 != null)
			{
				for (int i = 0; i < calculators_v2.Length; i++)
				{
					m_JobHandles[i].Complete();
					calculators_v2[i].CleanUp();

				}
			}
		}

		protected override float GetChecksum()
		{
			float details = 0;

			BasicSurfaceModifier.RemoveDuplicates(DetailGenerator);
			if (!Application.isPlaying)
			{
				DetailGenerator.Clear();
				DetailGenerator.AddRange(engine.GetComponentsInChildren<BasicSurfaceModifier>());
			}

			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if(DetailGenerator[i])
				details += DetailGenerator[i].GetChecksum();
			}

			return base.GetChecksum() + Cell_Subdivision * 1000 + width * 100 + NumCores * 10 + UVPower + SmoothAngle + MinimumID + MaximumID
				+ TexturePowerUV3 + TexturePowerUV4 + TexturePowerUV5 + TexturePowerUV6+details + (UseNewCalculator ? 0:1);

			

		}
	}

}
