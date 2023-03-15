using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using Fraktalia.Core.Math;
using Fraktalia.Utility;
using UnityEngine.Rendering;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{

	[BurstCompile]
	public unsafe struct NativeCreateUniformGrid : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

		public Vector3 positionoffset;
		public float voxelSize;
		public float cellSize;

		public float Surface;

		public float UVPower;
		public float SmoothingAngle;
		public int MaxBlocks;
		public int Width;

		public float Shrink;

		


		[WriteOnly]
		public NativeArray<float> UniformGridResult;

		public void Execute(int index)
		{
			Vector3Int position = MathUtilities.Convert1DTo3D(index, Width, Width, Width);

			float fx = positionoffset.x + (position.x - 1) * voxelSize;
			float fy = positionoffset.y + (position.y - 1) * voxelSize;
			float fz = positionoffset.z + (position.z - 1) * voxelSize;
			float rawvalue = data._PeekVoxelId(fx, fy, fz, 10, Shrink);

			UniformGridResult[index] = rawvalue;
		}
	}

	[BurstCompile]
	public unsafe struct NativeCreateUniformGrid_V2 : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

		public Vector3Int positionoffset;
		public int voxelSizeBitPosition;
		
		public int MaxBlocks;
		public int Width;

		public int Shrink;




		[WriteOnly]
		public NativeArray<float> UniformGridResult;

		public void Execute(int index)
		{
			Vector3Int position = MathUtilities.Convert1DTo3D(index, Width, Width, Width);

			int fx = positionoffset.x + ((position.x - 1) << voxelSizeBitPosition);
			int fy = positionoffset.y + ((position.y - 1) << voxelSizeBitPosition);
			int fz = positionoffset.z + ((position.z - 1) << voxelSizeBitPosition);
			float rawvalue = data._PeekVoxelId_InnerCoordinate(fx, fy, fz, 10, Shrink);

			UniformGridResult[index] = rawvalue;
		}
	}


	[BurstCompile]
	public unsafe struct NativeCreateUniformGrid_Normalized : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;
		public Vector3 positionoffset;
		public float voxelSize;
		public float minimumID;
		public float maximumID;
		public float Shrink;
		private int Width;

		[WriteOnly]
		public NativeArray<float> UniformGridResult;

		[BurstDiscard]
		public void Init(int width)
		{
			Width = width;
		}

		public void Execute(int index)
		{
			Vector3Int position = MathUtilities.Convert1DTo3D(index, Width, Width, Width);

			float fx = positionoffset.x + (position.x -1) * voxelSize;
			float fy = positionoffset.y + (position.y -1) * voxelSize;
			float fz = positionoffset.z + (position.z -1) * voxelSize;

			float rawvalue = data._PeekVoxelId(fx, fy, fz, 10, Shrink);
			float val = (rawvalue - minimumID) / (maximumID - minimumID);
			val *= 2.0f;
			val -= 1.0f;
			UniformGridResult[index] = val;
		}
	}

	[BurstCompile]
	public unsafe struct NativeCreateUniformGrid_Scientific : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;
		public Vector3 positionoffset;
		public float voxelSize;
		public float minimumID;
		public float maximumID;
		public float Shrink;
		private int Width;


		[WriteOnly]
		public NativeArray<float> UniformGridResult;

		[ReadOnly]
		public NativeArray<float> Histogramm;

		public Bounds VisibleBoundary;

		[BurstDiscard]
		public void Init(int width)
		{
			Width = width;
		}

		public void Execute(int index)
		{
			Vector3Int position = MathUtilities.Convert1DTo3D(index, Width, Width, Width);

			float fx = positionoffset.x + (position.x-1) * voxelSize;
			float fy = positionoffset.y + (position.y-1) * voxelSize;
			float fz = positionoffset.z + (position.z-1) * voxelSize;

			int histogrammvalue = 0;
			float rawvalue = 0;
			if (!(fx < VisibleBoundary.min.x || fy < VisibleBoundary.min.y || fz < VisibleBoundary.min.z || fx > VisibleBoundary.max.x || fy > VisibleBoundary.max.y || fz > VisibleBoundary.max.z))
			{
				histogrammvalue = data._PeekVoxelId(fx, fy, fz, 10, Shrink);
				rawvalue = Histogramm[histogrammvalue];
			}

			float val = (rawvalue - minimumID) / (maximumID - minimumID);
			val *= 2.0f;
			val -= 1.0f;
			UniformGridResult[index] = val;
		}
	}

	[BurstCompile]
	public unsafe struct GPUToMesh : IJob
	{
		[ReadOnly]
		public NativeArray<GPUVertex> verts;

		public int SIZE;

	
		public FNativeList<Vector3> verticeArray;
		public FNativeList<int> triangleArray;
		public FNativeList<Vector3> normalArray;

		public int GenerateCubeUV;
		public int CalculateNormals;
		public int CalculateBarycentricColors;

		public FNativeList<Vector2> uvArray;		
		public FNativeList<Vector4> tangentsArray;
		public FNativeList<Vector3> tan1;
		public FNativeList<Vector3> tan2;

		public FNativeList<Vector2> uv3Array;
		public FNativeList<Vector2> uv4Array;
		public FNativeList<Vector2> uv5Array;
		public FNativeList<Vector2> uv6Array;

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

		public float UVPower;
		public float NormalAngle;
		public float Shrink;

		public FNativeMultiHashMap<Vector3, VertexEntry> dictionary;
		public FNativeList<Vector3> dictionaryKeys;
		public FNativeList<Vector3> triNormals;
		public FNativeList<Color> colorArray;

		[BurstDiscard]
		public void Init()
		{
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


			int idx = 0;
			GPUVertex vertexinformation;

			for (int i = 0; i < SIZE; i++)
			{
				vertexinformation = verts[i];

				if (vertexinformation.position.w != -1)
				{
					verticeArray.Add(vertexinformation.position);
					normalArray.Add(vertexinformation.normal);
					triangleArray.Add(idx++);
				}

			}

			int count = verticeArray.Length;

			for (int i = 0; i < count; i++)
			{
				Vector3 vertex = verticeArray[i];
				if (texturedata_UV3.IsCreated)
				{
					float texturevalue = texturedata_UV3._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV3;
					uv3Array.Add(new Vector2(texturevalue, texturevalue));
				}

				if (texturedata_UV4.IsCreated)
				{
					float texturevalue = texturedata_UV4._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV4;
					uv4Array.Add(new Vector2(texturevalue, texturevalue));
				}

				if (texturedata_UV5.IsCreated)
				{
					float texturevalue = texturedata_UV5._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV5;
					uv5Array.Add(new Vector2(texturevalue, texturevalue));
				}

				if (texturedata_UV6.IsCreated)
				{
					float texturevalue = texturedata_UV6._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV6;
					uv6Array.Add(new Vector2(texturevalue, texturevalue));
				}

				tan1.Add(new Vector3(0, 0, 0));
				tan2.Add(new Vector3(0, 0, 0));
			}

			if(CalculateNormals == 1)
			{
				RecalculateNormals(NormalAngle);
			}

			if (GenerateCubeUV == 1)
			{				
				CalculateCubemap(count);
				ExecuteTangents();
				
			}

			if(CalculateBarycentricColors == 1)
			{
				CreateBarycentricColors();
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
				tangentsArray.Add(output);

			}


		}

		public void CalculateCubemap(int count)
		{
			Vector2 output;
			for (int i = 0; i < count; i += 3)
			{
				Vector3 vertex1 = (verticeArray[i]);
				Vector3 vertex2 = (verticeArray[i + 1]);
				Vector3 vertex3 = (verticeArray[i + 2]);
				Vector3 vertex = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1).normalized;

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


				vertex = vertex1;
				output = new Vector2();
				output += new Vector2(vertex.z, vertex.y) * choosenX;
				output += new Vector2(vertex.x, vertex.z) * choosenY;
				output += new Vector2(vertex.x, vertex.y) * choosenZ;
				Vector2 uv = output * UVPower;
				uvArray.Add(uv);

				vertex = vertex2;
				output = new Vector2();
				output += new Vector2(vertex.z, vertex.y) * choosenX;
				output += new Vector2(vertex.x, vertex.z) * choosenY;
				output += new Vector2(vertex.x, vertex.y) * choosenZ;
				uv = output * UVPower;
				uvArray.Add(uv);

				vertex = vertex3;
				output = new Vector2();
				output += new Vector2(vertex.z, vertex.y) * choosenX;
				output += new Vector2(vertex.x, vertex.z) * choosenY;
				output += new Vector2(vertex.x, vertex.y) * choosenZ;
				uv = output * UVPower;
				uvArray.Add(uv);

				/*
				Vector3 n = (verticeArray[i] - Vector3.one*20).normalized;
				output.x = Mathf.Atan2(n.x, n.z) / (2 * Mathf.PI) + 0.5f;
				output.y = n.y * 0.5f + 0.5f;
				output2.x = Mathf.Atan2(n.y, n.z) / (2 * Mathf.PI) + 0.5f;
				output2.y = n.x * 0.5f + 0.5f;
				output3.x = Mathf.Atan2(n.x, n.y) / (2 * Mathf.PI) + 0.5f;
				output3.y = n.z * 0.5f + 0.5f;

				float u = (Mathf.Atan2(n.z, n.x) / (2f * Mathf.PI));
				float v = (Mathf.Asin(n.y) / Mathf.PI) + 0.5f;

				uvArray.Add(new Vector2(u,v)* UVPower);
				*/
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

		public void CreateBarycentricColors()
		{
			int count = verticeArray.Length;
			for (int i = 0; i < count; i+=3)
			{
				colorArray.Add(new Color(1, 0, 0));
				colorArray.Add(new Color(0, 1, 0));
				colorArray.Add(new Color(0, 0, 1));
			}
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

			if (dictionary.IsCreated) dictionary.Dispose();
			if (dictionaryKeys.IsCreated) dictionaryKeys.Dispose();
			if (triNormals.IsCreated) triNormals.Dispose();
			if (colorArray.IsCreated) colorArray.Dispose();
		}
	}

	[BurstCompile]
	public unsafe struct GPUToMesh_Scientific : IJob
	{
		[ReadOnly]
		public NativeArray<GPUVertex> verts;

		public int SIZE;

		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

		public FNativeList<Vector3> verticeArray;
		public FNativeList<int> triangleArray;
		public FNativeList<Vector3> normalArray;

		public int GenerateCubeUV;
		public int CalculateNormals;
		public int CalculateBarycentricColors;	

		public float TexturePowerUV3;
		public float TexturePowerUV4;
		public float TexturePowerUV5;
		public float TexturePowerUV6;

		public float UVPower;
		public float NormalAngle;
		public float Shrink;

		public FNativeMultiHashMap<Vector3, VertexEntry> dictionary;
		public FNativeList<Vector3> dictionaryKeys;
		public FNativeList<Vector3> triNormals;
		public FNativeList<Color> colorArray;

		[ReadOnly]
		public NativeArray<float> Histogramm_RED;
		[ReadOnly]
		public NativeArray<float> Histogramm_GREEN;
		[ReadOnly]
		public NativeArray<float> Histogramm_BLUE;


		[BurstDiscard]
		public void Init()
		{
			verticeArray = new FNativeList<Vector3>(0, Allocator.Persistent);
			triangleArray = new FNativeList<int>(0, Allocator.Persistent);			
			normalArray = new FNativeList<Vector3>(0, Allocator.Persistent);
			

			dictionary = new FNativeMultiHashMap<Vector3, VertexEntry>(0, Allocator.Persistent);
			dictionaryKeys = new FNativeList<Vector3>(0, Allocator.Persistent);
			triNormals = new FNativeList<Vector3>(0, Allocator.Persistent);
			colorArray = new FNativeList<Color>(0, Allocator.Persistent);
		}


		public void Execute()
		{
			verticeArray.Clear();
			triangleArray.Clear();
			normalArray.Clear();
			colorArray.Clear();


			int idx = 0;
			GPUVertex vertexinformation;

			for (int i = 0; i < SIZE; i++)
			{
				vertexinformation = verts[i];

				if (vertexinformation.position.w != -1)
				{
					verticeArray.Add(vertexinformation.position);
					normalArray.Add(vertexinformation.normal);
					triangleArray.Add(idx++);
				}

			}


			int count = verticeArray.Length;

			for (int i = 0; i < count; i++)
			{
				Vector3 vertex = verticeArray[i];
				int value = data._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0);
				colorArray.Add(new Color(Histogramm_RED[value], Histogramm_GREEN[value], Histogramm_BLUE[value]));
			}

		}

	
	

		

		[BurstDiscard]
		public void CleanUp()
		{
			if (verticeArray.IsCreated) verticeArray.Dispose();
			if (triangleArray.IsCreated) triangleArray.Dispose();
			if (normalArray.IsCreated) normalArray.Dispose();			
			if (dictionary.IsCreated) dictionary.Dispose();
			if (dictionaryKeys.IsCreated) dictionaryKeys.Dispose();
			if (triNormals.IsCreated) triNormals.Dispose();
			if (colorArray.IsCreated) colorArray.Dispose();
		}
	}

	[BurstCompile]
	public unsafe struct GPUToMesh_v2 : IJob
	{
		[ReadOnly]
		public NativeArray<GPUVertex> verts;

		public float rootSize;
		public int meshSize;
		public int meshSize_triangles;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr managedVertexArray;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr managedTriangleArray;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr managedNormalArray;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr managedUVArray;

		public FNativeList<Vector3> verticeArray;
		public FNativeList<int> triangleArray;
		public FNativeList<Vector3> normalArray;

		public int GenerateCubeUV;
		public int GenerateSphereUV;
		public int CalculateNormals;
		public int CalculateBarycentricColors;

		public FNativeList<Vector2> uvArray;
		public FNativeList<Vector4> tangentsArray;
		public FNativeList<Vector3> tan1;
		public FNativeList<Vector3> tan2;

		public FNativeList<Vector2> uv3Array;
		public FNativeList<Vector2> uv4Array;
		public FNativeList<Vector2> uv5Array;
		public FNativeList<Vector2> uv6Array;

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

		public float UVPower;
		public float NormalAngle;
		public float Shrink;

		public FNativeMultiHashMap<Vector3, VertexEntry> dictionary;
		public FNativeList<Vector3> dictionaryKeys;
		public FNativeList<Vector3> triNormals;
		public FNativeList<Color> colorArray;

		public int isDistinct;

		[BurstDiscard]
		public void Init()
		{
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

			dictionary = new FNativeMultiHashMap<Vector3, VertexEntry>(0, Allocator.Persistent);
			dictionaryKeys = new FNativeList<Vector3>(0, Allocator.Persistent);
			triNormals = new FNativeList<Vector3>(0, Allocator.Persistent);
			colorArray = new FNativeList<Color>(0, Allocator.Persistent);

			managedUVArray = IntPtr.Zero;
			isDistinct = 0;
		}


		public void Execute()
		{
			isDistinct = 0;
			verticeArray.Clear();
			normalArray.Clear();
			triangleArray.Clear();
			uvArray.Clear();
			uv3Array.Clear();
			uv4Array.Clear();
			uv5Array.Clear();
			uv6Array.Clear();
			tangentsArray.Clear();
			
			tan1.Clear();
			tan2.Clear();
			colorArray.Clear();

			if (meshSize == 0) return;

			verticeArray.AddRange(managedVertexArray.ToPointer(), meshSize);
			normalArray.AddRange(managedNormalArray.ToPointer(), meshSize);
			triangleArray.AddRange(managedTriangleArray.ToPointer(), meshSize_triangles);

			if (managedUVArray != IntPtr.Zero && !(GenerateCubeUV == 1 || GenerateSphereUV == 1))
				uvArray.AddRange(managedUVArray.ToPointer(), meshSize);


			int count = verticeArray.Length;

			if (texturedata_UV3.IsCreated)
			{
				
				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = verticeArray[i];
					float texturevalue = texturedata_UV3._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV3;
					uv3Array.Add(new Vector2(texturevalue, texturevalue));
				}
			}

			if (texturedata_UV4.IsCreated)
			{

				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = verticeArray[i];
					float texturevalue = texturedata_UV4._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV4;
					uv4Array.Add(new Vector2(texturevalue, texturevalue));
				}
			}

			if (texturedata_UV5.IsCreated)
			{
				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = verticeArray[i];
					float texturevalue = texturedata_UV5._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV3;
					uv3Array.Add(new Vector2(texturevalue, texturevalue));
				}
			}

			if (texturedata_UV6.IsCreated)
			{
				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = verticeArray[i];
					float texturevalue = texturedata_UV6._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV3;
					uv3Array.Add(new Vector2(texturevalue, texturevalue));
				}
			}
			

			if (CalculateNormals == 1)
			{
				RecalculateNormals(NormalAngle);
			}

			if (GenerateCubeUV == 1 || GenerateSphereUV == 1)
			{
				uvArray.Clear();

				for (int i = 0; i < count; i++)
				{
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				if(GenerateCubeUV == 1)
				{
					CalculateCubemap(count);
				}
				else
				{
					CalculateSpheremap(count);
				}


				ExecuteTangents();
			}

			if (CalculateBarycentricColors == 1)
			{
				CreateBarycentricColors();
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
				tangentsArray.Add(output);

			}


		}

		public void CalculateCubemap(int count)
		{

			if (verticeArray.Length % 3 == 0)
			{
				Vector2 output;
				for (int i = 0; i < count; i += 3)
				{
					Vector3 vertex1 = (verticeArray[i]);
					Vector3 vertex2 = (verticeArray[i + 1]);
					Vector3 vertex3 = (verticeArray[i + 2]);
					Vector3 vertex = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1).normalized;

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

					vertex = vertex1;
					output = new Vector2();
					output += new Vector2(vertex.z, vertex.y) * choosenX;
					output += new Vector2(vertex.x, vertex.z) * choosenY;
					output += new Vector2(vertex.x, vertex.y) * choosenZ;
					Vector2 uv = output * UVPower;
					uvArray.Add(uv);

					vertex = vertex2;
					output = new Vector2();
					output += new Vector2(vertex.z, vertex.y) * choosenX;
					output += new Vector2(vertex.x, vertex.z) * choosenY;
					output += new Vector2(vertex.x, vertex.y) * choosenZ;
					uv = output * UVPower;
					uvArray.Add(uv);

					vertex = vertex3;
					output = new Vector2();
					output += new Vector2(vertex.z, vertex.y) * choosenX;
					output += new Vector2(vertex.x, vertex.z) * choosenY;
					output += new Vector2(vertex.x, vertex.y) * choosenZ;
					uv = output * UVPower;
					uvArray.Add(uv);
				}
			}
			else
			{
				Vector2 output;
				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = (verticeArray[i]);
					
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
					else if (absY > absZ && absY > absX)
					{
						choosenY = 1;
					}
					else
					{
						choosenZ = 1;
					}
		
					output = new Vector2();
					output += new Vector2(vertex.z, vertex.y) * choosenX;
					output += new Vector2(vertex.x, vertex.z) * choosenY;
					output += new Vector2(vertex.x, vertex.y) * choosenZ;
					Vector2 uv = output * UVPower;
					uvArray.Add(uv);		
				}
			}
		}

		public void CalculateSpheremap(int count)
		{		
			for (int i = 0; i < count; i ++)
			{				
				Vector3 n = (verticeArray[i] - Vector3.one* rootSize * 0.5f).normalized;
				
				float u = (Mathf.Atan2(n.z, n.x) / (2f * Mathf.PI));
				float v = (Mathf.Asin(n.y) / Mathf.PI) + 0.5f;

				uvArray.Add(new Vector2(u,v)* UVPower);				
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

		public void CreateBarycentricColors()
		{
			int count = verticeArray.Length;
			for (int i = 0; i < count; i += 3)
			{
				colorArray.Add(new Color(1, 0, 0));
				colorArray.Add(new Color(0, 1, 0));
				colorArray.Add(new Color(0, 0, 1));
			}
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

			if (dictionary.IsCreated) dictionary.Dispose();
			if (dictionaryKeys.IsCreated) dictionaryKeys.Dispose();
			if (triNormals.IsCreated) triNormals.Dispose();
			if (colorArray.IsCreated) colorArray.Dispose();

			managedUVArray = IntPtr.Zero;
			managedVertexArray = IntPtr.Zero;
			managedTriangleArray = IntPtr.Zero;
			managedNormalArray = IntPtr.Zero;
		}
	}

	[BurstCompile]
	public unsafe struct GPUToMesh_Consolidated : IJob
	{
		[ReadOnly]
		public NativeArray<GPUVertex> verts;

		public float rootSize;
		public int meshSize;
		public int meshSize_triangles;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr managedVertexArray;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr managedTriangleArray;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr managedNormalArray;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr managedUVArray;


		public FNativeList<Vector3> verticeArray;
		public FNativeList<int> triangleArray;
		public FNativeList<Vector3> normalArray;

		public int GenerateCubeUV;
		public int GenerateSphereUV;
		public int CalculateNormals;
		public int CalculateBarycentricColors;

		public FNativeList<Vector2> uvArray;
		public FNativeList<Vector4> tangentsArray;
		public FNativeList<Vector3> tan1;
		public FNativeList<Vector3> tan2;

		public FNativeList<Vector2> uv3Array;
		public FNativeList<Vector2> uv4Array;
		public FNativeList<Vector2> uv5Array;
		public FNativeList<Vector2> uv6Array;

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

		public float UVPower;
		public float NormalAngle;
		public float Shrink;

		public FNativeMultiHashMap<Vector3, VertexEntry> dictionary;
		public FNativeList<Vector3> dictionaryKeys;
		public FNativeList<Vector3> triNormals;
		public FNativeList<Color> colorArray;

		public int isDistinct;

		[BurstDiscard]
		public void Init()
		{
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

			dictionary = new FNativeMultiHashMap<Vector3, VertexEntry>(0, Allocator.Persistent);
			dictionaryKeys = new FNativeList<Vector3>(0, Allocator.Persistent);
			triNormals = new FNativeList<Vector3>(0, Allocator.Persistent);
			colorArray = new FNativeList<Color>(0, Allocator.Persistent);

			managedUVArray = IntPtr.Zero;
			isDistinct = 0;
		}


		public void Execute()
		{
			isDistinct = 0;
			verticeArray.Clear();
			normalArray.Clear();
			triangleArray.Clear();
			uvArray.Clear();
			uv3Array.Clear();
			uv4Array.Clear();
			uv5Array.Clear();
			uv6Array.Clear();
			tangentsArray.Clear();

			tan1.Clear();
			tan2.Clear();
			colorArray.Clear();

			if (meshSize == 0) return;

			verticeArray.AddRange(managedVertexArray.ToPointer(), meshSize);
			normalArray.AddRange(managedNormalArray.ToPointer(), meshSize);
			triangleArray.AddRange(managedTriangleArray.ToPointer(), meshSize_triangles);

			if (managedUVArray != IntPtr.Zero && !(GenerateCubeUV == 1 || GenerateSphereUV == 1))
				uvArray.AddRange(managedUVArray.ToPointer(), meshSize);


			int count = verticeArray.Length;

			if (texturedata_UV3.IsCreated)
			{

				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = verticeArray[i];
					float texturevalue = texturedata_UV3._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV3;
					uv3Array.Add(new Vector2(texturevalue, texturevalue));
				}
			}

			if (texturedata_UV4.IsCreated)
			{

				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = verticeArray[i];
					float texturevalue = texturedata_UV4._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV4;
					uv4Array.Add(new Vector2(texturevalue, texturevalue));
				}
			}

			if (texturedata_UV5.IsCreated)
			{
				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = verticeArray[i];
					float texturevalue = texturedata_UV5._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV3;
					uv3Array.Add(new Vector2(texturevalue, texturevalue));
				}
			}

			if (texturedata_UV6.IsCreated)
			{
				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = verticeArray[i];
					float texturevalue = texturedata_UV6._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * TexturePowerUV3;
					uv3Array.Add(new Vector2(texturevalue, texturevalue));
				}
			}


			if (CalculateNormals == 1)
			{
				RecalculateNormals(NormalAngle);
			}

			if (GenerateCubeUV == 1 || GenerateSphereUV == 1)
			{
				uvArray.Clear();

				for (int i = 0; i < count; i++)
				{
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				if (GenerateCubeUV == 1)
				{
					CalculateCubemap(count);
				}
				else
				{
					CalculateSpheremap(count);
				}


				ExecuteTangents();
			}

			if (CalculateBarycentricColors == 1)
			{
				CreateBarycentricColors();
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
				tangentsArray.Add(output);

			}


		}

		public void CalculateCubemap(int count)
		{

			if (verticeArray.Length % 3 == 0)
			{
				Vector2 output;
				for (int i = 0; i < count; i += 3)
				{
					Vector3 vertex1 = (verticeArray[i]);
					Vector3 vertex2 = (verticeArray[i + 1]);
					Vector3 vertex3 = (verticeArray[i + 2]);
					Vector3 vertex = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1).normalized;

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

					vertex = vertex1;
					output = new Vector2();
					output += new Vector2(vertex.z, vertex.y) * choosenX;
					output += new Vector2(vertex.x, vertex.z) * choosenY;
					output += new Vector2(vertex.x, vertex.y) * choosenZ;
					Vector2 uv = output * UVPower;
					uvArray.Add(uv);

					vertex = vertex2;
					output = new Vector2();
					output += new Vector2(vertex.z, vertex.y) * choosenX;
					output += new Vector2(vertex.x, vertex.z) * choosenY;
					output += new Vector2(vertex.x, vertex.y) * choosenZ;
					uv = output * UVPower;
					uvArray.Add(uv);

					vertex = vertex3;
					output = new Vector2();
					output += new Vector2(vertex.z, vertex.y) * choosenX;
					output += new Vector2(vertex.x, vertex.z) * choosenY;
					output += new Vector2(vertex.x, vertex.y) * choosenZ;
					uv = output * UVPower;
					uvArray.Add(uv);
				}
			}
			else
			{
				Vector2 output;
				for (int i = 0; i < count; i++)
				{
					Vector3 vertex = (verticeArray[i]);

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
					else if (absY > absZ && absY > absX)
					{
						choosenY = 1;
					}
					else
					{
						choosenZ = 1;
					}

					output = new Vector2();
					output += new Vector2(vertex.z, vertex.y) * choosenX;
					output += new Vector2(vertex.x, vertex.z) * choosenY;
					output += new Vector2(vertex.x, vertex.y) * choosenZ;
					Vector2 uv = output * UVPower;
					uvArray.Add(uv);
				}
			}
		}

		public void CalculateSpheremap(int count)
		{
			for (int i = 0; i < count; i++)
			{
				Vector3 n = (verticeArray[i] - Vector3.one * rootSize * 0.5f).normalized;

				float u = (Mathf.Atan2(n.z, n.x) / (2f * Mathf.PI));
				float v = (Mathf.Asin(n.y) / Mathf.PI) + 0.5f;

				uvArray.Add(new Vector2(u, v) * UVPower);
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

		public void CreateBarycentricColors()
		{
			int count = verticeArray.Length;
			for (int i = 0; i < count; i += 3)
			{
				colorArray.Add(new Color(1, 0, 0));
				colorArray.Add(new Color(0, 1, 0));
				colorArray.Add(new Color(0, 0, 1));
			}
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

			if (dictionary.IsCreated) dictionary.Dispose();
			if (dictionaryKeys.IsCreated) dictionaryKeys.Dispose();
			if (triNormals.IsCreated) triNormals.Dispose();
			if (colorArray.IsCreated) colorArray.Dispose();

			managedUVArray = IntPtr.Zero;
			managedVertexArray = IntPtr.Zero;
			managedTriangleArray = IntPtr.Zero;
			managedNormalArray = IntPtr.Zero;
		}
	}
}
