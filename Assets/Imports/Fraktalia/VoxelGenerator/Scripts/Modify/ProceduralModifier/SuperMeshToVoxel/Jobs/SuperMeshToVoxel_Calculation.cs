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
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	[BurstCompile]
	public struct SuperMeshToVoxel_Calculation : IJobParallelFor
	{
		public Vector3 Start;

		public float VoxelSize;
		public float HalfVoxelSize;
		public Vector3Int blocklengths;



		public Matrix4x4 GeneratorLocalToWorld;

		
		[ReadOnly]
		public NativeArray<NativeMesh> meshes;

		[WriteOnly]
		public NativeArray<NativeVoxelModificationData> changedata;

		public byte Depth;
		public float finalMultiplier;
		public int Smoothing;
		public float Tolerance;
		public int EvaluationMode;

	



		public void Execute(int index)
		{
			Vector3Int position = MathUtilities.Convert1DTo3D(index, blocklengths.x, blocklengths.y, blocklengths.z);

			int z = position.z;
			int y = position.y;
			int x = position.x;

			Vector3 localPosition = Start + new Vector3(x * VoxelSize, y * VoxelSize, z * VoxelSize);
			Vector3 worldPos = GeneratorLocalToWorld.MultiplyPoint3x4(localPosition);
			var output = new NativeVoxelModificationData();
			output.Depth = (byte)Depth;

			float ID = 0;
			int count = 0;




			for (x = -Smoothing; x <= Smoothing; x++)
			{
				for (y = -Smoothing; y <= Smoothing; y++)
				{
					for (z = -Smoothing; z <= Smoothing; z++)
					{
						count++;
						worldPos = GeneratorLocalToWorld.MultiplyPoint3x4(localPosition + new Vector3(x * VoxelSize, y * VoxelSize, z * VoxelSize));


						ID += IsPointWithinCollider(worldPos);
					}

				}
			}
			ID /= count;			

			output.ID = (int)Mathf.Clamp(ID * finalMultiplier, 0,255);

			output.X = localPosition.x;
			output.Y = localPosition.y;
			output.Z = localPosition.z;

			changedata[index] = output;
		}

		[BurstDiscard]
		public void CleanUp()
		{
			for (int i = 0; i < meshes.Length; i++)
			{
				meshes[i].Dispose();
			}

			meshes.Dispose();
			changedata.Dispose();
		}

		public byte IsPointWithinCollider(Vector3 point)
		{
			for (int i = 0; i < meshes.Length; i++)
			{
				NativeMesh mesh = meshes[i];
				if (mesh.IsCreated == 1)
				{
					Vector3 meshpoint = mesh.WorldToLocal.MultiplyPoint3x4(point);
					if (mesh.IsInsideMesh_Slow(meshpoint, Tolerance))
					{
						return 255;
					}
				}
			}
			return 0;
		}

		
	}

	[BurstCompile]
	public unsafe struct NativeMesh : IDisposable
	{
		public Bounds Boundary;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr Vertices;

		[NativeDisableUnsafePtrRestriction]
		public IntPtr Triangles;

		[ReadOnly]
		public int VertexCount;

		[ReadOnly]
		public int TriangleCount;

		public Matrix4x4 WorldToLocal;

		public byte IsCreated;


		public NativeMesh(Mesh mesh)
		{
			if (mesh == null)
			{
				VertexCount = 0;
				TriangleCount = 0;

				Vertices = IntPtr.Zero;
				Triangles = IntPtr.Zero;
				Boundary = new Bounds();
				IsCreated = 0;
			}
			else
			{
				Boundary = mesh.bounds;

				VertexCount = mesh.vertices.Length;
				TriangleCount = mesh.triangles.Length;

				Vector3[] vertices = mesh.vertices;
				int[] triangles = mesh.triangles;


				Vertices = (IntPtr)UnsafeUtility.Malloc(vertices.Length * UnsafeUtility.SizeOf<Vector3>(), UnsafeUtility.AlignOf<Vector3>(), Allocator.Persistent);
				Triangles = (IntPtr)UnsafeUtility.Malloc(triangles.Length * UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), Allocator.Persistent);

				fixed (void* vertexBufferPointer = vertices)
				{
					UnsafeUtility.MemCpy(Vertices.ToPointer(), vertexBufferPointer, VertexCount * UnsafeUtility.SizeOf<Vector3>());
				}

				fixed (void* triangleBufferPointer = triangles)
				{
					UnsafeUtility.MemCpy(Triangles.ToPointer(), triangleBufferPointer, TriangleCount * UnsafeUtility.SizeOf<int>());
				}

				IsCreated = 1;

			}
			WorldToLocal = Matrix4x4.identity;

		}

		public Vector3 ReadVertex(int index)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if (index < 0 || index >= VertexCount)
			{
				throw new IndexOutOfRangeException($"Index {index} is out of range in NativeList of '{VertexCount}' Length.");
			}
