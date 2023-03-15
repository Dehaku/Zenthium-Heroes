using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System.Collections.Specialized;

namespace Fraktalia.VoxelGen.Visualisation
{
	[BurstCompile]
	public struct UVWriter_Calculation : IJob
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

		[NativeDisableParallelForRestriction]
		public NativeVoxelTree texturedata_UV3;

		[NativeDisableParallelForRestriction]
		public NativeVoxelTree texturedata_UV4;

		[NativeDisableParallelForRestriction]
		public NativeVoxelTree texturedata_UV5;

		[NativeDisableParallelForRestriction]
		public NativeVoxelTree texturedata_UV6;

		public Vector3 positionoffset;

		private int Width;

		public NativeArray<Vector3> mesh_verticeArray;
		public NativeArray<int> mesh_triangleArray;
		public NativeArray<Vector2> mesh_uvArray;
		public NativeArray<Vector3> mesh_normalArray;
		public NativeArray<Vector4> mesh_tangentsArray;

		[NativeDisableParallelForRestriction][NativeDisableContainerSafetyRestriction]
		public NativeArray<Vector2> uv3Array;
		[NativeDisableParallelForRestriction]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<Vector2> uv4Array;
		[NativeDisableParallelForRestriction]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<Vector2> uv5Array;
		[NativeDisableParallelForRestriction]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<Vector2> uv6Array;

		public NativeArray<Color> colorArray;
		public int CreateTesselationColors;


		[BurstDiscard]
		public void Init(Mesh referenceMesh, int width)
		{
			CleanUp();

		
			Width = width;

			int blocks = Width * Width * Width;		

			int vertcount = referenceMesh.vertices.Length;
			int tricount = referenceMesh.triangles.Length;
			mesh_verticeArray = new NativeArray<Vector3>(referenceMesh.vertices, Allocator.Persistent);
			mesh_triangleArray = new NativeArray<int>(referenceMesh.triangles, Allocator.Persistent);
			mesh_uvArray = new NativeArray<Vector2>(referenceMesh.uv, Allocator.Persistent);
			mesh_normalArray = new NativeArray<Vector3>(referenceMesh.normals, Allocator.Persistent);
			mesh_tangentsArray = new NativeArray<Vector4>(referenceMesh.tangents, Allocator.Persistent);

			uv3Array = new NativeArray<Vector2>(vertcount, Allocator.Persistent);
			uv4Array = new NativeArray<Vector2>(vertcount, Allocator.Persistent);
			uv5Array = new NativeArray<Vector2>(vertcount, Allocator.Persistent);
			uv6Array = new NativeArray<Vector2>(vertcount, Allocator.Persistent);

			colorArray = new NativeArray<Color>(vertcount, Allocator.Persistent);

			if (CreateTesselationColors == 1)
			{
				for (int i = 0; i < vertcount; i += 3)
				{
					colorArray[i] = new Color(1, 0, 0);
				}

				for (int i = 1; i < vertcount; i += 3)
				{
					colorArray[i] = new Color(0, 1, 0);
				}

				for (int i = 2; i < vertcount; i += 3)
				{
					colorArray[i] = new Color(0, 0, 1);
				}

			}
		}

		public void Execute()
		{		
			int mesh_vertcount = mesh_verticeArray.Length;
			
			float uv;

			if (texturedata_UV3.IsCreated)
			{
				for (int index = 0; index < mesh_vertcount; index++)
				{
					Vector3 vertexpos = mesh_verticeArray[index];					
					uv = (byte)texturedata_UV3._PeekVoxelId(vertexpos.x, vertexpos.y, vertexpos.z, 10, 0);
					uv3Array[index] = new Vector2(uv, uv);
				}
			}

			if (texturedata_UV4.IsCreated)
			{
				for (int index = 0; index < mesh_vertcount; index++)
				{
					Vector3 vertexpos = mesh_verticeArray[index];
					uv = (byte)texturedata_UV4._PeekVoxelId(vertexpos.x, vertexpos.y, vertexpos.z, 10, 0);
					uv4Array[index] = new Vector2(uv, uv);
				}
			}

			if (texturedata_UV5.IsCreated)
			{
				for (int index = 0; index < mesh_vertcount; index++)
				{
					Vector3 vertexpos = mesh_verticeArray[index];
					uv = (byte)texturedata_UV5._PeekVoxelId(vertexpos.x, vertexpos.y, vertexpos.z, 10, 0);
					uv5Array[index] = new Vector2(uv, uv);
				}
			}

			if (texturedata_UV6.IsCreated)
			{
				for (int index = 0; index < mesh_vertcount; index++)
				{
					Vector3 vertexpos = mesh_verticeArray[index];
					uv = (byte)texturedata_UV6._PeekVoxelId(vertexpos.x, vertexpos.y, vertexpos.z, 10, 0);
					uv6Array[index] = new Vector2(uv, uv);
				}
			}		
		}

		[BurstDiscard]
		public void CleanUp()
		{
			if (uv3Array.IsCreated) uv3Array.Dispose();
			if (uv4Array.IsCreated) uv4Array.Dispose();
			if (uv5Array.IsCreated) uv5Array.Dispose();
			if (uv6Array.IsCreated) uv6Array.Dispose();
			if (mesh_verticeArray.IsCreated) mesh_verticeArray.Dispose();
			if (mesh_triangleArray.IsCreated) mesh_triangleArray.Dispose();
			if (mesh_uvArray.IsCreated) mesh_uvArray.Dispose();
			if (mesh_normalArray.IsCreated) mesh_normalArray.Dispose();
			if (mesh_tangentsArray.IsCreated) mesh_tangentsArray.Dispose();
			if (colorArray.IsCreated) colorArray.Dispose();
		}
	}
}
