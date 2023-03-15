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
using Fraktalia.Core.Math;

namespace Fraktalia.VoxelGen.Visualisation
{
	public unsafe class MarchingCubes_GPU_Experimental : UniformGridVisualHull_SingleStep_Async
	{
		[TitleText("Experimental:", TitleTextType.Title)]
		[InfoPanel("Information:", "This hull generator directly manipulates the mesh on the GPU which allows the highest generation efficiency possible. However since the mesh remain on the GPU, collision detection is not posssible.", "#ff0000ff", "#ffffffff")]
		[Range(0, 10)]
		public int SurfaceDimension = 0;
		[Range(0, 255)] [Tooltip("The ID value at which voxel is considered solid.")]
		public int SurfacePoint = 128;

		[Tooltip("Budget in vertices for each mesh. Make it as high as needed in order to render your desired geometry.")]
		[Range(500, 200000)]
		public int BUDGET = 50000;

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

		NativeCreateUniformGrid_V2 calculators;

		public NativeArray<float> UniformGrid;


	
		private int SIZE;
		

		ComputeBuffer voxeldata;	
		ComputeBuffer _triangleTable;
		ComputeBuffer _counterBufferResult;


		int BorderedWidth;
		public int TargetLOD;
		private List<int> LODSize;
		private static int[] counter = new int[1];
		private JobHandle m_JobHandles;
		protected override void Initialize()
		{	
			width = Mathf.ClosestPowerOfTwo(width);
			BUDGET = BUDGET - BUDGET % 8;
			Cell_Subdivision = Mathf.ClosestPowerOfTwo(Cell_Subdivision);
			if (width < 4) width = 8;
			base.Initialize();
			NoCollision = true;
		}


		protected override void initializeCalculators()
		{
			m_marchingCubes = Resources.Load<ComputeShader>("Voxelica_MarchingCubes_v3");
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

			calculators = new NativeCreateUniformGrid_V2();


			calculators.Width = width;

			if (SurfaceDimension < engine.Data.Length)
			{
				calculators.data = engine.Data[SurfaceDimension];
			}
			else
			{
				calculators.data = engine.Data[0];
			}

			calculators.UniformGridResult = UniformGrid;


			//Holds the voxel values, generated from perlin noise.
			voxeldata = ContainerStaticLibrary.GetComputeBuffer("VoxelBuffer", BorderedWidth * BorderedWidth * BorderedWidth, sizeof(float));
			voxeldata.IsValid();

			// Marching cubes triangle table
			_triangleTable = ContainerStaticLibrary.GetComputeBuffer("TriangleTable", 256, sizeof(ulong));
			_triangleTable.SetData(MarchingCubesTables.PaulBourkeTriangleTable);


			_counterBufferResult = ContainerStaticLibrary.GetComputeBuffer("CounterBufferResult", 1, sizeof(int));

			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].AllocateMesh(BUDGET * 3);

