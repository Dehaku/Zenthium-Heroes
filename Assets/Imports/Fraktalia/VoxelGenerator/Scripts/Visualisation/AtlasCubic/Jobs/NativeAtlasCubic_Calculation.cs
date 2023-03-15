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
	public struct NativeAtlasCubic_Calculation : IJob
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

		[NativeDisableParallelForRestriction]
		private NativeArray<int> Neighbours;


		public Vector3 positionoffset;
		public float voxelSize;
		public float cellSize;


		public int AtlasRow;


		public float SmoothingAngle;
		public NativeArray<int> MaxBlocks;
		private int Width;


		public FNativeList<Vector3> verticeArray;
		public FNativeList<int> triangleArray;
		public FNativeList<Vector2> uvArray;
		public FNativeList<Vector3> normalArray;
		public FNativeList<Vector4> tangentsArray;
		public FNativeList<Vector3> tan1;
		public FNativeList<Vector3> tan2;

		[ReadOnly]
		public NativeArray<int> VertexOffset;


		public FNativeMultiHashMap<Vector3, VertexEntry> dictionary;
		public FNativeList<Vector3> dictionaryKeys;
		public FNativeList<Vector3> triNormals;

		[BurstDiscard]
		public void Init(int width)
		{
			CleanUp();

			Width = width;

			int blocks = Width * Width * Width;
			MaxBlocks = new NativeArray<int>(1, Allocator.Persistent);
			MaxBlocks[0] = blocks;

			verticeArray = new FNativeList<Vector3>( Allocator.Persistent);
			triangleArray = new FNativeList<int>( Allocator.Persistent);
			uvArray = new FNativeList<Vector2>( Allocator.Persistent);
			normalArray = new FNativeList<Vector3>( Allocator.Persistent);
			tangentsArray = new FNativeList<Vector4>( Allocator.Persistent);

			tan1 = new FNativeList<Vector3>( Allocator.Persistent);
			tan2 = new FNativeList<Vector3>( Allocator.Persistent);

			Neighbours = new NativeArray<int>(6, Allocator.Persistent);


			dictionary = new FNativeMultiHashMap<Vector3, VertexEntry>(0, Allocator.Persistent);
			dictionaryKeys = new FNativeList<Vector3>( Allocator.Persistent);
			triNormals = new FNativeList<Vector3>( Allocator.Persistent);

		}

		public void Execute()
		{
			verticeArray.Clear();
			triangleArray.Clear();
			uvArray.Clear();
			tangentsArray.Clear();
			normalArray.Clear();
			tan1.Clear();
			tan2.Clear();

			int blocks = MaxBlocks[0];

			float halfcell = cellSize / 2;
			int hullPosition = 0;

			float xhalf = voxelSize / 2;
			float yhalf = voxelSize / 2;
			float zhalf = voxelSize / 2;

			Vector3 blockoffset = positionoffset + new Vector3(xhalf, xhalf, xhalf);

			//SpriteAtlas
			float uvoffset = 1.0f / AtlasRow;
			int subdivision = 256 / (AtlasRow * AtlasRow);

			for (int index = 0; index < blocks; index++)
			{
				int x = index % Width;
				int y = (index - x) / Width % Width;
				int z = ((index - x) / Width - y) / Width;

				int i;
				int ix, iy, iz;

				float fx = positionoffset.x + x * voxelSize + xhalf;
				float fy = positionoffset.y + y * voxelSize + yhalf;
				float fz = positionoffset.z + z * voxelSize + zhalf;

				Vector3 voxeloffset = new Vector3(x, y, z) * voxelSize;
				Vector3 offset = blockoffset + voxeloffset;

				int centerID = data._PeekVoxelId(fx, fy, fz, 10);

				if (centerID == 0) continue;



				//Get the values in the 6 neighbours which make up a cube
				for (i = 0; i < 6; i++)
				{
					ix = x + VertexOffset[i * 3 + 0];
					iy = y + VertexOffset[i * 3 + 1];
					iz = z + VertexOffset[i * 3 + 2];

					fx = positionoffset.x + ix * voxelSize + xhalf;
					fy = positionoffset.y + iy * voxelSize + yhalf;
					fz = positionoffset.z + iz * voxelSize + zhalf;

					Neighbours[i] = data._PeekVoxelId(fx, fy, fz, 10);
				}


				int atlaspos_X = centerID / subdivision;//2
				int atlaspos_Y = atlaspos_X / AtlasRow;//1

				atlaspos_X %= AtlasRow;//1

				float uvoffset_X = uvoffset * atlaspos_X;
				float uvoffset_XMax = uvoffset_X + uvoffset;
				float uvoffset_Y = uvoffset * atlaspos_Y;
				float uvoffset_YMax = uvoffset_Y + uvoffset;


				//links
				if (Neighbours[0] == 0)
				{
					verticeArray.Add(new Vector3(-xhalf, -yhalf, -zhalf) + offset);
					verticeArray.Add(new Vector3(-xhalf, yhalf, -zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, yhalf, -zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, -yhalf, -zhalf) + offset);

					uvArray.Add(new Vector2(uvoffset_X, uvoffset_Y));
					uvArray.Add(new Vector2(uvoffset_X, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_Y));

					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));

					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 1);
					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 3);
					triangleArray.Add(hullPosition * 4 + 0);

					hullPosition++;

					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				//rechts
				if (Neighbours[1] == 0)
				{
					verticeArray.Add(new Vector3(-xhalf, -yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(-xhalf, yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, -yhalf, zhalf) + offset);

					uvArray.Add(new Vector2(uvoffset_X, uvoffset_Y));
					uvArray.Add(new Vector2(uvoffset_X, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_Y));

					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));

					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 1);
					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 3);
					triangleArray.Add(hullPosition * 4 + 2);

					hullPosition++;

					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				//oben
				if (Neighbours[2] == 0)
				{
					verticeArray.Add(new Vector3(-xhalf, -yhalf, -zhalf) + offset);
					verticeArray.Add(new Vector3(-xhalf, -yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(-xhalf, yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(-xhalf, yhalf, -zhalf) + offset);

					uvArray.Add(new Vector2(uvoffset_X, uvoffset_Y));
					uvArray.Add(new Vector2(uvoffset_X, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_Y));

					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));

					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 1);
					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 3);
					triangleArray.Add(hullPosition * 4 + 0);
					hullPosition++;

					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				//unten
				if (Neighbours[3] == 0)
				{
					verticeArray.Add(new Vector3(xhalf, -yhalf, -zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, -yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, yhalf, -zhalf) + offset);

					uvArray.Add(new Vector2(uvoffset_X, uvoffset_Y));
					uvArray.Add(new Vector2(uvoffset_X, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_Y));

					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));

					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 1);
					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 3);
					triangleArray.Add(hullPosition * 4 + 2);
					hullPosition++;

					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				//hinten
				if (Neighbours[4] == 0)
				{
					verticeArray.Add(new Vector3(-xhalf, -yhalf, -zhalf) + offset);
					verticeArray.Add(new Vector3(-xhalf, -yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, -yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, -yhalf, -zhalf) + offset);

					uvArray.Add(new Vector2(uvoffset_X, uvoffset_Y));
					uvArray.Add(new Vector2(uvoffset_X, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_Y));

					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));

					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 1);
					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 3);
					triangleArray.Add(hullPosition * 4 + 2);
					hullPosition++;

					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				//vorne
				if (Neighbours[5] == 0)
				{
					verticeArray.Add(new Vector3(-xhalf, yhalf, -zhalf) + offset);
					verticeArray.Add(new Vector3(-xhalf, yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, yhalf, zhalf) + offset);
					verticeArray.Add(new Vector3(xhalf, yhalf, -zhalf) + offset);

					uvArray.Add(new Vector2(uvoffset_X, uvoffset_Y));
					uvArray.Add(new Vector2(uvoffset_X, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_YMax));
					uvArray.Add(new Vector2(uvoffset_XMax, uvoffset_Y));

					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));
					normalArray.Add(new Vector3(0, 0, 0));

					triangleArray.Add(hullPosition * 4 + 0);
					triangleArray.Add(hullPosition * 4 + 1);
					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 2);
					triangleArray.Add(hullPosition * 4 + 3);
					triangleArray.Add(hullPosition * 4 + 0);
					hullPosition++;

					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

			}

			int trianglecount = triangleArray.Length;
			for (int i = 0; i < trianglecount; i += 3)
			{
				int vertindex0 = triangleArray[i];
				int vertindex1 = triangleArray[i + 1];
				int vertindex2 = triangleArray[i + 2];


				Vector3 v1 = verticeArray[vertindex0];
				Vector3 v2 = verticeArray[vertindex1];
				Vector3 v3 = verticeArray[vertindex2];
				Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
				normalArray[vertindex0] = normal;
				normalArray[vertindex1] = normal;
				normalArray[vertindex2] = normal;
			}

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
			if (normalArray.IsCreated) normalArray.Dispose();
			if (tangentsArray.IsCreated) tangentsArray.Dispose();
			if (tan1.IsCreated) tan1.Dispose();
			if (tan2.IsCreated) tan2.Dispose();

			if (MaxBlocks.IsCreated) MaxBlocks.Dispose();

			if (Neighbours.IsCreated) Neighbours.Dispose();


			if (dictionary.IsCreated) dictionary.Dispose();
			if (dictionaryKeys.IsCreated) dictionaryKeys.Dispose();
			if (triNormals.IsCreated) triNormals.Dispose();
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
							// The dot product is the cosine of the angle between the two triangles.
							// A larger cosine means a smaller angle.
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

			dictionary.Clear();
			
			dictionaryKeys.Clear();
			dictionaryKeys.Capacity = 0;
			triNormals.Clear();
			triNormals.Capacity = 0;
		}

	}
}
