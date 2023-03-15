using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
	public class NativeAtlasCubic : UniformGridVisualHull
	{
		
		
		
		[BeginInfo("ATLASCUBIC")]
		[InfoTitle("Atlas Cubes", "Atlas cubes is the generic visualisation for blocky terrain as it is used in minecraft. " +
			"The main parameter is the atlas rows which is used to define the position of the texture atlas.", "ATLASCUBIC")]
		[InfoSection1("How to use:", "This script is a hull generator which means that it is used by the voxel generator for the visualisation. " +
			"Therefore it must be attached as child game object to the game object containing the voxel generator. " +
			"The resolution settings define the measurement. Higher values will make the result much finer as smaller voxels are measured in a grid like fashion. " +
			"Voxel ID of 0 = Empty Voxel, 1-255 is solid and the UV coordinate is defined by the ID value. So a texture atlas can have up to 255 different textures. " +
			"For example it is possible to use the texture atlas provided by minecraft (Is not included due to copyright). ", "ATLASCUBIC")]
		[InfoSection2("Resolution Settings:", "" +
		"<b>Width:</b> Width of each chunk. Width of 8 means a chunk contains 8x8x8 voxels\n" +
		"<b>Cell Subdivision:</b> Defines how many chunks will be generated. Value of 8 means 8x8x8 = 64 chunks.\n" +
		"<b>Num Cores:</b> Amount of parallelisation. Value of 8 means 8 CPU cores are dedicated for hull generation\n" +
		"", "ATLASCUBIC")]
		[InfoSection3("Appearance Settings:", "" +
		"<b>Atlas Rows:</b> How many rows has your sprite atlas. Minecraft Atlas is a 16 x 16 grid. Therefore this value must be 16\n" +
		"<b>Voxel Material:</b> Normal Unity3D material. Any material is possible (Standard, Mobile, Custom shaders)\n\n" +
		"<b>Smooth Angle:</b> Smoothes the vertex normals. Works similar to the smooth settings inside the import settings of 3D Models but is much faster.", "ATLASCUBIC")]
		[InfoVideo("https://www.youtube.com/watch?v=2nc3Vi2YQCw&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=12", false, "ATLASCUBIC")]
		[InfoText("Atlas Cubes:", "ATLASCUBIC")]
		[Header("Appearance Settings")]
		[Tooltip("Defines how UV coordinates are shifted according to the Voxel ID. " +
			"For example if set to 16, a Sprite atlas containing 255 tiles is represented " +
			"where each ID represents one tile similar to minecraft (0 is empty).")]
		public int AtlasRows = 2;
		
		public float SmoothAngle = 60;

		NativeAtlasCubic_Calculation[] calculators;
		
		protected override void initializeCalculators()
		{
			calculators = new NativeAtlasCubic_Calculation[NumCores];
			for (int i = 0; i < calculators.Length; i++)
			{
				calculators[i].Init(width);
				calculators[i].VertexOffset = Cubic_Data.GetVertexOffset;
				calculators[i].AtlasRow = AtlasRows;
				calculators[i].data = engine.Data[0];
			}
		}

		protected override void beginCalculation(int m, int index, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{
			calculators[m].voxelSize = cellSize / (width);
			calculators[m].cellSize = cellSize;
			calculators[m].positionoffset = new Vector3(startX, startY, startZ);
			calculators[m].SmoothingAngle = SmoothAngle;
			m_JobHandles[m] = calculators[m].Schedule();
		}

		protected override void finishCalculation(int m, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
		{
			NativeAtlasCubic_Calculation usedcalculator = calculators[m];
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


#if UNITY_EDITOR
	[CustomEditor(typeof(NativeAtlasCubic))]
	public class CurveEditor : Editor
	{


		public override void OnInspectorGUI()
		{

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 14;
			bold.richText = true;




			NativeAtlasCubic mytarget = target as NativeAtlasCubic;
			DrawDefaultInspector();


		}
	}
#endif

}
