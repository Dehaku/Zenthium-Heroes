using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Utility;
using Fraktalia.Core.Math;
using Unity.Collections.LowLevel.Unsafe;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
	public unsafe class NativeAtlasCubic_GPU : UniformGridVisualHull_SingleStep
	{	
		[BeginInfo("ATLASCUBIC_GPU")]
		[InfoTitle("Atlas Cubes", "Atlas cubes is the generic visualisation for blocky terrain as it is used in minecraft. " +
			"The main parameter is the atlas rows which is used to define the position of the texture atlas.\n\n" +
			"<b>GPU Version:</b> This version use the GPU for most calculations and is faster than the CPU only variant. Is in experimental stage to get it faster (still have to learn a lot about GPU optimisations).", "ATLASCUBIC_GPU")]
		[InfoSection1("How to use:", "This script is a hull generator which means that it is used by the voxel generator for the visualisation. " +
			"Therefore it must be attached as child game object to the game object containing the voxel generator. " +
			"The resolution settings define the measurement. Higher values will make the result much finer as smaller voxels are measured in a grid like fashion. " +
			"Voxel ID of 0 = Empty Voxel, 1-255 is solid and the UV coordinate is defined by the ID value. So a texture atlas can have up to 255 different textures. " +
			"For example it is possible to use the texture atlas provided by minecraft (Is not included due to copyright). ", "ATLASCUBIC_GPU")]
		[InfoSection2("Resolution Settings:", "" +
		"<b>Width:</b> Width of each chunk. Width of 8 means a chunk contains 8x8x8 voxels\n" +
		"<b>Cell Subdivision:</b> Defines how many chunks will be generated. Value of 8 means 8x8x8 = 64 chunks.\n" +
		"<b>Num Cores:</b> Amount of parallelisation. Value of 8 means 8 CPU cores are dedicated for hull generation\n" +
		"", "ATLASCUBIC_GPU")]
		[InfoSection3("Appearance Settings:", "" +
		"<b>Atlas Rows:</b> How many rows has your sprite atlas. Minecraft Atlas is a 16 x 16 grid. Therefore this value must be 16\n" +
		"<b>Voxel Material:</b> Normal Unity3D material. Any material is possible (Standard, Mobile, Custom shaders)\n\n" +
		"<b>Smooth Angle:</b> Smoothes the vertex normals. Works similar to the smooth settings inside the import settings of 3D Models but is much faster.", "ATLASCUBIC_GPU")]
		[InfoVideo("https://www.youtube.com/watch?v=2nc3Vi2YQCw&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=12", false, "ATLASCUBIC_GPU")]
		[InfoText("Atlas Cubes:", "ATLASCUBIC_GPU")]
		[Header("Appearance Settings")]
		[Tooltip("Defines how UV coordinates are shifted according to the Voxel ID. " +
			"For example if set to 16, a Sprite atlas containing 255 tiles is represented " +
			"where each ID represents one tile similar to minecraft (0 is empty).")]
		public int AtlasRows = 2;		
		public float SmoothAngle = 60;

		[Range(0, 10)]
		public int SurfaceDimension = 0;

		

		[Tooltip("Removes triangles from the final result. New algorithm allows this. Could be used for spawn effects.")]
		public int Destruction;

		[Header("No Triplanar Settings:")]
		[Tooltip("Write barycentric colors into the mesh. Useful for Wireframe or seamless tesellation.")]
		public bool AddBarycentricColors;
		[Tooltip("Create Cube UVs for non-triplanar shader usage.")]
		public bool GenerateCubeUV;
		[Tooltip("Attempt to create spherical UV for planets (experimental).")]
		public bool GenerateSphereUV;
		[Tooltip("Recalculate Normals based on the mesh using Normal Angle for flat or smooth normals.")]
		[LabelText("Recalculate Normals (expensive, super time loss)")]
		public bool RecalculateNormals;
		public float NormalAngle;
		public float UVPower = 1;

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

		[Header("Compute Shaders:")]
		public ComputeShader m_atlasCubes;
		GPUToMesh_v2[] dataconverters;

		public int TargetLOD;
		private List<int> LODSize;
		

		ComputeBuffer voxeldata;
		ComputeBuffer vertexArray_GPU;
		ComputeBuffer normalArray_GPU;
		ComputeBuffer triangleArray_GPU;
		ComputeBuffer uvArray_GPU;

		ComputeBuffer _counterBuffer;
		ComputeBuffer _counterBufferResult;
		private static int[] counter = new int[1];

		private Vector3[] vertexArray;
		private Vector3[] normalArray;
		private int[] triangleArray;
		private Vector2[] uvArray;

		public NativeArray<float> UniformGrid;
		NativeCreateUniformGrid_V2[] calculators;
		NativeAtlasCubic_Calculation[] calculators2;
		int BorderedWidth;
		private int MaximumVertexCount;
		private int MaximumTriangleCount;

		protected override void Initialize()
		{
			width = Mathf.ClosestPowerOfTwo(width);
			Cell_Subdivision = Mathf.ClosestPowerOfTwo(Cell_Subdivision);
			if (width < 4) width = 8;
			base.Initialize();
		}

		protected override void initializeCalculators()
		{
			m_atlasCubes = Resources.Load<ComputeShader>("Voxelica_AtlasCubes");

			LODSize = new List<int>();
			int lodsize = width;
			while (lodsize >= 4)
			{
				LODSize.Add(lodsize);
				lodsize = lodsize / 2;
			}


			MaximumVertexCount = width * width * width * 24;
			MaximumTriangleCount = width * width * width * 36;
			BorderedWidth = width + 3;
			UniformGrid = ContainerStaticLibrary.GetArray_float(BorderedWidth * BorderedWidth * BorderedWidth);

			calculators = new NativeCreateUniformGrid_V2[NumCores];
			dataconverters = new GPUToMesh_v2[NumCores];
			for (int i = 0; i < calculators.Length; i++)
			{
				calculators[i].Width = width;

				if (SurfaceDimension < engine.Data.Length)
				{
					calculators[i].data = engine.Data[SurfaceDimension];
				}
				else
				{
					calculators[i].data = engine.Data[0];
				}

				calculators[i].UniformGridResult = UniformGrid;

				GPUToMesh_v2 dataconverter = new GPUToMesh_v2();

				dataconverter.Init();

				if (TextureDimensionUV3 != -1 && TextureDimensionUV3 < engine.Data.Length)
				{
					dataconverter.texturedata_UV3 = engine.Data[TextureDimensionUV3];
				}

				if (TextureDimensionUV4 != -1 && TextureDimensionUV4 < engine.Data.Length)
				{
					dataconverter.texturedata_UV4 = engine.Data[TextureDimensionUV4];
				}

				if (TextureDimensionUV5 != -1 && TextureDimensionUV5 < engine.Data.Length)
				{
					dataconverter.texturedata_UV5 = engine.Data[TextureDimensionUV5];
				}

				if (TextureDimensionUV6 != -1 && TextureDimensionUV6 < engine.Data.Length)
				{
					dataconverter.texturedata_UV6 = engine.Data[TextureDimensionUV6];
				}
				dataconverter.verts = ContainerStaticLibrary.GetVertexBank("ATLASCUBES_GPU", MaximumVertexCount, i, NumCores);
				dataconverters[i] = dataconverter;
			}

			normalArray = new Vector3[MaximumVertexCount];
			vertexArray = new Vector3[MaximumVertexCount];
			triangleArray = new int[MaximumTriangleCount];
			uvArray = new Vector2[MaximumVertexCount];

			vertexArray_GPU = ContainerStaticLibrary.GetComputeBuffer("VertexBuffer", MaximumVertexCount, sizeof(float) * 3);
			normalArray_GPU = ContainerStaticLibrary.GetComputeBuffer("NormalBuffer", MaximumVertexCount, sizeof(float) * 3);
			triangleArray_GPU = ContainerStaticLibrary.GetComputeBuffer("TriangleBuffer", MaximumTriangleCount, sizeof(int));
			uvArray_GPU = ContainerStaticLibrary.GetComputeBuffer("UvBuffer", MaximumVertexCount, sizeof(float) * 2);

			//Holds the voxel values, generated from perlin noise.
			voxeldata = ContainerStaticLibrary.GetComputeBuffer("VoxelBuffer", BorderedWidth * BorderedWidth * BorderedWidth, sizeof(float));
			voxeldata.IsValid();

			// Buffer for triangle counting
			_counterBuffer = ContainerStaticLibrary.GetComputeBuffer("CounterBuffer", 1, 4, ComputeBufferType.Counter);
			_counterBufferResult = ContainerStaticLibrary.GetComputeBuffer("CounterBufferResult", 1, sizeof(int));

		}


		protected override void beginCalculation(int m, int index, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{
			int lod = Mathf.Clamp(TargetLOD, 0, LODSize.Count - 1);
			voxelSize = voxelSize * Mathf.Pow(2, lod);
			int lodwidth = LODSize[lod];
			MaximumVertexCount = lodwidth * lodwidth * lodwidth * 3 * 5;
			BorderedWidth = lodwidth + 3;
			int LODLength = BorderedWidth * BorderedWidth * BorderedWidth;

			Vector3Int offset = new Vector3Int();
			offset.x = NativeVoxelTree.ConvertLocalToInner(startX, engine.RootSize);
			offset.y = NativeVoxelTree.ConvertLocalToInner(startY, engine.RootSize);
			offset.z = NativeVoxelTree.ConvertLocalToInner(startZ, engine.RootSize);
			calculators[m].positionoffset = offset;
			calculators[m].Width = BorderedWidth;
			calculators[m].Shrink = (int)Shrink;
			int innerVoxelSize = NativeVoxelTree.ConvertLocalToInner(voxelSize, engine.RootSize);

			calculators[m].voxelSizeBitPosition = MathUtilities.RightmostBitPosition(innerVoxelSize);

			m_JobHandles[m] = calculators[m].Schedule(LODLength, LODLength / SystemInfo.processorCount);
			//m_JobHandles[m] = calculators[m].Schedule(UniformGrid.Length, 10000);
			m_JobHandles[m].Complete();

			voxeldata.SetData(UniformGrid);
			counter[0] = 0;

			_counterBufferResult.SetData(counter);
			m_atlasCubes.SetInt("_BlockWidth", BorderedWidth);
			m_atlasCubes.SetFloat("cellSize",cellSize);
			m_atlasCubes.SetVector("positionoffset", new Vector4(startX, startY, startZ, 0));
			m_atlasCubes.SetFloat("voxelSize", voxelSize);
			RunCompute(voxeldata, lodwidth);


			_counterBufferResult.GetData(counter);
			int meshsize = (Mathf.Clamp(counter[0] - Destruction, 0, counter[0]));

			GPUToMesh_v2 dataconverter = dataconverters[m];

			//_Buffer.GetData(verts);
			vertexArray_GPU.GetData(vertexArray, 0, 0, meshsize*4);
			normalArray_GPU.GetData(normalArray, 0, 0, meshsize*4);
			triangleArray_GPU.GetData(triangleArray, 0, 0, meshsize*6);
			uvArray_GPU.GetData(uvArray, 0, 0, meshsize * 6);

			//dataconverter.verts.CopyFrom(verts);
			dataconverter.managedVertexArray = (IntPtr)UnsafeUtility.AddressOf(ref vertexArray[0]);
			dataconverter.managedTriangleArray = (IntPtr)UnsafeUtility.AddressOf(ref triangleArray[0]);
			dataconverter.managedNormalArray = (IntPtr)UnsafeUtility.AddressOf(ref normalArray[0]);
			dataconverter.managedUVArray = (IntPtr)UnsafeUtility.AddressOf(ref uvArray[0]);

			dataconverter.meshSize = meshsize*4;
			dataconverter.meshSize_triangles = meshsize*6;
			dataconverter.Shrink = Shrink;
			dataconverter.UVPower = UVPower;
			dataconverter.GenerateCubeUV = GenerateCubeUV ? 1 : 0;
			dataconverter.GenerateSphereUV = GenerateSphereUV ? 1 : 0;
			dataconverter.rootSize = engine.RootSize;

			dataconverter.TexturePowerUV3 = TexturePowerUV3;
			dataconverter.TexturePowerUV4 = TexturePowerUV4;
			dataconverter.TexturePowerUV5 = TexturePowerUV5;
			dataconverter.TexturePowerUV6 = TexturePowerUV6;
			dataconverter.NormalAngle = NormalAngle;
			dataconverter.CalculateBarycentricColors = AddBarycentricColors ? 1 : 0;
			dataconverter.CalculateNormals = RecalculateNormals ? 1 : 0;
			dataconverters[m] = dataconverter;
			m_JobHandles[m] = dataconverter.Schedule();



			//calculators[m].voxelSize = cellSize / (width);
			//calculators[m].cellSize = cellSize;
			//calculators[m].positionoffset = new Vector3(startX, startY, startZ);
			//calculators[m].SmoothingAngle = SmoothAngle;
			//m_JobHandles[m] = calculators[m].Schedule();
		}

		void RunCompute(ComputeBuffer voxelsdata, int lodwidth)
		{
			_counterBuffer.SetCounterValue(0);
			m_atlasCubes.SetInt("AtlasRow", AtlasRows);
			m_atlasCubes.SetBuffer(0, "Voxels", voxelsdata);
			m_atlasCubes.SetBuffer(0, "vertexArray", vertexArray_GPU);
			m_atlasCubes.SetBuffer(0, "normalArray", normalArray_GPU);
			m_atlasCubes.SetBuffer(0, "triangleArray", triangleArray_GPU);
			m_atlasCubes.SetBuffer(0, "uvArray", uvArray_GPU);
			m_atlasCubes.SetBuffer(0, "counterBuffer", _counterBufferResult);
			m_atlasCubes.SetBuffer(0, "Counter", _counterBuffer);
			m_atlasCubes.Dispatch(0, lodwidth / 8, lodwidth / 8, lodwidth / 8);

			//Finalize the buffer count.
			m_atlasCubes.SetBuffer(1, "counterBuffer", _counterBufferResult);
			m_atlasCubes.SetBuffer(1, "Counter", _counterBuffer);
			m_atlasCubes.Dispatch(1, 1, 1, 1);
		}

		protected override void finishCalculation(int m, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
		{
			GPUToMesh_v2 dataconverter = dataconverters[m];


			if (dataconverter.verticeArray.Length != 0)
			{
				piece.SetVertices(dataconverter.verticeArray);
				piece.SetTriangles(dataconverter.triangleArray);
				piece.SetNormals(dataconverter.normalArray);
				piece.SetTangents(dataconverter.tangentsArray);
				piece.SetUVs(0, dataconverter.uvArray);

				if (TextureDimensionUV3 != -1)
					piece.SetUVs(2, dataconverter.uv3Array);

				if (TextureDimensionUV4 != -1)
					piece.SetUVs(3, dataconverter.uv4Array);

				if (TextureDimensionUV5 != -1)
					piece.SetUVs(4, dataconverter.uv5Array);

				if (TextureDimensionUV6 != -1)
					piece.SetUVs(5, dataconverter.uv6Array);

				if (GenerateCubeUV)
				{
					piece.SetUVs(0, dataconverter.uvArray);
					piece.SetTangents(dataconverter.tangentsArray);
				}

				if (AddBarycentricColors) piece.SetColors(dataconverter.colorArray);
			}

			vertices = dataconverter.verticeArray;
			triangles = dataconverter.triangleArray;
			normals = dataconverter.normalArray;
		}

		protected override void cleanUpCalculation()
		{
			if (calculators != null)
			{
				for (int i = 0; i < calculators.Length; i++)
				{
					m_JobHandles[i].Complete();
					dataconverters[i].CleanUp();
				}
			}
		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(NativeAtlasCubic_GPU))]
	public class NativeAtlasCubic_GPUEditor : Editor
	{


		public override void OnInspectorGUI()
		{

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 14;
			bold.richText = true;




			NativeAtlasCubic_GPU mytarget = target as NativeAtlasCubic_GPU;
			DrawDefaultInspector();


		}
	}
#endif

}