				_counterBufferResult.SetData(counter);
				VoxelMeshes[i]._counterBuffer.SetCounterValue(0);
				m_marchingCubes.SetBuffer(1, "_VertexBuffer", VoxelMeshes[i]._vertexBuffer);
				m_marchingCubes.SetBuffer(1, "_IndexBuffer", VoxelMeshes[i]._indexBuffer);
				m_marchingCubes.SetBuffer(1, "Counter", VoxelMeshes[i]._counterBuffer);
				m_marchingCubes.SetBuffer(1, "counterBuffer", _counterBufferResult);
				m_marchingCubes.Dispatch(1, BUDGET / 8, 1, 1);
			}
		}

		protected override IEnumerator beginCalculationasync(Queue<int> WorkerQueue, float cellSize, float voxelSize)
		{
			int cellindex = WorkerQueue.Dequeue();
			int i = cellindex % Cell_Subdivision;
			int j = (cellindex - i) / Cell_Subdivision % Cell_Subdivision;
			int k = ((cellindex - i) / Cell_Subdivision - j) / Cell_Subdivision;
			float startX = i * cellSize;
			float startY = j * cellSize;
			float startZ = k * cellSize;

			int lod = Mathf.Clamp(TargetLOD, 0, LODSize.Count - 1);
			voxelSize = voxelSize * Mathf.Pow(2, lod);
			int lodwidth = LODSize[lod];
			SIZE = lodwidth * lodwidth * lodwidth * 3 * 5;
			BorderedWidth = lodwidth + 3;
			int LODLength = BorderedWidth * BorderedWidth * BorderedWidth;

			Vector3Int offset = new Vector3Int();
			offset.x = NativeVoxelTree.ConvertLocalToInner(startX, engine.RootSize);
			offset.y = NativeVoxelTree.ConvertLocalToInner(startY, engine.RootSize);
			offset.z = NativeVoxelTree.ConvertLocalToInner(startZ, engine.RootSize);
			calculators.positionoffset = offset;		
			calculators.Width = BorderedWidth;
			calculators.Shrink = (int)Shrink;
			int innerVoxelSize = NativeVoxelTree.ConvertLocalToInner(voxelSize, engine.RootSize);
			
			calculators.voxelSizeBitPosition = MathUtilities.RightmostBitPosition(innerVoxelSize);

			m_JobHandles = calculators.Schedule(LODLength, LODLength / SystemInfo.processorCount);

			while (!m_JobHandles.IsCompleted)
			{
				yield return new YieldInstruction();
			}
			m_JobHandles.Complete();

			
			

		
			voxeldata.SetData(UniformGrid);		
			counter[0] = 0;

		
			m_marchingCubes.SetInt("_BlockWidth", BorderedWidth);
		
			m_marchingCubes.SetVector("_Positionoffset", new Vector4(startX, startY, startZ, 0));
			m_marchingCubes.SetFloat("_VoxelSize", voxelSize);
			RunCompute(voxeldata, SurfacePoint, lodwidth, cellindex);

			Bounds bound = new Bounds();
			bound.SetMinMax(new Vector3(startX, startY, startZ), new Vector3(startX, startY, startZ) + Vector3.one * cellSize);

			if(VoxelMeshes[cellindex].voxelMesh)
			VoxelMeshes[cellindex].voxelMesh.bounds = bound;

			yield return null;

			

		}


		void RunCompute(ComputeBuffer voxelsdata, float target, int lodwidth, int meshIndex)
		{
			_counterBufferResult.SetData(counter);

			VoxelMeshes[meshIndex]._counterBuffer.SetCounterValue(0);
			//Hull generation
			m_marchingCubes.SetFloat("_Target", target);
			m_marchingCubes.SetFloat("MaxTriangle", BUDGET);

			m_marchingCubes.SetBuffer(0, "TriangleTable", _triangleTable);
			m_marchingCubes.SetBuffer(0, "Voxels", voxelsdata);
			m_marchingCubes.SetBuffer(0, "_VertexBuffer", VoxelMeshes[meshIndex]._vertexBuffer);
			m_marchingCubes.SetBuffer(0, "_IndexBuffer", VoxelMeshes[meshIndex]._indexBuffer);
			m_marchingCubes.SetBuffer(0, "Counter", VoxelMeshes[meshIndex]._counterBuffer);
			m_marchingCubes.SetBuffer(0, "counterBuffer", _counterBufferResult);
			m_marchingCubes.Dispatch(0, lodwidth / 4, lodwidth / 4, lodwidth / 4);

			//Finalize the buffer count.
			m_marchingCubes.SetBuffer(2, "counterBuffer", _counterBufferResult);
			m_marchingCubes.SetBuffer(2, "Counter", VoxelMeshes[meshIndex]._counterBuffer);
			m_marchingCubes.Dispatch(2, 1, 1, 1);

			// Clear unused area of the buffers.
			m_marchingCubes.SetBuffer(1, "_VertexBuffer", VoxelMeshes[meshIndex]._vertexBuffer);
			m_marchingCubes.SetBuffer(1, "_IndexBuffer", VoxelMeshes[meshIndex]._indexBuffer);
			m_marchingCubes.SetBuffer(1, "Counter", VoxelMeshes[meshIndex]._counterBuffer);
			m_marchingCubes.SetBuffer(1, "counterBuffer", _counterBufferResult);
			m_marchingCubes.Dispatch(1, BUDGET / 8, 1, 1);

			
		}


		protected override void cleanUpCalculation()
		{
			m_JobHandles.Complete();	
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

			return base.GetChecksum() + Cell_Subdivision * 1000 + width * 100 * 10 + SurfacePoint + SurfaceDimension + BUDGET +
				+ details + Shrink + TexturePowerUV3 + TexturePowerUV4 + TexturePowerUV5 + TexturePowerUV6 + details + (GenerateCubeUV ? 0 : 1) + (GenerateSphereUV ? 0 : 1) +
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

		public override void OnDuplicate()
		{
			
			var pieces = GetComponentsInChildren<VoxelPiece>(true);
			for (int i = 0; i < pieces.Length; i++)
			{
				pieces[i].voxelMesh = null;
				pieces[i].meshfilter.sharedMesh = null;
			}

			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].AllocateMesh(BUDGET * 3);	
				VoxelMeshes[i]._counterBuffer.SetCounterValue(0);				
			}

		}
	}

}
