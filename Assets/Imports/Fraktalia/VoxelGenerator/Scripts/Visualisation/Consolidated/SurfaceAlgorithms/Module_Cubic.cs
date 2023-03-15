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
	public unsafe class Module_Cubic : HullGenerationModule
	{
		[Range(0, 255)]
		[Tooltip("The ID value at which voxel is considered solid.")]
		public int SurfacePoint = 128;
		[Range(0, 16)]
		[Tooltip("The rows of a palette sheet (usually 16)")]
		public int AtlasRows = 16;



		[HideInInspector]
		public ComputeShader m_marchingCubes;
		ComputeBuffer[] voxeldata;
		ComputeBuffer[] _vertexnormaluvTriangleCombinedBuffer;
		[NativeDisableContainerSafetyRestriction]
		private NativeArray<float>[] verticesnormaluvtrianglesCombined;	
		ComputeBuffer[] _counterBuffer;
		ComputeBuffer[] _counterBufferResult;
		ComputeBuffer[] _positionOffsetBuffer;
		Vector3[][] positionoffset;
		int BorderedWidth;
		private int[][] counter;
		private int width;
		private int NumCores;
		private VoxelGenerator engine;
		private int MaximumVertexCount;
		private int MaximumTriangleCount;
		private int Buffersize;
		protected override void initializeModule()
		{
			width = HullGenerator.width;
			NumCores = HullGenerator.CurrentNumCores;
			engine = HullGenerator.engine;
			m_marchingCubes = Resources.Load<ComputeShader>("Voxelica_AtlasCubes_v2");

			MaximumVertexCount = width * width * width * 24;
			MaximumTriangleCount = width * width * width * 36;
			Buffersize = (MaximumVertexCount * 2 * 3) + MaximumVertexCount * 2 + MaximumTriangleCount;

			BorderedWidth = width + 3;

			counter = new int[NumCores][];			
			voxeldata = new ComputeBuffer[NumCores];
			_counterBuffer = new ComputeBuffer[NumCores];
			_counterBufferResult = new ComputeBuffer[NumCores];
			_positionOffsetBuffer = new ComputeBuffer[NumCores];
			positionoffset = new Vector3[NumCores][];

			_vertexnormaluvTriangleCombinedBuffer = new ComputeBuffer[NumCores];
			verticesnormaluvtrianglesCombined = new NativeArray<float>[NumCores];
		
			for (int i = 0; i < NumCores; i++)
			{
				counter[i] = new int[1];
				positionoffset[i] = new Vector3[1];
				
				_vertexnormaluvTriangleCombinedBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("NormalVertexUVTriangleBuffer_" + i, Buffersize + 1, sizeof(float));
				verticesnormaluvtrianglesCombined[i] = ContainerStaticLibrary.GetArray_float(Buffersize + 1, i);
		
				voxeldata[i] = ContainerStaticLibrary.GetComputeBuffer("VoxelBuffer_" + i, BorderedWidth * BorderedWidth * BorderedWidth, sizeof(float));
				_counterBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("CounterBuffer_" + i, 1, 4, ComputeBufferType.Counter);
				_counterBufferResult[i] = ContainerStaticLibrary.GetComputeBuffer("CounterBufferResult_" + i, 1, sizeof(int));
				_positionOffsetBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("PositionOffsetBuffer_" + i, 1, sizeof(float) * 3);	
			}
		}

		public override IEnumerator beginCalculationasync(float cellSize, float voxelSize)
		{
			voxelSize = voxelSize * Mathf.Pow(2, HullGenerator.TargetLOD);
			int lodwidth = HullGenerator.LODSize[HullGenerator.TargetLOD];

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

				AsyncGPUReadbackRequest requestverts = AsyncGPUReadback.RequestIntoNativeArray(ref verticesnormaluvtrianglesCombined[coreindex], _vertexnormaluvTriangleCombinedBuffer[coreindex]);
				while (!requestverts.done)
				{
					if (HullGenerator.synchronitylevel < 0) break;
					yield return new YieldInstruction();
				}
				requestverts.WaitForCompletion();

				disassembleData(coreindex);						
			}

			yield return null;
		}

		private void disassembleData(int coreindex)
        {
			int cellIndex = HullGenerator.activeCells[coreindex];
			_counterBufferResult[coreindex].GetData(counter[coreindex]);
			float counts = verticesnormaluvtrianglesCombined[coreindex][Buffersize];
			

			int verticecount = (int)counts;
			int meshsize = (Mathf.Clamp(verticecount, 0, verticecount)) * 4;
			int meshSize_triangles = verticecount * 6;

			

			NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];

			NativeArray<Vector3> resultVertices = verticesnormaluvtrianglesCombined[coreindex].GetSubArray(0, meshsize * 3).Reinterpret<Vector3>(sizeof(float));
			NativeArray<Vector3> resultNormals = verticesnormaluvtrianglesCombined[coreindex].GetSubArray(MaximumVertexCount * 3, meshsize * 3).Reinterpret<Vector3>(sizeof(float));

			int triangleposition = MaximumVertexCount * 3 * 2 + MaximumVertexCount * 2;
			NativeArray<float> resultTriangles = verticesnormaluvtrianglesCombined[coreindex].GetSubArray(triangleposition, meshSize_triangles);
			NativeArray<int> resultTriangles_int = resultTriangles.Reinterpret<int>(sizeof(int));
			NativeArray<Vector2> resultUvs = verticesnormaluvtrianglesCombined[coreindex].GetSubArray(MaximumVertexCount * 3 * 2, meshsize * 2).Reinterpret<Vector2>(sizeof(float));

			data.verticeArray_original.AddRange(resultVertices);
			data.normalArray_original.AddRange(resultNormals);
			data.uvArray.AddRange(resultUvs);			
			data.triangleArray_original.AddRange(resultTriangles_int);
		}


		void RunCompute(ComputeBuffer voxelsdata, float target, int lodwidth, int coreindex)
		{
			_counterBuffer[coreindex].SetCounterValue(0);
			//Hull generation
			m_marchingCubes.SetFloat("_Target", target);
			m_marchingCubes.SetInt("_VertexSize", MaximumVertexCount);
			m_marchingCubes.SetInt("_TriangleSize", MaximumTriangleCount);
			m_marchingCubes.SetInt("AtlasRow", AtlasRows);
			
			m_marchingCubes.SetBuffer(coreindex, "counterBuffer", _counterBufferResult[coreindex]);			
			m_marchingCubes.SetBuffer(coreindex, "Voxels", voxelsdata);
			m_marchingCubes.SetBuffer(coreindex, "_VertexNormalBuffer", _vertexnormaluvTriangleCombinedBuffer[coreindex]);
			m_marchingCubes.SetBuffer(coreindex, "Counter", _counterBuffer[coreindex]);
			m_marchingCubes.SetBuffer(coreindex, "PositionOffset", _positionOffsetBuffer[coreindex]);
		
			m_marchingCubes.Dispatch(coreindex, lodwidth / 4, lodwidth / 4, lodwidth / 4);

			//Finalize the buffer count.
			m_marchingCubes.SetBuffer(coreindex + 8, "_VertexNormalBuffer", _vertexnormaluvTriangleCombinedBuffer[coreindex]);
			m_marchingCubes.SetBuffer(coreindex + 8, "counterBuffer", _counterBufferResult[coreindex]);
			m_marchingCubes.SetBuffer(coreindex + 8, "Counter", _counterBuffer[coreindex]);
			m_marchingCubes.Dispatch(coreindex + 8, 1, 1, 1);


		}

		public override float GetChecksum()
		{
			return SurfacePoint + AtlasRows;
		}
	}	
}