#endif

			return UnsafeUtility.ReadArrayElement<Vector3>(Vertices.ToPointer(), index);
		}

		public int ReadTriangle(int index)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if (index < 0 || index >= TriangleCount)
			{
				throw new IndexOutOfRangeException($"Index {index} is out of range in NativeList of '{TriangleCount}' Length.");
			}
#endif

			return UnsafeUtility.ReadArrayElement<int>(Triangles.ToPointer(), index);
		}

		public void Dispose()
		{
			if (IsCreated == 1)
			{
				UnsafeUtility.Free(Vertices.ToPointer(), Allocator.Persistent);
				UnsafeUtility.Free(Triangles.ToPointer(), Allocator.Persistent);
				IsCreated = 0;
			}
		}

		/// <summary>
		/// Fast method to check if point is inside mesh. Only works with simple meshes.
		/// </summary>
		public bool IsInsideMesh_Fast(Vector3 point, float tolerance)
		{
			float distance = -1000;

			int triangleCount = TriangleCount / 3;
			for (int i = 0; i < triangleCount; i++)
			{
				var V1 = ReadVertex(ReadTriangle(i * 3));
				var V2 = ReadVertex(ReadTriangle(i * 3 + 1));
				var V3 = ReadVertex(ReadTriangle(i * 3 + 2));
				var P = new Plane(V1, V2, V3);

				if (P.GetSide(point))
				{
					distance = Mathf.Max(distance, P.GetDistanceToPoint(point));
				}
			}

			if (distance <= tolerance)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Slow method to check if point is inside mesh. Works with complex meshes but very slow.
		/// </summary>
		public bool IsInsideMesh_Slow(Vector3 point, float tolerance)
		{
			Vector3 center = Boundary.center;
			Vector3 p = point - center;

			int triangleCount = TriangleCount / 3;
			for (int i = 0; i < triangleCount; i++)
			{
				var a = ReadVertex(ReadTriangle(i * 3)) - center;
				var b = ReadVertex(ReadTriangle(i * 3 + 1)) - center;
				var c = ReadVertex(ReadTriangle(i * 3 + 2)) - center;

				if (RayWithinTriangle(p, a, b, c, tolerance))
					return true;
			}

			return false;
		}

		private bool RayWithinTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2, float tolerance)
		{
			Vector3 intersectionPoint;
			if (RayIntersectsTriangle(point, v0, v1, v2, out intersectionPoint, tolerance))
			{
				float pointDist = point.sqrMagnitude;
				float intersectionDist = intersectionPoint.sqrMagnitude;
				return (pointDist < intersectionDist);
			}
			return false;
		}

		private bool RayIntersectsTriangle(Vector3 direction, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 intersectionPoint, float tolerance)
		{
			intersectionPoint = new Vector3();

			Vector3 e1 = v1 - v0;
			Vector3 e2 = v2 - v0;

			Vector3 h = Vector3.Cross(direction, e2);
			float a = Vector3.Dot(e1, h);

			if (a > -tolerance && a < tolerance)
				return false;

			float f = 1 / a;
			Vector3 s = Vector3.zero - v0;
			float u = f * Vector3.Dot(s, h);

			if (u < 0.0 || u > 1.0)
				return false;

			Vector3 q = Vector3.Cross(s, e1);
			float v = f * Vector3.Dot(direction, q);

			if (v < 0.0 || u + v > 1.0)
				return false;

			// At this stage we can compute t to find out where
			// the intersection point is on the line.
			float t = f * Vector3.Dot(e2, q);

			if (t > tolerance) // ray intersection
			{
				intersectionPoint[0] = direction[0] * t;
				intersectionPoint[1] = direction[1] * t;
				intersectionPoint[2] = direction[2] * t;
				return true;
			}

			// At this point there is a line intersection
			// but not a ray intersection.
			return false;
		}

		public void GetVerticeArray(bool transformed, FNativeList<Vector3> outputList)
		{
			if(IsCreated != 1)
			{
				throw new InvalidOperationException("Native Mesh is not created");
			}

			outputList.Clear();
			for (int i = 0; i < VertexCount; i++)
			{
				outputList.Add(WorldToLocal.MultiplyPoint3x4(ReadVertex(i)));
			}			
		}

		public void GetTriangleArray(FNativeList<int> outputList)
		{
			if (IsCreated != 1)
			{
				throw new InvalidOperationException("Native Mesh is not created");
			}

			outputList.Clear();
			for (int i = 0; i < TriangleCount; i++)
			{
				outputList.Add(ReadTriangle(i));
			}
			
		}

		public Bounds GetTransformedBounds()
		{
			Bounds output = Boundary;
			output.min = WorldToLocal.MultiplyPoint3x4(Boundary.min);
			output.max = WorldToLocal.MultiplyPoint3x4(Boundary.max);
			return output;
		}
	}
}
