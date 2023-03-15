using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using NativeCopyFast;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	public unsafe class NativeMarchingCubes : UniformGridVisualHull
	{
		[BeginInfo("MARCHINGCUBES")]
		[InfoTitle("Marching Cubes", "Marching cubes is the most common visualization method for voxels and is mostly used for terrain or other natural environments because the appearance is completely smooth. " +
			"The smoothness is caused by the ID value which is between 0 and 255 so even voxel maps with low resolution have smooth surfaces", "MARCHINGCUBES")]
		[InfoSection1("How to use:", "This script is a hull generator which means that it is used by the voxel generator for the visualisation. " +
			"Therefore it must be attached as child game object to the game object containing the voxel generator. " +
			"The resolution settings define the measurement. Higher values will make the result much finer as smaller voxels are measured in a grid like fashion", "MARCHINGCUBES")]
		[InfoSection2("Resolution Settings:", "" +
		"<b>Width:</b> Width of each chunk. Width of 8 means a chunk contains 8x8x8 voxels\n" +
		"<b>Cell Subdivision:</b> Defines how many chunks will be generated. Value of 8 means 8x8x8 = 64 chunks.\n" +
		"<b>Num Cores:</b> Amount of parallelisation. Value of 8 means 8 CPU cores are dedicated for hull generation\n" +
		"", "MARCHINGCUBES")]
		[InfoSection3("Appearance Settings:", "" +
		"<b>Minimum ID:</b> The Minimum ID which is possible during generation. 0 is default.\n" +
		"<b>Maximum ID:</b> The Maximum ID which is possible during generation. 255 is default.\n" +
		"<b>Voxel Material:</b> Normal Unity3D material. Any material is possible (Standard, Mobile, Custom shaders)\n\n" +
		"<b>UV Power:</b> Multiplier for texture UV coordinates. \n\n" +
		"<b>Smooth Angle:</b> Smoothes the vertex normals. Works similar to the smooth settings inside the import settings of 3D Models but is much faster. \n\n" +
		"<color=blue>Marching cubes was one of the first hull generators and there is a variety including one supporing multi material and texture atlas. " +
			"This algorithm is perfectly suited for terrain and will also be commonly used inside other, more advanced hull generators. </color>", "MARCHINGCUBES")]
		[InfoVideo("https://www.youtube.com/watch?v=xYlTQ2MgfdY&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=9", false, "MARCHINGCUBES")]
		[InfoText("Marching Cubes:", "MARCHINGCUBES")]
		
		[Range(0, 255)]
		public float MinimumID;
		[Range(0, 255)]
		public float MaximumID;

		public float UVPower = 1;
		public float SmoothAngle = 60;

		private NativeMarchingCubes_Calculation[] calculators;

		protected override float GetChecksum()
		{
			return base.GetChecksum() + 10 + UVPower + SmoothAngle + MinimumID + MaximumID;
		}

		protected override void initializeCalculators()
		{		
		
			calculators = new NativeMarchingCubes_Calculation[NumCores];
			
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
			m_JobHandles[m] = calculators[m].Schedule();
		}

		protected override void finishCalculation(int m, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
		{
			NativeMarchingCubes_Calculation usedcalculator = calculators[m];
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
	}

}
