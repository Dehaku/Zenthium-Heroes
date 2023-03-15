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
using Unity.Collections.LowLevel.Unsafe;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	public unsafe class MarchingCubes_GPU_v2 : UniformGridVisualHull_SingleStep
	{

		

		[BeginInfo("MarchingCubes_GPUv2")]
		[InfoTitle("GPU based marching cubes", "This is the fastest hull generator currently possible. It combines the power of CPU and GPU to increase perfomance far beyond " +
		"CPU only based hull generators. " +
			"\n\n V2 is about 10x faster than the previous one.", "MarchingCubes_GPUv2")]
		[InfoSection1("How to use:", "Functionality is same as the CPU based marching cubes. It is important to have the compute shaders (Marching Cubes and Clear Buffer) assigned. " +
			"\n\nIt is recommended to use a triplanar shader so expensive UV, Normals and Tangents calculations can be avoided. If you cannot use triplanar shaders, you can enable them.", "MarchingCubes_GPUv2")]
		[InfoSection2("Compatibility:", "" +
		"<b>Direct X 11:</b> This hull generator uses Compute Shader which may not be supported by your target system.\n" +
		"For more information check the official statement from Unity:\n\nhttps://docs.unity3d.com/Manual/class-ComputeShader.html\n", "MarchingCubes_GPUv2")]		
		[InfoText("GPU based marching cubes:", "MarchingCubes_GPUv2")]
		[Range(0, 255)]
		[Tooltip("The ID value at which voxel is considered solid.")]
		public int SurfacePoint = 128;
		
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
		public ComputeShader m_marchingCubes;
		
		NativeCreateUniformGrid[] calculators;

		public NativeArray<float> UniformGrid;


		//The size of the buffer that holds the verts.
		//This is the maximum number of verts that the 
		//marching cube can produce, 5 triangles for each voxel.
		private int SIZE;

		ComputeBuffer voxeldata;
		ComputeBuffer _vertexBuffer;
		ComputeBuffer _normalBuffer;
	
		private Vector3[] verticesVerts;
		private Vector3[] normalsVerts;
		private int[] indexVerts;
	
		ComputeBuffer _triangleTable;
		ComputeBuffer _counterBuffer;
		ComputeBuffer _counterBufferResult;

		GPUToMesh_v2[] dataconverters;
		int BorderedWidth;

		public int TargetLOD;
		private List<int> LODSize;
		private static int[] counter = new int[1];

		protected override void Initialize()
		{
			if (width < 4) width = 8;
			width = width - width % 4;

			base.Initialize();
		}


		protected override void initializeCalculators()
		{
			m_marchingCubes = Resources.Load<ComputeShader>("Voxelica_MarchingCubes_v2");

			LODSize = new List<int>();

			int lodsize = width;
			while (lodsize >= 4)
			{
				LODSize.Add(lodsize);
				lodsize = lodsize / 2;
			}


			SIZE = width * width * width * 3 * 5;
			BorderedWidth = width + 3;
			UniformGrid = ContainerStaticLibrary.GetArray_float(BorderedWidth * BorderedWidth * BorderedWidth);

			calculators = new NativeCreateUniformGrid[NumCores];
			dataconverters = new GPUToMesh_v2[NumCores];
			for (int i = 0; i < calculators.Length; i++)
			{
				calculators[i].Width = width;				
				calculators[i].data = engine.Data[0];
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
				dataconverter.verts = ContainerStaticLibrary.GetVertexBank("MARCHINGCUBES_GPU", SIZE, i, NumCores);
				dataconverters[i] = dataconverter;
			}

			normalsVerts = new Vector3[SIZE];
			verticesVerts = new Vector3[SIZE];
			indexVerts = new int[SIZE];
			for (int i = 0; i < SIZE; i++)
			{
				indexVerts[i] = i;
			}

			_vertexBuffer = ContainerStaticLibrary.GetComputeBuffer("VertexBuffer", SIZE, sizeof(float) * 3);
			_normalBuffer = ContainerStaticLibrary.GetComputeBuffer("NormalBuffer", SIZE, sizeof(float) * 3);
			
			//Holds the voxel values, generated from perlin noise.
			voxeldata = ContainerStaticLibrary.GetComputeBuffer("VoxelBuffer", BorderedWidth * BorderedWidth * BorderedWidth, sizeof(float));
			voxeldata.IsValid();

			// Marching cubes triangle table
			_triangleTable = ContainerStaticLibrary.GetComputeBuffer("TriangleTable", 256, sizeof(ulong));
			_triangleTable.SetData(MarchingCubesTables.PaulBourkeTriangleTable);

			// Buffer for triangle counting
			_counterBuffer = ContainerStaticLibrary.GetComputeBuffer("CounterBuffer", 1, 4, ComputeBufferType.Counter);
			_counterBufferResult = ContainerStaticLibrary.GetComputeBuffer("CounterBufferResult", 1, sizeof(int));
			
		}

		protected override void beginCalculation(int m, int index, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{
			int lod = Mathf.Clamp(TargetLOD, 0, LODSize.Count - 1);
			voxelSize = voxelSize * Mathf.Pow(2, lod);

			int lodwidth = LODSize[lod];
			SIZE = lodwidth * lodwidth * lodwidth * 3 * 5;
			BorderedWidth = lodwidth + 3;
			int LODLength = BorderedWidth * BorderedWidth * BorderedWidth;

			calculators[m].Width = BorderedWidth;
			calculators[m].Shrink = Shrink;			
			calculators[m].voxelSize = voxelSize;	
			calculators[m].positionoffset = new Vector3(startX, startY, startZ);	
			
			m_JobHandles[m] = calculators[m].Schedule(LODLength, LODLength / SystemInfo.processorCount);
			//m_JobHandles[m] = calculators[m].Schedule(UniformGrid.Length, 10000);
			m_JobHandles[m].Complete();

			
			

			
			voxeldata.SetData(UniformGrid);		
			counter[0] = 0;

			_counterBufferResult.SetData(counter);
			m_marchingCubes.SetInt("_BlockWidth", BorderedWidth);
		
			m_marchingCubes.SetVector("_Positionoffset", new Vector4(startX, startY, startZ, 0));
			m_marchingCubes.SetFloat("_VoxelSize", voxelSize);
			RunCompute(voxeldata, SurfacePoint, lodwidth);

			
			_counterBufferResult.GetData(counter);
			int meshsize = (Mathf.Clamp( counter[0] - Destruction, 0, counter[0])) * 3;
	
			GPUToMesh_v2 dataconverter = dataconverters[m];

			//_Buffer.GetData(verts);
			_vertexBuffer.GetData(verticesVerts, 0, 0, meshsize);
			_normalBuffer.GetData(normalsVerts, 0, 0, meshsize);


			//dataconverter.verts.CopyFrom(verts);
			dataconverter.managedVertexArray = (IntPtr) UnsafeUtility.AddressOf(ref verticesVerts[0]);
			dataconverter.managedTriangleArray = (IntPtr)UnsafeUtility.AddressOf(ref indexVerts[0]);
			dataconverter.managedNormalArray = (IntPtr)UnsafeUtility.AddressOf(ref normalsVerts[0]);
			
			dataconverter.meshSize = meshsize;
			dataconverter.meshSize_triangles = meshsize;
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
		}

		void RunCompute(ComputeBuffer voxelsdata, float target, int lodwidth)
		{
			_counterBuffer.SetCounterValue(0);
			//Hull generation
			m_marchingCubes.SetFloat("_Target", target);


			m_marchingCubes.SetBuffer(0, "counterBuffer", _counterBufferResult);
			m_marchingCubes.SetBuffer(0, "TriangleTable", _triangleTable);
			m_marchingCubes.SetBuffer(0, "Voxels", voxelsdata);
			m_marchingCubes.SetBuffer(0, "_VertexBuffer", _vertexBuffer);
			m_marchingCubes.SetBuffer(0, "_NormalBuffer", _normalBuffer);
			m_marchingCubes.SetBuffer(0, "Counter", _counterBuffer);
			m_marchingCubes.Dispatch(0, lodwidth / 4, lodwidth / 4, lodwidth / 4);


			//Finalize the buffer count.
			m_marchingCubes.SetBuffer(1, "counterBuffer", _counterBufferResult);
			m_marchingCubes.SetBuffer(1, "Counter", _counterBuffer);

			m_marchingCubes.Dispatch(1, 1, 1, 1);
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

		protected override float GetChecksum()
		{
			float details = 0 + Destruction;

			BasicSurfaceModifier.RemoveDuplicates(DetailGenerator);
			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if(DetailGenerator[i])
				details += DetailGenerator[i].GetChecksum();
			}

			return base.GetChecksum() + Cell_Subdivision * 1000 + width * 100 + NumCores * 10 + SurfacePoint
				+details + Shrink + TexturePowerUV3 + TexturePowerUV4 + TexturePowerUV5 + TexturePowerUV6 + details + (GenerateCubeUV ? 0 : 1) + (GenerateSphereUV ? 0 : 1) +
				(AddBarycentricColors ? 0 : 1) + (RecalculateNormals ? 0 : 1) + NormalAngle + UVPower + TextureDimensionUV3 + TextureDimensionUV4 + TextureDimensionUV5 + TextureDimensionUV6;
		}

		public override void UpdateLOD(int newLOD)
		{
			base.UpdateLOD(newLOD);
			if (TargetLOD != newLOD)
			{
				TargetLOD = newLOD;
				engine.Rebuild();
			}
		}
	}

}
