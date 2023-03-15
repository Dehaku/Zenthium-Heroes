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

namespace Fraktalia.VoxelGen.Visualisation
{
	public class MarchingCubes_Scientific : UniformGridVisualHull
	{
		[BeginInfo("Scientific Visualisation")]
		[InfoTitle("Scientific Visualisation", "This hull generator uses GPU based Marching Cubes and combines the power of CPU and GPU to increase perfomance far beyond " +
		"CPU only based hull generators.", "Scientific Visualisation")]
		[InfoSection1("How to use:", "Functionality is same as the CPU based marching cubes. It is important to have the compute shaders (Marching Cubes and Clear Buffer) assigned. " +
			"\n\nAlso this hull generator does not generate UV coordinates so a triplanar shader is recommended. Also it is important to have the histogramm curve defined as it is used " +
			"to manipulate the final outcome. The histogramm values go from 0-1 which is then remapped to 0-255 and multiplied into the voxel map. This allows the visualisation of specific " +
			"voxel values.\n\n" +
			"Also this hull generator has a visible boundary in order to see potential interior content withoug modifying the dataset. Also histogramm curves for each color can be used in order " +
			"to colorize important voxels. The color is written into the vertex color so a material which uses vertex colors (included) is required.", "Scientific Visualisation")]
		[InfoSection2("Compatibility:", "" +
		"<b>Direct X 11:</b> This hull generator uses Compute Shader which may not be supported by your target system.\n" +
		"For more information check the official statement from Unity:\n\nhttps://docs.unity3d.com/Manual/class-ComputeShader.html\n", "Scientific Visualisation")]
		[InfoText("Scientific Visualisation:", "Scientific Visualisation")]

		
		[Range(0, 255)]
		public float MinimumID;
		[Range(0, 255)]
		public float MaximumID;
		[Range(0, 5)]
		public int TargetDimension;
		public AnimationCurve HistogrammCurve = new AnimationCurve();
		public Bounds VisibleBoundary;
		[Space]
		public bool AddColors;
		public AnimationCurve HistogrammCurve_Red = new AnimationCurve();
		public AnimationCurve HistogrammCurve_Green = new AnimationCurve();
		public AnimationCurve HistogrammCurve_Blue = new AnimationCurve();


		[Header("Compute Shaders:")]
		public ComputeShader m_marchingCubes;
		public ComputeShader m_clearBuffer;


		private NativeCreateUniformGrid_Scientific VoxelMapToGPU;
		private NativeArray<float> UniformGrid;
		private int N;	
		private int SIZE;

		ComputeBuffer voxeldata;
		ComputeBuffer m_meshBuffer;	
		ComputeBuffer m_cubeEdgeFlags, m_triangleConnectionTable;
		
		private NativeArray<GPUVertex> nativeVerts;

		private GPUVertex[] verts;
		private GPUToMesh_Scientific dataconverter;


		
		private float[] Histogramm;
		private NativeArray<float> NativeHistogramm;
		private NativeArray<float> NativeHistogramm_Red;
		private NativeArray<float> NativeHistogramm_Green;
		private NativeArray<float> NativeHistogramm_Blue;


		private float rebuildsum = 0;
		
		private void OnDrawGizmosSelected()
		{
			if (!engine) return;
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(VisibleBoundary.center, VisibleBoundary.size);
			Gizmos.color = Color.blue;
			Gizmos.DrawWireCube(new Vector3(engine.RootSize, engine.RootSize, engine.RootSize) / 2, new Vector3(engine.RootSize, engine.RootSize, engine.RootSize));

			FixedUpdate();
		}

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
			if (!UniformGrid.IsCreated) UniformGrid = new NativeArray<float>(BlockWidth * BlockWidth * BlockWidth, Allocator.Persistent);


			TargetDimension = Mathf.Clamp(TargetDimension, 0, engine.DimensionCount - 1);


			VoxelMapToGPU.Init(BlockWidth);
			VoxelMapToGPU.data = engine.Data[TargetDimension];
			VoxelMapToGPU.UniformGridResult = UniformGrid;


			CreateHistogramm(HistogrammCurve,ref NativeHistogramm);
			CreateHistogramm(HistogrammCurve_Red,ref NativeHistogramm_Red);
			CreateHistogramm(HistogrammCurve_Green,ref NativeHistogramm_Green);
			CreateHistogramm(HistogrammCurve_Blue,ref NativeHistogramm_Blue);

			dataconverter.Histogramm_RED = NativeHistogramm_Red;
			dataconverter.Histogramm_GREEN = NativeHistogramm_Green;
			dataconverter.Histogramm_BLUE = NativeHistogramm_Blue;


			dataconverter.Init();
			dataconverter.SIZE = SIZE;
			nativeVerts = new NativeArray<GPUVertex>(SIZE, Allocator.Persistent);
			verts = new GPUVertex[SIZE];
			m_meshBuffer = new ComputeBuffer(SIZE, sizeof(float) * 7);

