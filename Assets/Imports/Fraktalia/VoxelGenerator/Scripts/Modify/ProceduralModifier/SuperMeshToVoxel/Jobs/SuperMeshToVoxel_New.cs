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

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	[BurstCompile]
	public struct SuperMeshToVoxel_NewTreebuilder : IJob
	{
		public Vector3 Start;
		public Vector3 End;

		public float VoxelSize;
		public float HalfVoxelSize;
		public Vector3Int blocklengths;



		public Matrix4x4 GeneratorLocalToWorld;
		public Matrix4x4 GeneratorWorldToLocal;


		[NativeDisableUnsafePtrRestriction]
		public NativeMesh mesh;
		public byte Depth;
		public float finalMultiplier;

		public MeshRayTracer tree;
		
		public void Initialize()
		{
			tree = new MeshRayTracer();
			tree.Initialize(mesh);
		}


		public void Execute()
		{
			tree.BuildTree();			
		}		
	}

	[BurstCompile]
	public struct SuperMeshToVoxel_FillArray : IJob
	{
		public Vector3 Start;
		public Vector3 End;
		public float VoxelSize;
		public float HalfVoxelSize;
		public Vector3Int blocklengths;
		public Matrix4x4 GeneratorWorldToLocal;

		[WriteOnly]
		[NativeDisableParallelForRestriction]
		public NativeArray<NativeVoxelModificationData> changedata;
		public byte Depth;
		public float finalMultiplier;

		public void Execute()
		{		
			for (int x = 0; x < blocklengths.x; x++)
			{
				for (int y = 0; y < blocklengths.y; y++)
				{
					for (int z = 0; z < blocklengths.z; z++)
					{
						Vector3 localPosition = Start + new Vector3(x * VoxelSize, y * VoxelSize, z * VoxelSize);
						localPosition = GeneratorWorldToLocal.MultiplyPoint3x4(localPosition);
						NativeVoxelModificationData data = new NativeVoxelModificationData();
						data.X = localPosition.x;
						data.Y = localPosition.y;
						data.Z = localPosition.z;
						data.Depth = Depth;
						data.ID = 0;

						int index = MathUtilities.Convert3DTo1D(x, y, z, blocklengths.x, blocklengths.y, blocklengths.z);

						changedata[index] = data;
					}
				}
			}
		}
	}


	[BurstCompile]
	public struct SuperMeshToVoxel_New : IJobParallelFor
	{
		public Vector3 Start;
		public Vector3 End;

		public float VoxelSize;
		public float HalfVoxelSize;
		public Vector3Int blocklengths;



		public Matrix4x4 GeneratorLocalToWorld;
		public Matrix4x4 GeneratorWorldToLocal;


		[NativeDisableUnsafePtrRestriction]
		public NativeMesh mesh;

		[WriteOnly][NativeDisableParallelForRestriction]
		public NativeArray<NativeVoxelModificationData> changedata;

		public byte Depth;
		public float finalMultiplier;

		public MeshRayTracer tree;

		private Box3 bounds;
		private Vector3 extents;
		private Vector3 delta;
		private Vector3 offset;
		private float eps;

		public void Initialize()
		{
			bounds = new Box3(Start, End);
			extents = bounds.Size;
			delta = new Vector3(extents.x / blocklengths.x, extents.y / blocklengths.y, extents.z / blocklengths.z);
			offset = new Vector3(0.5f / blocklengths.x, 0.5f / blocklengths.y, 0.5f / blocklengths.z);

			eps = 1e-7f * extents.z;
		}

		public void Execute(int index)
		{
			Vector2Int beampos = MathUtilities.Convert1DTo2D(index, blocklengths.x, blocklengths.y);
			int x = beampos.x;
			int y = beampos.y;




			bool inside = false;
			Vector3 rayDir = new Vector3(0.0f, 0.0f, bounds.Depth);

			// z-coord starts somewhat outside bounds 
			Vector3 rayStart = bounds.Min + new Vector3(x * delta.x + offset.x, y * delta.y + offset.y, -0.0f * extents.z);

			while (true)
			{
				MeshRay ray = tree.TraceRay(rayStart, rayDir);

				if (ray.hit == 1)
				{
					// calculate cell in which intersection occurred
					float zpos = rayStart.z + ray.distance * rayDir.z;
					float zhit = (zpos - bounds.Min.z) / delta.z;

					int z = (int)((rayStart.z - bounds.Min.z) / delta.z);
					int zend = (int)Math.Min(zhit, blocklengths.z - 1);

					if (inside)
					{
						for (int k = z; k <= zend; ++k)
						{
							Vector3 localPosition = Start + new Vector3(x * VoxelSize, y * VoxelSize, k * VoxelSize);
							localPosition = GeneratorWorldToLocal.MultiplyPoint3x4(localPosition);
							NativeVoxelModificationData data = new NativeVoxelModificationData();
							data.X = localPosition.x;
							data.Y = localPosition.y;
							data.Z = localPosition.z;	
							data.Depth = Depth;
							data.ID = 255;

							int vindex = MathUtilities.Convert3DTo1D(x, y, k, blocklengths.x, blocklengths.y, blocklengths.z);

							changedata[vindex] = data;
						}
					}

					inside = !inside;
					rayStart += rayDir * Mathf.Max(0.01f, ray.distance + eps);
				}
				else
					break;
			}
		}

		[BurstDiscard]
		public void CleanUp()
		{
					
			tree.CleanUp();
			mesh.Dispose();
		
		}
	}

}
