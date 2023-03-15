using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Fraktalia.Core.Math;
using Fraktalia.Utility;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	[BurstCompile]
	public unsafe struct Surface_Detail_Calculation : IJob
	{
		/// <summary>
		/// MODE:
		/// 0 = Crystallic
		/// 1 = Individual Object
		/// </summary>
		public int MODE;

		
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree requirementData;
		public int requirementvalid;

		[NativeDisableParallelForRestriction]
		public NativeVoxelTree lifeData;
		public int lifevalid;

		public float voxelSize;
		public float halfSize;
		public float cellSize;

		[ReadOnly]
		public FNativeList<Vector3> surface_verticeArray;
		[ReadOnly]
		public FNativeList<int> surface_triangleArray;
		[ReadOnly]
		public FNativeList<Vector3> surface_normalArray;

		[ReadOnly]
		public FNativeList<Vector3> mesh_verticeArray;
		[ReadOnly]	
		public FNativeList<Vector3> mesh_normalArray;
		[ReadOnly]
		public FNativeList<int> mesh_triangleArray;
		[ReadOnly]
		public FNativeList<Vector4> mesh_tangentsArray;
		[ReadOnly]
		public FNativeList<Color> mesh_colorArray;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uvArray;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uv3Array;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uv4Array;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uv5Array;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uv6Array;
		
	



		public FNativeList<Vector3> verticeArray;
		public FNativeList<int> triangleArray;
		public FNativeList<Vector2> uvArray;
		public FNativeList<Vector2> uv3Array;
		public FNativeList<Vector2> uv4Array;
		public FNativeList<Vector2> uv5Array;
		public FNativeList<Vector2> uv6Array;
		public FNativeList<Vector3> normalArray;
		public FNativeList<Vector4> tangentsArray;
		public FNativeList<Color> colorArray;
		public FNativeList<Matrix4x4> objectArray;
		
		[ReadOnly]
		public NativeArray<Vector3> Permutations;

		public Vector2 TrianglePos_min;
		public Vector2 TrianglePos_max;
		public float Angle_Min;
		public float Angle_Max;
		public Vector3 Angle_UpwardVector;
		public PlacementManifest CrystalManifest;
		public float CrystalNormalInfluence;

		public PlacementManifest ObjectManifest;
		public float ObjectNormalInfluence;

		public float CrystalProbability;
		public float ObjectProbability;
		public float Density;
		public DetailPlacement Placement;
		public Vector3 positionoffset;	

		public int slotIndex;

		[BurstDiscard]
		public void Init()
		{
			CleanUp();		
			verticeArray = new FNativeList<Vector3>(Allocator.Persistent);
			triangleArray = new FNativeList<int>(Allocator.Persistent);
			uvArray = new FNativeList<Vector2>(Allocator.Persistent);
			uv3Array = new FNativeList<Vector2>(Allocator.Persistent);
			uv4Array = new FNativeList<Vector2>(Allocator.Persistent);
			uv5Array = new FNativeList<Vector2>(Allocator.Persistent);
			uv6Array = new FNativeList<Vector2>(Allocator.Persistent);
			normalArray = new FNativeList<Vector3>(Allocator.Persistent);
			tangentsArray = new FNativeList<Vector4>(Allocator.Persistent);
			colorArray = new FNativeList<Color>(Allocator.Persistent);
			objectArray = new FNativeList<Matrix4x4>(Allocator.Persistent);		
		}

		public bool _IsBetweenAngle(Vector3 normal)
		{
			float angle = Vector3.Angle(normal, Angle_UpwardVector);
			if (angle > Angle_Max) return false;
			if (angle < Angle_Min) return false;
			return true;
		}

		public void Execute()
		{
			int permutationcount = Permutations.Length;

			verticeArray.Clear();
			triangleArray.Clear();
			uvArray.Clear();
			tangentsArray.Clear();
			normalArray.Clear();
			colorArray.Clear();
			uv3Array.Clear();
			uv4Array.Clear();
			uv5Array.Clear();
			uv6Array.Clear();

			objectArray.Clear();
			
			int triangleindex = 0;
			int crystalcount = 0;
			int count = surface_triangleArray.Length;
			for (int index = 0; index < count; index+=3)
			{
				triangleindex = surface_triangleArray[index];
				Vector3 normal = surface_normalArray[triangleindex];
				if (!_IsBetweenAngle(normal)) continue;

				Vector3 v1 = surface_verticeArray[triangleindex];


				Vector3 v2 = surface_verticeArray[surface_triangleArray[index + 1]];
				Vector3 v3 = surface_verticeArray[surface_triangleArray[index + 2]];

				Vector3 centerposition = (v1 + v2 + v3) / 3;
				

				float fX = centerposition.x;
				float fY = centerposition.y;
				float fZ = centerposition.z;

				float survivalchance = 1;
				if(requirementvalid == 1) survivalchance *= Placement.CalculateDetail(fX, fY, fZ, ref requirementData);
				if (lifevalid == 1) survivalchance *= Placement.CalculateLife(fX, fY, fZ, ref lifeData);

				


				float areas = MathUtilities.TriangleArea(v1, v2, v3);

				Vector3Int voxelPosition = new Vector3Int((int)(centerposition.x / voxelSize*4), (int)(centerposition.y / voxelSize*4), (int)(centerposition.z / voxelSize*4));


				int randomlookup = Mathf.Abs(voxelPosition.x * 2287 + voxelPosition.y * 3457 + voxelPosition.z * 3347 + slotIndex * 4397);

				float objectprobability = ObjectProbability * survivalchance;
				if (MODE == 1 || MODE == 2)
				{
					
					Vector3 random3 = Permutations[randomlookup % permutationcount];

					if (random3.y <= objectprobability)
					{
						Matrix4x4 detailmatrix = GetMatrix(ref ObjectManifest, v1, v2, v3, randomlookup, ObjectNormalInfluence);
						AddObject(detailmatrix);
					}
					
				}

				float crystalprobability = CrystalProbability * survivalchance;
				randomlookup++;
				if (MODE == 0 || MODE == 2)
				{
					for (int k = 0; k < Density; k++)
					{
						randomlookup += k * 1000;
						Vector3 random3 = Permutations[randomlookup % permutationcount];

						if (random3.y <= crystalprobability)
						{
							Matrix4x4 detailmatrix = GetMatrix(ref CrystalManifest, v1, v2, v3, randomlookup, CrystalNormalInfluence);
							AddCrystal(detailmatrix, ref crystalcount);
						}
					}
				}

				
			}
		}

		public Matrix4x4 GetMatrix(ref PlacementManifest manifest,  Vector3 triangleA, Vector3 triangleB, Vector3 triangleC, int randomnlockup, float normalinfluence)
		{
			int permutationcount = Permutations.Length;

			Vector3 random = Permutations[Mathf.Abs(randomnlockup * 31) % permutationcount];
			Vector3 random2 = Permutations[Mathf.Abs(randomnlockup * 7) % permutationcount];
			Vector3 random3 = Permutations[Mathf.Abs(randomnlockup * 11) % permutationcount];

			float r1 = random3.x;
			float r2 = random2.y;
			if (r1 + r2 > 1)
			{
				r1 = (1 - r1);
				r2 = (1 - r2);
			}

			Vector2 posontriangle;
			posontriangle.x = Mathf.Lerp(TrianglePos_min.x, TrianglePos_max.x, r1);
			posontriangle.y = Mathf.Lerp(TrianglePos_min.y, TrianglePos_max.y, r2);

			Vector3 edge0 = triangleA;
			Vector3 corner1 = triangleB - edge0;
			Vector3 corner2 = triangleC - edge0;

			Vector3 surfacenormal = Vector3.Cross(corner1, corner2).normalized * normalinfluence;
			Vector3 surfacecenterPoint = edge0 + corner1 * posontriangle.x + corner2 * posontriangle.y;

			Vector3 offset = manifest._GetOffset(random.x, random2.y, random3.z) * voxelSize;
			
			Vector3 position;
			position.x = surfacecenterPoint.x;
			position.y = surfacecenterPoint.y;
			position.z = surfacecenterPoint.z;

			float scalefactor = manifest._GetScaleFactor(random3.x) * voxelSize;
			Vector3 scale = manifest._GetScale(random2.x, random2.y, random3.z) * scalefactor;
	
			Vector3 rot = manifest._GetRotation(random.x, random.y, random.z);

			Quaternion objectrotation = Quaternion.Euler(rot);
			Quaternion rotation = Quaternion.FromToRotation(Vector3.up, surfacenormal) * objectrotation;
			offset = rotation * offset;

			return Matrix4x4.TRS(position + offset, rotation, scale);
		}


		public void AddCrystal(Matrix4x4 matrix, ref int crystalcount)
		{
			
			int mesh_vertcount = mesh_verticeArray.Length;
			int mesh_tricount = mesh_triangleArray.Length;

			for (int v = 0; v < mesh_vertcount; v++)
			{
				Vector3 vertex = mesh_verticeArray[v];
				vertex = matrix.MultiplyPoint3x4(vertex);

				Vector4 tangent = mesh_tangentsArray[v];
				float w = tangent.w;
				tangent = matrix.MultiplyVector(tangent);
				tangent.w = w;

				verticeArray.Add(vertex);
				uvArray.Add(mesh_uvArray[v]);
				tangentsArray.Add(tangent);

				normalArray.Add(matrix.MultiplyVector(mesh_normalArray[v]));
			}

			for (int v = 0; v < mesh_colorArray.Length; v++)
			{
				colorArray.Add(mesh_colorArray[v]);
			}


			for (int v = 0; v < mesh_uv3Array.Length; v++)
			{
				uv3Array.Add(mesh_uv3Array[v]);
			}

			for (int v = 0; v < mesh_uv4Array.Length; v++)
			{
				uv4Array.Add(mesh_uv4Array[v]);
			}

			for (int v = 0; v < mesh_uv5Array.Length; v++)
			{
				uv5Array.Add(mesh_uv5Array[v]);
			}

			for (int v = 0; v < mesh_uv6Array.Length; v++)
			{
				uv6Array.Add(mesh_uv6Array[v]);
			}

			for (int t = 0; t < mesh_tricount; t++)
			{
				triangleArray.Add(crystalcount * mesh_vertcount + mesh_triangleArray[t]);
			}

			crystalcount++;
		}

		public void AddObject(Matrix4x4 matrix)
		{
			objectArray.Add(matrix);
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
			if (colorArray.IsCreated) colorArray.Dispose();
			if (objectArray.IsCreated) objectArray.Dispose();
		}
	}
}
