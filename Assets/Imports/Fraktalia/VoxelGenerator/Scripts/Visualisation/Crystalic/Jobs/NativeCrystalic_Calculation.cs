using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	[BurstCompile]
	public struct NativeCrystalic_Calculation : IJob
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

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
		public float Shrink;

		public NativeArray<int> MaxBlocks;
		private int Width;

		public FNativeList<Vector3> mesh_verticeArray;
		public FNativeList<int> mesh_triangleArray;
		public FNativeList<Vector2> mesh_uvArray;
		public FNativeList<Vector3> mesh_normalArray;
		public FNativeList<Vector4> mesh_tangentsArray;

		public FNativeList<Vector3> verticeArray;
		public FNativeList<int> triangleArray;
		public FNativeList<Vector2> uvArray;
		public FNativeList<Vector3> normalArray;
		public FNativeList<Vector4> tangentsArray;
		public FNativeList<Vector3> tan1;
		public FNativeList<Vector3> tan2;

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
		[ReadOnly]
		public NativeArray<Vector3> Permutations;

		public Vector3 Offset_min;
		public Vector3 Offset_max;
		public Vector3 ScaleMin;
		public Vector3 ScaleMax;
		public float ScaleFactor_min;
		public float ScaleFactor_max;
		public Vector3 rotation_min;
		public Vector3 rotation_max;
		public int RandomIndexOffset;

		public float Probability;

		public int UseBoxUV;
		public float UVPower;

		[BurstDiscard]
		public void Init(Mesh crystal, int width, float surface = 0.5f)
		{
			CleanUp();

			Surface = surface;



			Width = width;

			int blocks = Width * Width * Width;
			MaxBlocks = new NativeArray<int>(1, Allocator.Persistent);
			MaxBlocks[0] = blocks;

			verticeArray = new FNativeList<Vector3>(0, Allocator.Persistent);
			triangleArray = new FNativeList<int>(0, Allocator.Persistent);
			uvArray = new FNativeList<Vector2>(0, Allocator.Persistent);
			normalArray = new FNativeList<Vector3>(0, Allocator.Persistent);
			tangentsArray = new FNativeList<Vector4>(0, Allocator.Persistent);

			int vertcount = crystal.vertices.Length;
			int tricount = crystal.triangles.Length;
			mesh_verticeArray = new FNativeList<Vector3>(vertcount, Allocator.Persistent);
			mesh_triangleArray = new FNativeList<int>(tricount, Allocator.Persistent);
			mesh_uvArray = new FNativeList<Vector2>(vertcount, Allocator.Persistent);
			mesh_normalArray = new FNativeList<Vector3>(vertcount, Allocator.Persistent);
			mesh_tangentsArray = new FNativeList<Vector4>(vertcount, Allocator.Persistent);
			tan1 = new FNativeList<Vector3>(0, Allocator.Persistent);
			tan2 = new FNativeList<Vector3>(0, Allocator.Persistent);


			for (int i = 0; i < vertcount; i++)
			{
				mesh_verticeArray.Add(crystal.vertices[i]);
				mesh_uvArray.Add(crystal.uv[i]);
				mesh_normalArray.Add(crystal.normals[i]);
				mesh_tangentsArray.Add(crystal.tangents[i]);
			}

			for (int i = 0; i < tricount; i++)
			{
				mesh_triangleArray.Add(crystal.triangles[i]);
			}






			Cube = new NativeArray<float>(8, Allocator.Persistent);
			EdgeVertex = new NativeArray<Vector3>(12, Allocator.Persistent);
		}

		public void CreateRotationPermutation(int length)
		{
			Permutations = new NativeArray<Vector3>(length, Allocator.Persistent);
			for (int i = 0; i < length; i++)
			{
				Permutations[i] = Random.insideUnitSphere;
			}



		}


		public void Execute()
		{
			int permutationcount = Permutations.Length;

			verticeArray.Clear();
			triangleArray.Clear();
			uvArray.Clear();
			tangentsArray.Clear();
			normalArray.Clear();


			int blocks = MaxBlocks[0];


			int mesh_vertcount = mesh_verticeArray.Length;
			int mesh_tricount = mesh_triangleArray.Length;

			int count = 0;

			for (int index = 0; index < blocks; index++)
			{
				Vector3 random3 = Permutations[(index * 3 + RandomIndexOffset) % permutationcount];
				if (random3.y > Probability) continue;

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


					float rawvalue = data._PeekVoxelId(fx, fy, fz, 10, Shrink);
					float val = (rawvalue - minimumID) / (maximumID - minimumID);
					val *= 2.0f;
					val -= 1.0f;

					Cube[i] = val;
				}

								
				int flagIndex = 0;
				float offset = 0.0f;				
				for (i = 0; i < 8; i++) if (Cube[i] <= Surface) flagIndex |= 1 << i;
				
				int edgeFlags = CubeEdgeFlags[flagIndex];		
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
						offset = GetOffset(Cube[EdgeConnection[i * 2 + 0]], Cube[EdgeConnection[i * 2 + 1]]);

						Vector3 edge = EdgeVertex[i];
						edge.x = x + (VertexOffset[EdgeConnection[i * 2 + 0] * 3 + 0] + offset * EdgeDirection[i * 3 + 0]);
						edge.y = y + (VertexOffset[EdgeConnection[i * 2 + 0] * 3 + 1] + offset * EdgeDirection[i * 3 + 1]);
						edge.z = z + (VertexOffset[EdgeConnection[i * 2 + 0] * 3 + 2] + offset * EdgeDirection[i * 3 + 2]);
						EdgeVertex[i] = edge;
					}
				}

				float fx2 = positionoffset.x + x * voxelSize;
				float fy2 = positionoffset.y + y * voxelSize;
				float fz2 = positionoffset.z + z * voxelSize;

				Vector3 random = Permutations[(index + RandomIndexOffset) % permutationcount];
				Vector3 random2 = Permutations[(index * 2 + RandomIndexOffset) % permutationcount];



				float scale = Mathf.Lerp(ScaleFactor_min, ScaleFactor_max, random3.x);
				Vector3 vscale;
				vscale.x = Mathf.Lerp(ScaleMin.x, ScaleMax.x, random2.x);
				vscale.y = Mathf.Lerp(ScaleMin.y, ScaleMax.y, random2.y);
				vscale.z = Mathf.Lerp(ScaleMin.z, ScaleMax.z, random2.z);

				Vector3 randomoffset = new Vector3(0, 0, 0);
				randomoffset.x = Mathf.Lerp(Offset_min.x, Offset_max.x, random.x);
				randomoffset.y = Mathf.Lerp(Offset_min.y, Offset_max.y, random2.y);
				randomoffset.z = Mathf.Lerp(Offset_min.z, Offset_max.z, random3.z);


				Vector3 rotmax = rotation_max;
				rotmax.Scale(random);
				Vector3 rot = rotation_min + rotmax;

				Quaternion rotation = Quaternion.Euler(rot);

				for (int v = 0; v < mesh_vertcount; v++)
				{
					Vector3 vertex = mesh_verticeArray[v] + randomoffset;
					vertex.Scale(vscale * (voxelSize * scale));



					vertex = rotation * vertex;

					Vector4 tangent = mesh_tangentsArray[v];
					float w = tangent.w;
					tangent = rotation * (tangent);
					tangent.w = w;

					verticeArray.Add(vertex + new Vector3(fx2, fy2, fz2));
					uvArray.Add(mesh_uvArray[v]);
					tangentsArray.Add(tangent);
					normalArray.Add(rotation * mesh_normalArray[v]);
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				for (int t = 0; t < mesh_tricount; t++)
				{
					triangleArray.Add(count * mesh_vertcount + mesh_triangleArray[t]);
				}

				count++;

			}

			if (UseBoxUV == 1)
			{
				CalculateCubemap(uvArray.Length);
			}

			ExecuteTangents();
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
				uvArray[i] = (output * UVPower);
			}

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
				tangentsArray[a] = output;

			}


		}

		[BurstDiscard]
		public void CleanUp()
		{

			if (verticeArray.IsCreated) verticeArray.Dispose();
			if (triangleArray.IsCreated) triangleArray.Dispose();
			if (uvArray.IsCreated) uvArray.Dispose();
			if (normalArray.IsCreated) normalArray.Dispose();
			if (tangentsArray.IsCreated) tangentsArray.Dispose();
			if (mesh_verticeArray.IsCreated) mesh_verticeArray.Dispose();
			if (mesh_triangleArray.IsCreated) mesh_triangleArray.Dispose();
			if (mesh_uvArray.IsCreated) mesh_uvArray.Dispose();
			if (mesh_normalArray.IsCreated) mesh_normalArray.Dispose();
			if (mesh_tangentsArray.IsCreated) mesh_tangentsArray.Dispose();

			if (tan1.IsCreated) tan1.Dispose();
			if (tan2.IsCreated) tan2.Dispose();



			if (MaxBlocks.IsCreated) MaxBlocks.Dispose();

			if (Cube.IsCreated) Cube.Dispose();
			if (EdgeVertex.IsCreated) EdgeVertex.Dispose();

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



	}
}
