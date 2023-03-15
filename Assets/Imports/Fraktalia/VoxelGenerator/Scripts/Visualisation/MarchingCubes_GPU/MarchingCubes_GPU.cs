using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using UnityEngine.Rendering;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Utility;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	public class MarchingCubes_GPU : UniformGridVisualHull_SingleStep
	{
		[BeginInfo("MarchingCubes_GPU")]
		[InfoTitle("GPU based marching cubes", "This is the fastest hull generator currently possible. It combines the power of CPU and GPU to increase perfomance far beyond " +
		"CPU only based hull generators.", "MarchingCubes_GPU")]
		[InfoSection1("How to use:", "Functionality is same as the CPU based marching cubes. It is important to have the compute shaders (Marching Cubes and Clear Buffer) assigned. " +
			"\n\nIt is recommended to use a triplanar shader so expensive UV, Normals and Tangents calculations can be avoided. If you cannot use triplanar shaders, you can enable them.", "MarchingCubes_GPU")]
		[InfoSection2("Compatibility:", "" +
		"<b>Direct X 11:</b> This hull generator uses Compute Shader which may not be supported by your target system.\n" +
		"For more information check the official statement from Unity:\n\nhttps://docs.unity3d.com/Manual/class-ComputeShader.html\n", "MarchingCubes_GPU")]		
		[InfoText("GPU based marching cubes:", "MarchingCubes_GPU")]



		[Range(0, 255)]
		public float MinimumID;
		[Range(0, 255)]
		public float MaximumID;

		[Header("No Triplanar Settings:")]
	
		[Tooltip("Write barycentric colors into the mesh. Useful for Wireframe or seamless tesellation.")]
		public bool AddBarycentricColors;
		[Tooltip("Create Cube UVs for non-triplanar shader usage.")]
		public bool GenerateCubeUV;
		[Tooltip("Recalculate Normals based on the mesh using Normal Angle for flat or smooth normals.")]
		[LabelText("Recalculate Normals (expensive)")]
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
		public ComputeShader m_marchingCubes;
		public ComputeShader m_clearBuffer;

		NativeCreateUniformGrid_Normalized[] calculators;

		public NativeArray<float> UniformGrid;


		//The size of the voxel array for each dimension
		private int N;

		//The size of the buffer that holds the verts.
		//This is the maximum number of verts that the 
		//marching cube can produce, 5 triangles for each voxel.
		private int SIZE;

		

		ComputeBuffer voxeldata;
		ComputeBuffer m_meshBuffer;	
		ComputeBuffer m_cubeEdgeFlags, m_triangleConnectionTable;
		
		private NativeArray<GPUVertex> nativeVerts;
		private GPUVertex[] verts;


		GPUToMesh[] dataconverters;

		protected override void Initialize()
		{
			if (width < 8) width = 8;
			width = width - width % 8;

			base.Initialize();
		}


		protected override void initializeCalculators()
		{
			m_marchingCubes = Resources.Load<ComputeShader>("Voxelica_MarchingCubes");
			m_clearBuffer = Resources.Load<ComputeShader>("Voxelica_ClearBuffer");


			N = width;
			SIZE = N * N * N * 3 * 5;

			int BlockWidth = N + 3;
			UniformGrid = ContainerStaticLibrary.GetArray_float(BlockWidth * BlockWidth * BlockWidth);

			calculators = new NativeCreateUniformGrid_Normalized[NumCores];
			dataconverters = new GPUToMesh[NumCores];
			for (int i = 0; i < calculators.Length; i++)
			{
				calculators[i].Init(BlockWidth);				
				calculators[i].data = engine.Data[0];
				calculators[i].UniformGridResult = UniformGrid;

				GPUToMesh dataconverter = new GPUToMesh();

				dataconverter.Init();
				dataconverter.SIZE = SIZE;

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
				dataconverter.verts = ContainerStaticLibrary.GetVertexBank("MARCHINGCUBES_GPU", SIZE, i, NumCores);
				dataconverters[i] = dataconverter;
			}

			verts = new GPUVertex[SIZE];
			m_meshBuffer = new ComputeBuffer(SIZE, sizeof(float) * 7);
			m_meshBuffer.IsValid();
		
			//Holds the voxel values, generated from perlin noise.
			voxeldata = new ComputeBuffer(BlockWidth * BlockWidth * BlockWidth, sizeof(float));
			voxeldata.IsValid();
			
			m_cubeEdgeFlags = new ComputeBuffer(256, sizeof(int));
			m_cubeEdgeFlags.SetData(MarchingCubes_Data.GetCubeEdgeFlags);
			m_triangleConnectionTable = new ComputeBuffer(256 * 16, sizeof(int));
			m_triangleConnectionTable.SetData(MarchingCubes_Data.GetTriangleConnectionTable);

			
			m_marchingCubes.SetBuffer(0, "_CubeEdgeFlags", m_cubeEdgeFlags);
			m_marchingCubes.SetBuffer(0, "_TriangleConnectionTable", m_triangleConnectionTable);
		}

		protected override void beginCalculation(int m, int index, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{
			if (!IsInitialized) return;

			calculators[m].Shrink = Shrink;			
			calculators[m].voxelSize = cellSize / (width);	
			calculators[m].positionoffset = new Vector3(startX, startY, startZ);	
			calculators[m].minimumID = MinimumID;
			calculators[m].maximumID = MaximumID;
			m_JobHandles[m] = calculators[m].Schedule(UniformGrid.Length, 1000);
			m_JobHandles[m].Complete();

			
			int BlockWidth = N + 3;

			m_clearBuffer.SetInt("_Width", N);
			m_clearBuffer.SetInt("_Height", N);
			m_clearBuffer.SetInt("_Depth", N);
			m_clearBuffer.SetBuffer(0, "_Buffer", m_meshBuffer);
			m_clearBuffer.Dispatch(0, N / 8, N / 8, N / 8);	
			voxeldata.SetData(UniformGrid);

		
			m_marchingCubes.SetInt("_Width", N);
			m_marchingCubes.SetInt("_BlockWidth", BlockWidth);
			m_marchingCubes.SetInt("_Height", N);
			m_marchingCubes.SetInt("_Depth", N);
			m_marchingCubes.SetInt("_Border", 1);
			m_marchingCubes.SetFloat("_Target", 0.0f);
			m_marchingCubes.SetBuffer(0, "_Buffer", m_meshBuffer);


			//Make the mesh verts
			m_marchingCubes.SetVector("_Positionoffset", new Vector4(startX, startY, startZ, 0));
			m_marchingCubes.SetFloat("_VoxelSize", voxelSize);
			m_marchingCubes.SetBuffer(0, "_Voxels", voxeldata);												
			m_marchingCubes.Dispatch(0, N / 8, N / 8, N / 8);
	
			

			GPUToMesh dataconverter = dataconverters[m];

			m_meshBuffer.GetData(verts);
			dataconverter.verts.CopyFrom(verts);

			dataconverter.Shrink = Shrink;
			dataconverter.UVPower = UVPower;
			dataconverter.GenerateCubeUV = GenerateCubeUV ? 1 : 0;
			dataconverter.TexturePowerUV3 = TexturePowerUV3;
			dataconverter.TexturePowerUV4 = TexturePowerUV4;
			dataconverter.TexturePowerUV5 = TexturePowerUV5;
			dataconverter.TexturePowerUV6 = TexturePowerUV6;
			dataconverter.NormalAngle = NormalAngle;
			dataconverter.CalculateBarycentricColors = AddBarycentricColors ? 1 : 0;
			dataconverter.CalculateNormals = RecalculateNormals ? 1 : 0;
			dataconverters[m] = dataconverter;
			m_JobHandles[m] = dataconverter.Schedule();
		}	

		protected override void finishCalculation(int m, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
		{
			GPUToMesh dataconverter = dataconverters[m];


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

			if (voxeldata != null)
			{
				voxeldata.Release();
				m_meshBuffer.Release();	
				m_cubeEdgeFlags.Release();
				m_triangleConnectionTable.Release();
			
			}

			if (nativeVerts.IsCreated) nativeVerts.Dispose();
			
		}

		protected override float GetChecksum()
		{
			float details = 0;

			BasicSurfaceModifier.RemoveDuplicates(DetailGenerator);
			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if(DetailGenerator[i])
				details += DetailGenerator[i].GetChecksum();
			}

			return base.GetChecksum() + Cell_Subdivision * 1000 + width * 100 + NumCores * 10 + MinimumID + MaximumID
				+details + Shrink + TexturePowerUV3 + TexturePowerUV4 + TexturePowerUV5 + TexturePowerUV6 + details + (GenerateCubeUV ? 0 : 1) +
				(AddBarycentricColors ? 0 : 1) + (RecalculateNormals ? 0 : 1) + NormalAngle + UVPower + TextureDimensionUV3 + TextureDimensionUV4 + TextureDimensionUV5 + TextureDimensionUV6;
		}
	}

}
