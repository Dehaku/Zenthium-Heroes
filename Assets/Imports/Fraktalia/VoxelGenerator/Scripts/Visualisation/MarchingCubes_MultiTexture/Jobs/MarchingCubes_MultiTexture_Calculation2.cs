using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{

	[BurstCompile]
	public struct MarchingCubes_MultiTexture_Calculation2 : IJob
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

		public float TexturePowerUV3;
		public float TexturePowerUV4;
		public float TexturePowerUV5;
		public float TexturePowerUV6;

		[NativeDisableParallelForRestriction]
		private NativeArray<float> Cube;

		[NativeDisableParallelForRestriction]
		private NativeArray<Vector3> EdgeVertex;

		public Vector3 positionoffset;
		public float voxelSize;
		public float cellSize;

		public float Surface;

		public float minimumID;
		public float maximumID;

		public float UVPower;
		public float SmoothingAngle;
		public int MaxBlocks;

		public float Shrink;

		private int Width;


		public FNativeList<Vector3> verticeArray;
		public FNativeList<int> triangleArray;
		public FNativeList<Vector2> uvArray;

		//UV2 is reserved by Unity for Baked Light
		public FNativeList<Vector2> uv3Array;
		public FNativeList<Vector2> uv4Array;
		public FNativeList<Vector2> uv5Array;
		public FNativeList<Vector2> uv6Array;

		public FNativeList<Vector3> normalArray;
		public FNativeList<Vector4> tangentsArray;
		public FNativeList<Vector3> tan1;
		public FNativeList<Vector3> tan2;
		public FNativeList<Color> colorArray;

		[ReadOnly]
		public NativeArray<int> VertexOffset;
		[ReadOnly]
		public NativeArray<int> EdgeConnection;
		[ReadOnly]
		public NativeArray<float> EdgeDirection;
		[ReadOnly]
		public NativeArray<int> CubeEdgeFlags;
		[ReadOnly]
		public NativeArray<int> TriangleConnectionTable;

		public FNativeMultiHashMap<Vector3, VertexEntry> dictionary;
		public FNativeList<Vector3> dictionaryKeys;
		public FNativeList<Vector3> triNormals;

		[BurstDiscard]
		public void Init(int width, float surface = 0.5f)
		{
			CleanUp();

			Surface = surface;



			Width = width;

			int blocks = Width * Width * Width;
			MaxBlocks = blocks;

			verticeArray = new FNativeList<Vector3>(0, Allocator.Persistent);
			triangleArray = new FNativeList<int>(0, Allocator.Persistent);
			uvArray = new FNativeList<Vector2>(0, Allocator.Persistent);



			uv3Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			uv4Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			uv5Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			uv6Array = new FNativeList<Vector2>(0, Allocator.Persistent);





			normalArray = new FNativeList<Vector3>(0, Allocator.Persistent);
			tangentsArray = new FNativeList<Vector4>(0, Allocator.Persistent);

			tan1 = new FNativeList<Vector3>(0, Allocator.Persistent);
			tan2 = new FNativeList<Vector3>(0, Allocator.Persistent);

			Cube = new NativeArray<float>(8, Allocator.Persistent);
			EdgeVertex = new NativeArray<Vector3>(12, Allocator.Persistent);

			dictionary = new FNativeMultiHashMap<Vector3, VertexEntry>(0, Allocator.Persistent);
			dictionaryKeys = new FNativeList<Vector3>(0, Allocator.Persistent);
			triNormals = new FNativeList<Vector3>(0, Allocator.Persistent);
			colorArray = new FNativeList<Color>(0, Allocator.Persistent);
		}

		public void Execute()
		{
			verticeArray.Clear();
			triangleArray.Clear();
			uvArray.Clear();
			uv3Array.Clear();
			uv4Array.Clear();
			uv5Array.Clear();
			uv6Array.Clear();
			tangentsArray.Clear();
			normalArray.Clear();
			tan1.Clear();
			tan2.Clear();
			colorArray.Clear();
			int blocks = MaxBlocks;
			int count = 0;


			for (int index = 0; index < blocks; index++)
			{
				int x = index % Width;
				int y = (index - x) / Width % Width;
				int z = ((index - x) / Width - y) / Width;

				int i;
				int ix, iy, iz;


				//Get the values in the 8 neighbours which make up a cube
				for (i = 0; i < 8; i++)
				{
					ix = x + VertexOffset[i * 3 + 0];
					iy = y + VertexOffset[i * 3 + 1];
					iz = z + VertexOffset[i * 3 + 2];

					float fx = positionoffset.x + ix * voxelSize;
					float fy = positionoffset.y + iy * voxelSize;
					float fz = positionoffset.z + iz * voxelSize;


					float rawvalue = data._PeekVoxelId(fx, fy, fz, 10,Shrink);
					float val = (rawvalue - minimumID) / (maximumID - minimumID);
					val *= 2.0f;
					val -= 1.0f;

					Cube[i] = val;
				}

				//START MARCHING
				int j, vert;
				int flagIndex = 0;
				float offset = 0.0f;
				//Find which vertices are inside of the surface and which are outside
				for (i = 0; i < 8; i++) if (Cube[i] <= Surface) flagIndex |= 1 << i;
				//Find which edges are intersected by the surface
				int edgeFlags = CubeEdgeFlags[flagIndex];
				//If the cube is entirely inside or outside of the surface, then there will be no intersections
				if (edgeFlags == 0)
				{
					continue;
				}
				//Find the point of intersection of the surface with each edge
				for (i = 0; i < 12; i++)
				{
					//if there is an intersection on this edge
					if ((edgeFlags & (1 << i)) != 0)
					{
						int edgeconnection = EdgeConnection[i * 2 + 0];

						offset = GetOffset(Cube[edgeconnection], Cube[EdgeConnection[i * 2 + 1]]);

						Vector3 edge = EdgeVertex[i];
						edge.x = x + (VertexOffset[edgeconnection * 3 + 0] + offset * EdgeDirection[i * 3 + 0]);
						edge.y = y + (VertexOffset[edgeconnection * 3 + 1] + offset * EdgeDirection[i * 3 + 1]);
						edge.z = z + (VertexOffset[edgeconnection * 3 + 2] + offset * EdgeDirection[i * 3 + 2]);
						EdgeVertex[i] = edge;
					}
				}

				for (i = 0; i < 5; i++)
				{

					if (TriangleConnectionTable[flagIndex * 16 + 3 * i] < 0) break;

					for (j = 0; j < 3; j++)
					{
						vert = TriangleConnectionTable[flagIndex * 16 + 3 * i + j];
						Vector3 vertex = (EdgeVertex[vert] * voxelSize) + positionoffset;

						if (texturedata_UV3.IsCreated)
						{
							float texturevalue = texturedata_UV3._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, Shrink) * TexturePowerUV3;
							uv3Array.Add(new Vector2(texturevalue, texturevalue));
						}

						if (texturedata_UV4.IsCreated)
						{
							float texturevalue = texturedata_UV4._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, Shrink) * TexturePowerUV4;
							uv4Array.Add(new Vector2(texturevalue, texturevalue));
						}

						if (texturedata_UV5.IsCreated)
						{
							float texturevalue = texturedata_UV5._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, Shrink) * TexturePowerUV5;
							uv5Array.Add(new Vector2(texturevalue, texturevalue));
						}

						if (texturedata_UV6.IsCreated)
						{
							float texturevalue = texturedata_UV6._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, Shrink) * TexturePowerUV6;
							uv6Array.Add(new Vector2(texturevalue, texturevalue));
						}

						verticeArray.Add(vertex);
						triangleArray.Add(count);
						tan1.Add(new Vector3(0, 0, 0));
						tan2.Add(new Vector3(0, 0, 0));


						count++;
					}
				}
			}

			int trianglecount = triangleArray.Length;
			for (int i = 0; i < trianglecount; i += 3)
			{
				Vector3 v1 = verticeArray[i];
				Vector3 v2 = verticeArray[i + 1];
				Vector3 v3 = verticeArray[i + 2];
				Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
				normalArray.Add(normal);
				normalArray.Add(normal);
				normalArray.Add(normal);

				colorArray.Add(new Color(1, 0, 0));
				colorArray.Add(new Color(0, 1, 0));
				colorArray.Add(new Color(0, 0, 1));
			}


			CalculateCubemap(count);


			if (SmoothingAngle > 0)
				RecalculateNormals(SmoothingAngle);


			ExecuteTangents();
		}

		[BurstDiscard]
		public void CleanUp()
		{

			if (verticeArray.IsCreated) verticeArray.Dispose();
			if (triangleArray.IsCreated) triangleArray.Dispose();
			if (uvArray.IsCreated) uvArray.Dispose();
			if (uv3Array.IsCreated) uv3Array.Dispose();
			if (uv4Array.IsCreated) uv4Array.Dispose();
			if (uv5Array.IsCreated) uv5Array.Dispose();
			if (uv6Array.IsCreated) uv6Array.Dispose();

			if (normalArray.IsCreated) normalArray.Dispose();
			if (tangentsArray.IsCreated) tangentsArray.Dispose();
			if (tan1.IsCreated) tan1.Dispose();
			if (tan2.IsCreated) tan2.Dispose();

			
			if (Cube.IsCreated) Cube.Dispose();
			if (EdgeVertex.IsCreated) EdgeVertex.Dispose();

			if (dictionary.IsCreated) dictionary.Dispose();
			if (dictionaryKeys.IsCreated) dictionaryKeys.Dispose();
			if (triNormals.IsCreated) triNormals.Dispose();
			if (colorArray.IsCreated) colorArray.Dispose();
		}

		private float GetOffset(float v1, float v2)
		{
			float delta = v2 - v1;
			if (delta == 0.0f)
			{
				return Surface;
			}
			return (Surface - v1) / delta;
		}

		public void ExecuteTangents()
		{

			//variable definitions
			int triangleCount = triangleArray.Length;
			int vertexCount = verticeArray.Length;


			for (int a = 0; a < triangleCount; a += 3)
			{
				int i1 = triangleArray[a + 0];
				int i2 = triangleArray[a + 1];
				int i3 = triangleArray[a + 2];

				Vector3 v1 = verticeArray[i1];
				Vector3 v2 = verticeArray[i2];
				Vector3 v3 = verticeArray[i3];

				Vector2 w1 = uvArray[i1];
				Vector2 w2 = uvArray[i2];
				Vector2 w3 = uvArray[i3];

				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;

				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;

				float div = s1 * t2 - s2 * t1;
				float r = div == 0.0f ? 0.0f : 1.0f / div;

				Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;

				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
			}


			for (int a = 0; a < vertexCount; ++a)
			{
				Vector3 n = normalArray[a];
				Vector3 t = tan1[a];

				//Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
				//tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
				Vector3.OrthoNormalize(ref n, ref t);

				Vector4 output = new Vector4();
				output.x = t.x;
				output.y = t.y;
				output.z = t.z;

				output.w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
				tangentsArray.Add(output);

			}


		}

		public void RecalculateNormals(float angle)
		{
			dictionary.Clear();
			dictionaryKeys.Clear();
			triNormals.Clear();

			var cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

			// Holds the normal of each triangle in each sub mesh.
			//var triNormals = new Vector3[triangleArray.Length / 3];

			for (int i = 0; i < triangleArray.Length / 3; i++)
			{
				triNormals.Add(new Vector3(0, 0, 0));
			}


			var triangles = triangleArray;

			for (var i = 0; i < triangles.Length; i += 3)
			{
				int i1 = triangles[i];
				int i2 = triangles[i + 1];
				int i3 = triangles[i + 2];

				// Calculate the normal of the triangle
				Vector3 p1 = verticeArray[i2] - verticeArray[i1];
				Vector3 p2 = verticeArray[i3] - verticeArray[i1];
				Vector3 normal = Vector3.Cross(p1, p2).normalized;
				int triIndex = i / 3;
				triNormals[triIndex] = normal;



				VertexEntry entry;
				FNativeMultiHashMapIterator<Vector3> iter;
				Vector3 hash = verticeArray[i1];
				if (!dictionary.TryGetFirstValue(hash, out entry, out iter))
				{
					dictionaryKeys.Add(hash);
				}
				dictionary.Add(hash, new VertexEntry(triIndex, i1));

				hash = verticeArray[i2];
				if (!dictionary.TryGetFirstValue(hash, out entry, out iter))
				{
					dictionaryKeys.Add(hash);
				}
				dictionary.Add(hash, new VertexEntry(triIndex, i2));

				hash = verticeArray[i3];
				if (!dictionary.TryGetFirstValue(hash, out entry, out iter))
				{
					dictionaryKeys.Add(hash);
				}
				dictionary.Add(hash, new VertexEntry(triIndex, i3));

			}


			// Each entry in the dictionary represents a unique vertex position.
			for (int i = 0; i < dictionaryKeys.Length; i++)
			{
				FNativeMultiHashMapIterator<Vector3> it_i;
				FNativeMultiHashMapIterator<Vector3> it_j;
				VertexEntry lhsEntry;
				VertexEntry rhsEntry;
				bool hasvalue_I = dictionary.TryGetFirstValue(dictionaryKeys[i], out lhsEntry, out it_i);

				while (hasvalue_I)
				{
					bool hasvalue_J = dictionary.TryGetFirstValue(dictionaryKeys[i], out rhsEntry, out it_j);
					var sum = new Vector3();
					while (hasvalue_J)
					{
						if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
						{
							sum += triNormals[rhsEntry.TriangleIndex];
						}
						else
						{
							
							var dot = Vector3.Dot(
								triNormals[lhsEntry.TriangleIndex],
								triNormals[rhsEntry.TriangleIndex]);
							if (dot >= cosineThreshold)
							{
								sum += triNormals[rhsEntry.TriangleIndex];
							}
						}

						hasvalue_J = dictionary.TryGetNextValue(out rhsEntry, ref it_j);
					}

					normalArray[lhsEntry.VertexIndex] = sum.normalized;

					hasvalue_I = dictionary.TryGetNextValue(out lhsEntry, ref it_i);
				}
			}
		}

		public void CalculateCubemap(int count)
		{
			Vector2 output = new Vector2();
			for (int i = 0; i < count; i++)
			{
				Vector3 vertex = normalArray[i];
				float absX = Mathf.Abs(vertex.x);
				float absY = Mathf.Abs(vertex.y);
				float absZ = Mathf.Abs(vertex.z);
				int choosenX = 0;
				int choosenY = 0;
				int choosenZ = 0;
				if (absX > absY && absX > absZ)
				{
					choosenX = 1;
				}
				else if (absY > absZ)
				{
					choosenY = 1;
				}
				else
				{
					choosenZ = 1;
				}
				vertex = verticeArray[i];
				
				output = new Vector2();
				output += new Vector2(vertex.z, vertex.y) * choosenX;
				output += new Vector2(vertex.x, vertex.z) * choosenY;
				output += new Vector2(vertex.x, vertex.y) * choosenZ;

				Vector2 uv = output * UVPower;
			
				uvArray.Add(uv);
			}

		}


	}

	
}
