using Fraktalia.Core.Collections;
using NativeCopyFast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fraktalia.VoxelGen.Visualisation
{
	[ExecuteInEditMode]
	public class VoxelPiece : MonoBehaviour
	{
		public Mesh voxelMesh;
		public MeshFilter meshfilter;
		public MeshCollider meshcollider;
		public MeshRenderer meshrenderer;

		[NonSerialized]
		public List<Vector3> vertices = new List<Vector3>();
		[NonSerialized]
		public List<int> triangles = new List<int>();
		[NonSerialized]
		public List<Vector3> normals = new List<Vector3>();
		[NonSerialized]
		public List<Vector4> tangents = new List<Vector4>();
		[NonSerialized]
		public List<Color> colors = new List<Color>();
		[NonSerialized]
		public List<Color32> colors32 = new List<Color32>();
		[NonSerialized]
		public List<List<Vector2>> uvs = new List<List<Vector2>>();

		public GraphicsBuffer _vertexBuffer;
		public GraphicsBuffer _indexBuffer;
		public ComputeBuffer _counterBuffer;
		public bool isGPU = false;
		public bool supressClear = false;

		//last time this chunk was modified. Can be used by anything which reads the mesh and needs to update. 
		double _timeOfLastChange;
		public double timeOfLastChange => _timeOfLastChange;

		public void Initialize()
		{
			if (!meshfilter) meshfilter = GetComponent<MeshFilter>();
			if (!meshcollider) meshcollider = GetComponent<MeshCollider>();
			if (!meshrenderer) meshrenderer = GetComponent<MeshRenderer>();

			for (int i = 0; i < 8; i++)
			{
				uvs.Add(new List<Vector2>());
			}
		}

		public void Clear()
		{
			if (isGPU) return;
			if(supressClear)
			{
				supressClear = false;
				return;
			}
			if(voxelMesh != null)
			voxelMesh.Clear();
		}

		public void SetVertices(FNativeList<Vector3> verticeArray)
		{
			vertices.Clear();
			voxelMesh.SetVertices(verticeArray.AsArray());
			_timeOfLastChange = Time.realtimeSinceStartupAsDouble;
		}

		public void SetVertices(NativeArray<Vector3> verticeArray)
		{
			vertices.Clear();
			voxelMesh.SetVertices(verticeArray);
			_timeOfLastChange = Time.realtimeSinceStartupAsDouble;
		}


		public void SetTriangles(FNativeList<int> triangleArray)
		{
			triangles.Clear();
			NativeUtility.NativeListToList(triangleArray, triangles);
			voxelMesh.SetTriangles(triangles, 0);
		}

		public void SetTriangles(NativeArray<int> triangleArray)
		{
			triangles.Clear();
			NativeUtility.NativeListToList(triangleArray, triangles);
			voxelMesh.SetTriangles(triangles, 0);
		}

		public void SetNormals(FNativeList<Vector3> normalArray)
		{					
			voxelMesh.SetNormals(normalArray.AsArray());
		}

		public void SetNormals(NativeArray<Vector3> normalArray)
		{
			voxelMesh.SetNormals(normalArray);
		}

		public void SetTangents(FNativeList<Vector4> tangentsArray)
		{			
			voxelMesh.SetTangents(tangentsArray.AsArray());
		}

		public void SetUVs(int channel, FNativeList<Vector2> uvArray)
		{	
			voxelMesh.SetUVs(channel, uvArray.AsArray());
		}

		public void SetColors(FNativeList<Color> colorArray)
		{		
			voxelMesh.SetColors(colorArray.AsArray());
		}

		public void SetColors(FNativeList<Color32> colorArray)
		{		
			voxelMesh.SetColors(colorArray.AsArray());
		}

		public void EnableCollision(bool enable)
		{
			
			if (enable && voxelMesh.bounds.extents.sqrMagnitude > 0 && voxelMesh.vertexCount > 0)
			{
				meshcollider.sharedMesh = voxelMesh;
			}
			else
			{
				meshcollider.sharedMesh = null;
			}	
		}	

		private void OnDestroy()
		{
			

			DestroyImmediate(voxelMesh);
		}

		public Mesh.MeshDataArray AllocateMeshData()
		{
			return Mesh.AllocateWritableMeshData(1);
		}

		public void ApplyMeshDataArray(Mesh.MeshDataArray data)
		{
			Mesh.ApplyAndDisposeWritableMeshData(data, voxelMesh);
		}

		public void AllocateMesh(int vertexCount)
		{
			isGPU = true;
			voxelMesh = new Mesh();

#if UNITY_2021_2_OR_NEWER
            // We want GraphicsBuffer access as Raw (ByteAddress) buffers.
            voxelMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
			voxelMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

			// Vertex position: float32 x 3
			var vp = new VertexAttributeDescriptor
			  (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

			// Vertex normal: float32 x 3
			var vn = new VertexAttributeDescriptor
			  (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

			// Vertex/index buffer formats
			voxelMesh.SetVertexBufferParams(vertexCount, vp, vn);
			voxelMesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);

			// Submesh initialization
			voxelMesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount),
							 MeshUpdateFlags.DontRecalculateBounds);

			meshfilter.sharedMesh = voxelMesh;
			// GraphicsBuffer references
			_vertexBuffer = voxelMesh.GetVertexBuffer(0);
			_indexBuffer = voxelMesh.GetIndexBuffer();
			_counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
#else
			throw new NotSupportedException("You are using a version below 2021.0 which does not support this functionality. Asset downward compatibility is possible but only up to a degree.");
#endif


		}

		public void ClearBuffers()
		{
			if (isGPU)
			{
				isGPU = false;
				if(_vertexBuffer != null)
				_vertexBuffer.Release();

				if (_indexBuffer != null)
					_indexBuffer.Release();

				if (_counterBuffer != null)
					_counterBuffer.Release();
			}
		}

	}
}
