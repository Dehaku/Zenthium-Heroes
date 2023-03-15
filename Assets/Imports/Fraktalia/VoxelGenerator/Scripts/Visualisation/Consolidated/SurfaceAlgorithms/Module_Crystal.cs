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
	public unsafe class Module_Crystal : HullGenerationModule
	{
		const int MAXIMUM_VERTEXCOUNT = 50;

		[Range(0, 255)]
		[Tooltip("The ID value at which voxel is considered solid.")]
		public int SurfacePoint = 128;	
		public Mesh CrystalMesh;
		public PlacementManifest CrystalPositioning;
		public string PermutationSeed = "awdwadwa";

		ComputeBuffer mesh_verticeArray;
		ComputeBuffer mesh_triangleArray;
		ComputeBuffer mesh_normalArray;
		ComputeBuffer mesh_uvArray;
		int mesh_verticeCount;
		int mesh_triangleCount;


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

		private NativeArray<Vector3> Permutations;
		ComputeBuffer PermutationBuffer;

		private bool isValid;
		protected override void initializeModule()
		{
			isValid = false;
			if (CrystalMesh == null) return;
			if (CrystalMesh.vertexCount > MAXIMUM_VERTEXCOUNT)
			{
				Debug.LogError("Module Crystal ERROR: Crystal Mesh has to many vertices to be considered unsafe. " +
					"Limit is " + MAXIMUM_VERTEXCOUNT + " vertices. Your chosen one has: " + CrystalMesh.vertexCount + ". Affected object:"+ this.HullGenerator);
				return;
			}
			width = HullGenerator.width;
			NumCores = HullGenerator.CurrentNumCores;
			engine = HullGenerator.engine;
			m_marchingCubes = Resources.Load<ComputeShader>("Voxelica_Crystal");
	
			
			mesh_verticeCount = CrystalMesh.vertexCount;
			int[] triangles = CrystalMesh.triangles;
			mesh_triangleCount = triangles.Length;

			string mesh_checksum = "" + mesh_verticeCount + "" + mesh_triangleCount + CrystalMesh.bounds.size.x + CrystalMesh.bounds.size.y + CrystalMesh.bounds.size.z; 

			mesh_verticeArray = ContainerStaticLibrary.GetComputeBuffer("CrystalMesh_Vertex" + mesh_checksum, mesh_verticeCount, sizeof(Vector3));
			mesh_verticeArray.SetData(CrystalMesh.vertices);
			mesh_normalArray = ContainerStaticLibrary.GetComputeBuffer("CrystalMesh_Normals" + mesh_checksum, mesh_verticeCount, sizeof(Vector3));
			mesh_normalArray.SetData(CrystalMesh.normals);
			mesh_triangleArray = ContainerStaticLibrary.GetComputeBuffer("CrystalMesh_Triangle" + mesh_checksum, triangles.Length, sizeof(uint));
			mesh_triangleArray.SetData(triangles);
			mesh_uvArray = ContainerStaticLibrary.GetComputeBuffer("CrystalMesh_UV" + mesh_checksum, mesh_verticeCount, sizeof(Vector2));
			mesh_uvArray.SetData(CrystalMesh.uv);

			MaximumVertexCount = width * width * width * mesh_verticeCount;
			MaximumTriangleCount = width * width * width * mesh_triangleCount;
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

				_vertexnormaluvTriangleCombinedBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("NormalVertexUVTriangleBuffer_Crystal" + i, Buffersize + 1, sizeof(float));
				verticesnormaluvtrianglesCombined[i] = ContainerStaticLibrary.GetArray_float(Buffersize + 1, i);

				voxeldata[i] = ContainerStaticLibrary.GetComputeBuffer("VoxelBuffer_" + i, BorderedWidth * BorderedWidth * BorderedWidth, sizeof(float));
				_counterBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("CounterBuffer_" + i, 1, 4, ComputeBufferType.Counter);
				_counterBufferResult[i] = ContainerStaticLibrary.GetComputeBuffer("CounterBufferResult_" + i, 1, sizeof(int));
				_positionOffsetBuffer[i] = ContainerStaticLibrary.GetComputeBuffer("PositionOffsetBuffer_" + i, 1, sizeof(float) * 3);
			}

			Permutations = ContainerStaticLibrary.GetPermutationTable(PermutationSeed, 10000);
			PermutationBuffer = ContainerStaticLibrary.GetComputeBuffer(PermutationSeed, 10000, sizeof(Vector3));
			PermutationBuffer.SetData(Permutations);

			isValid = true;
		}

		public override IEnumerator beginCalculationasync(float cellSize, float voxelSize)
		{
			if (!isValid) yield return null;
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
			
			float counts = verticesnormaluvtrianglesCombined[coreindex][Buffersize];
			int verticecount = (int)counts;
			if (counts > 0)
			{
				int meshsize = (Mathf.Clamp(verticecount, 0, verticecount)) * mesh_verticeCount;
				int meshSize_triangles = verticecount * mesh_triangleCount;



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
		}

		void RunCompute(ComputeBuffer voxelsdata, float target, int lodwidth, int coreindex)
		{
			_counterBuffer[coreindex].SetCounterValue(0);
			//Hull generation
			m_marchingCubes.SetBuffer(coreindex, "mesh_verticeArray", mesh_verticeArray);
			m_marchingCubes.SetBuffer(coreindex, "mesh_triangleArray", mesh_triangleArray);
			m_marchingCubes.SetBuffer(coreindex, "mesh_normalArray", mesh_normalArray);
			m_marchingCubes.SetBuffer(coreindex, "mesh_uvArray", mesh_uvArray);
			m_marchingCubes.SetInt("mesh_verticeCount", mesh_verticeCount);
			m_marchingCubes.SetInt("mesh_triangleCount", mesh_triangleCount);
			
			m_marchingCubes.SetFloat("ScaleFactor_min", CrystalPositioning.ScaleFactor_min);
			m_marchingCubes.SetFloat("ScaleFactor_max", CrystalPositioning.ScaleFactor_max);
			m_marchingCubes.SetVector("Offset_min", CrystalPositioning.Offset_min);
			m_marchingCubes.SetVector("Offset_max", CrystalPositioning.Offset_max);
			m_marchingCubes.SetVector("Scale_min", CrystalPositioning.Scale_min);
			m_marchingCubes.SetVector("Scale_max", CrystalPositioning.Scale_max);
			m_marchingCubes.SetVector("Rotation_min", CrystalPositioning.Rotation_min);
			m_marchingCubes.SetVector("Rotation_max", CrystalPositioning.Rotation_max);

			m_marchingCubes.SetBuffer(coreindex, "PermutationBuffer", PermutationBuffer);
			//Hull generation
			m_marchingCubes.SetFloat("_Target", target);
			m_marchingCubes.SetInt("_VertexSize", MaximumVertexCount);
			m_marchingCubes.SetInt("_TriangleSize", MaximumTriangleCount);
			
			//m_marchingCubes.SetBuffer(coreindex, "counterBuffer", _counterBufferResult[coreindex]);
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
			if (CrystalMesh == null) return 0;

			return SurfacePoint + CrystalPositioning._GetChecksum() + CrystalMesh.bounds.size.sqrMagnitude + CrystalMesh.vertexCount;
		}      
    }
}