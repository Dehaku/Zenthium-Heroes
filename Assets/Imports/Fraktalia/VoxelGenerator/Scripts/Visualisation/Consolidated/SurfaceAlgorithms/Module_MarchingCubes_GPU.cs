using System.Collections;
using UnityEngine;
using Unity.Collections;
using System;
using UnityEngine.Rendering;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Utility;
using Unity.Collections.LowLevel.Unsafe;


#if UNITY_EDITOR
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
    [System.Serializable]
	public unsafe class Module_MarchingCubes_GPU : HullGenerationModule
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
		[InfoTextKey("GPU based marching cubes:", "MarchingCubes_GPUv2")]

		[Range(0, 255)]
		[Tooltip("The ID value at which voxel is considered solid.")]
		public int SurfacePoint = 128;

		[HideInInspector]
		public ComputeShader m_marchingCubes;

		ComputeBuffer[] voxeldata;
		ComputeBuffer[] _vertexnormalCombinedBuffer;


		[NativeDisableContainerSafetyRestriction]
		private NativeArray<Vector3>[] verticesnormalsCombined;

		[NativeDisableContainerSafetyRestriction]
		private NativeArray<int> indexVerts;

		ComputeBuffer _triangleTable;
		ComputeBuffer[] _counterBuffer;
		ComputeBuffer[] _counterBufferResult;
		ComputeBuffer[] _positionOffsetBuffer;
		Vector3[][] positionoffset;

		int BorderedWidth;



		private int[][] counter;
		private NativeArray<int>[] ncounter;	

		private int width;
		private int NumCores;
		private VoxelGenerator engine;

		private int SIZE;
		protected override void initializeModule()
		{
			width = HullGenerator.width;
			NumCores = HullGenerator.CurrentNumCores;
			engine = HullGenerator.engine;

			m_marchingCubes = Resources.Load<ComputeShader>("Voxelica_MarchingCubes_v4");

			SIZE = width * width * width * 3 * 5;
			BorderedWidth = width + 3;

			indexVerts = ContainerStaticLibrary.GetRisingNativeIntArray("Index", SIZE);

			counter = new int[NumCores][];
			ncounter = new NativeArray<int>[NumCores];
			voxeldata = new ComputeBuffer[NumCores];
			_counterBuffer = new ComputeBuffer[NumCores];
			_counterBufferResult = new ComputeBuffer[NumCores];
			_positionOffsetBuffer = new ComputeBuffer[NumCores];
			positionoffset = new Vector3[NumCores][];
			
			_vertexnormalCombinedBuffer = new ComputeBuffer[NumCores];
			verticesnormalsCombined = new NativeArray<Vector3>[NumCores];

			for (int i = 0; i < NumCores; i++)
			{
				counter[i] = new int[1];
				positionoffset[i] = new Vector3[1];

				ncounter[i] = ContainerStaticLibrary.GetArray_int(1, i);
				_vertexnormalCombinedBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("NormalVertexBuffer_" + i, (SIZE * 2) + 1, sizeof(float) * 3);
				voxeldata[i] = ContainerStaticLibrary.GetComputeBuffer("VoxelBuffer_" + i, BorderedWidth * BorderedWidth * BorderedWidth, sizeof(float));
				_counterBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("CounterBuffer_" + i, 1, 4, ComputeBufferType.Counter);
				_counterBufferResult[i] = ContainerStaticLibrary.GetComputeBuffer("CounterBufferResult_" + i, 1, sizeof(int));
				_positionOffsetBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("PositionOffsetBuffer_" + i, 1, sizeof(float) * 3);
				verticesnormalsCombined[i] = ContainerStaticLibrary.GetVertexArray((SIZE * 2) + 1, i);
			}

			// Marching cubes triangle table
			_triangleTable = ContainerStaticLibrary.GetComputeBuffer("TriangleTable", 256, sizeof(ulong));
			_triangleTable.SetData(MarchingCubesTables.PaulBourkeTriangleTable);
		}

		public override IEnumerator beginCalculationasync(float cellSize, float voxelSize)
		{
			voxelSize = voxelSize * Mathf.Pow(2, HullGenerator.TargetLOD);
			int lodwidth = HullGenerator.LODSize[HullGenerator.TargetLOD];
			SIZE = lodwidth * lodwidth * lodwidth * 3 * 5;
			BorderedWidth = lodwidth + 3;
			int LODLength = BorderedWidth * BorderedWidth * BorderedWidth;
			int innerVoxelSize = NativeVoxelTree.ConvertLocalToInner(voxelSize, engine.RootSize);

			for (int coreindex = 0; coreindex < HullGenerator.activeCores; coreindex++)
			{
				int cellIndex = HullGenerator.activeCells[coreindex];
				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.GenerateGeometry) continue;
			
				NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];
				_positionOffsetBuffer[coreindex].SetData(data.positionOffset);

				voxeldata[coreindex].SetData(HullGenerator.UniformGrid[coreindex]);
				counter[coreindex][0] = 0;
				_counterBufferResult[coreindex].SetData(counter[coreindex]);
				m_marchingCubes.SetInt("_BlockWidth", BorderedWidth);
				m_marchingCubes.SetFloat("_VoxelSize", voxelSize);
				RunCompute(voxeldata[coreindex], SurfacePoint, lodwidth, coreindex);
			}

			for (int coreindex = 0; coreindex < HullGenerator.activeCores; coreindex++)
			{
				int cellIndex = HullGenerator.activeCells[coreindex];
				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.GenerateGeometry) continue;


				Vector3 counts;

				AsyncGPUReadbackRequest requestverts = AsyncGPUReadback.RequestIntoNativeArray(ref verticesnormalsCombined[coreindex], _vertexnormalCombinedBuffer[coreindex]);
				while (!requestverts.done)
				{
					if (HullGenerator.synchronitylevel < 0) break;
					yield return new YieldInstruction();
				}
				requestverts.WaitForCompletion();

				counts = verticesnormalsCombined[coreindex][(SIZE * 2)];
				int verticecount = (int)counts.x;
				int meshsize = (Mathf.Clamp(verticecount, 0, verticecount)) * 3;

				NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];
				data.verticeArray_original.AddRange(verticesnormalsCombined[coreindex].GetSubArray(0, meshsize));
				data.normalArray_original.AddRange(verticesnormalsCombined[coreindex].GetSubArray(SIZE, meshsize));
				data.triangleArray_original.AddRange(indexVerts.GetSubArray(0, meshsize));
				
			}

			yield return null;
		}

		void RunCompute(ComputeBuffer voxelsdata, float target, int lodwidth, int coreindex)
		{
			_counterBuffer[coreindex].SetCounterValue(0);
			//Hull generation
			m_marchingCubes.SetFloat("_Target", target);
			m_marchingCubes.SetInt("_Size", SIZE);
			m_marchingCubes.SetBuffer(coreindex, "counterBuffer", _counterBufferResult[coreindex]);
			m_marchingCubes.SetBuffer(coreindex, "TriangleTable", _triangleTable);
			m_marchingCubes.SetBuffer(coreindex, "Voxels", voxelsdata);
			m_marchingCubes.SetBuffer(coreindex, "_VertexNormalBuffer", _vertexnormalCombinedBuffer[coreindex]);
			m_marchingCubes.SetBuffer(coreindex, "Counter", _counterBuffer[coreindex]);
			m_marchingCubes.SetBuffer(coreindex, "PositionOffset", _positionOffsetBuffer[coreindex]);

			m_marchingCubes.Dispatch(coreindex, lodwidth / 4, lodwidth / 4, lodwidth / 4);

			//Finalize the buffer count.
			m_marchingCubes.SetBuffer(coreindex + 8, "_VertexNormalBuffer", _vertexnormalCombinedBuffer[coreindex]);
			m_marchingCubes.SetBuffer(coreindex + 8, "counterBuffer", _counterBufferResult[coreindex]);
			m_marchingCubes.SetBuffer(coreindex + 8, "Counter", _counterBuffer[coreindex]);
			m_marchingCubes.Dispatch(coreindex + 8, 1, 1, 1);
		}

		public override float GetChecksum()
		{
			return SurfacePoint;
		}
	}
}