			//Holds the voxel values, generated from perlin noise.
			voxeldata = new ComputeBuffer(BlockWidth * BlockWidth * BlockWidth, sizeof(float));


			m_cubeEdgeFlags = new ComputeBuffer(256, sizeof(int));
			m_cubeEdgeFlags.SetData(MarchingCubesTables.CubeEdgeFlags);
			m_triangleConnectionTable = new ComputeBuffer(256 * 16, sizeof(int));
			m_triangleConnectionTable.SetData(MarchingCubesTables.TriangleConnectionTable);


			m_marchingCubes.SetBuffer(0, "_CubeEdgeFlags", m_cubeEdgeFlags);
			m_marchingCubes.SetBuffer(0, "_TriangleConnectionTable", m_triangleConnectionTable);
		}

		protected override void beginCalculation(int m, int index, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{
			VoxelMapToGPU.VisibleBoundary = VisibleBoundary;
			VoxelMapToGPU.Shrink = Shrink;			
			VoxelMapToGPU.voxelSize = cellSize / (width);	
			VoxelMapToGPU.positionoffset = new Vector3(startX, startY, startZ);	
			VoxelMapToGPU.minimumID = MinimumID;
			VoxelMapToGPU.maximumID = MaximumID;
			VoxelMapToGPU.Histogramm = NativeHistogramm;
			VoxelMapToGPU.Schedule(UniformGrid.Length, 1000).Complete();
					
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


		
			VoxelPiece piece = VoxelMeshes[index];
			piece.Clear();
			piece.supressClear = true;
			m_meshBuffer.GetData(verts);
			//Remove in 2019.3+ PERFORMANCE BOOST. saves 1.7ms in deep profile

			nativeVerts.CopyFrom(verts);

			dataconverter.data = engine.Data[TargetDimension];
			dataconverter.verts = nativeVerts;	
			dataconverter.Schedule().Complete();

			
			piece.SetVertices(dataconverter.verticeArray);
			piece.SetTriangles(dataconverter.triangleArray);
			piece.SetNormals(dataconverter.normalArray);
			if(AddColors)
			{
				piece.SetColors(dataconverter.colorArray);
			}
		}



		protected override void cleanUpCalculation()
		{
			if(UniformGrid.IsCreated) UniformGrid.Dispose();
			if (voxeldata != null)
			{
				voxeldata.Release();
				m_meshBuffer.Release();	
				m_cubeEdgeFlags.Release();
				m_triangleConnectionTable.Release();
			
			}

			if (nativeVerts.IsCreated) nativeVerts.Dispose();
			dataconverter.CleanUp();
			if (NativeHistogramm.IsCreated)
			{
				NativeHistogramm.Dispose();
				NativeHistogramm_Red.Dispose();
				NativeHistogramm_Green.Dispose();
				NativeHistogramm_Blue.Dispose();
			}
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
				+details + Shrink + TargetDimension;
		}

		private void CreateHistogramm(AnimationCurve curve, ref NativeArray<float> result)
		{
			if (Histogramm == null || Histogramm.Length != 256)
			{
				Histogramm = new float[256];
			}

			for (int i = 0; i < 256; i++)
			{
				float value = curve.Evaluate(i / 256.0f) * 256;
				Histogramm[i] = Mathf.Clamp(value, 0, 256);

			}

			if(!result.IsCreated)
			{
				result = new NativeArray<float>(Histogramm, Allocator.Persistent);
			}
			else
			{
				result.CopyFrom(Histogramm);
			}

			
		}

		private void FixedUpdate()
		{
			if (!engine || !engine.IsInitialized) return;

			float checksum = 0;
			for (int i = 0; i < 256; i++)
			{
				checksum += HistogrammCurve.Evaluate(i / 256.0f) * 256;
				checksum += HistogrammCurve_Red.Evaluate(i / 256.0f) * 256;
				checksum += HistogrammCurve_Green.Evaluate(i / 256.0f) * 256;
				checksum += HistogrammCurve_Blue.Evaluate(i / 256.0f) * 256;
			}

			checksum += AddColors ? 1 : 0;
			checksum += VisibleBoundary.min.sqrMagnitude + VisibleBoundary.max.sqrMagnitude;



			if (!rebuildsum.Equals(checksum))
			{
				if (engine.IsInitialized)
				{
					rebuildsum = checksum;
					CreateHistogramm(HistogrammCurve, ref NativeHistogramm);
					CreateHistogramm(HistogrammCurve_Red, ref NativeHistogramm_Red);
					CreateHistogramm(HistogrammCurve_Green, ref NativeHistogramm_Green);
					CreateHistogramm(HistogrammCurve_Blue, ref NativeHistogramm_Blue);
					engine.Rebuild();
				}
			}
		}
	}

}
