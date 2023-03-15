using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.Math;

namespace Fraktalia.VoxelGen.Visualisation
{
	public class UVWriter : BasicNativeVisualHull
	{	
		[Header("Target Mesh Settings")]
		public Mesh MeshSource;
		public Vector3 MeshPosition;
		public Vector3 MeshRotationEuler;
		public Vector3 MeshScale;
		public bool BestFit;


		public Material VoxelMaterial;
		[Tooltip("For an extra material layer, usually you'd place a grass shader here")]
		public Material VoxelMaterialShell;
		
		Queue<int> WorkerQueue;
		bool[] Haschset;

		UVWriter_Calculation[] m_MeshModJobs;
		JobHandle[] m_JobHandles;
		bool[] m_JobFree;


		int[] workIndex;

		[Header("Property Channels")]
		[Range(-1, 4)]
		public int TextureDimensionUV3 = -1;
		[Range(-1, 4)]
		public int TextureDimensionUV4 = -1;
		[Range(-1, 4)]
		public int TextureDimensionUV5 = -1;
		[Range(-1, 4)]
		public int TextureDimensionUV6 = -1;

		[Header("Other Settings")]
		public bool AddTesselationColors;

		private bool isInitialized = false;	
		private int[] skips;
		

		private static int activeHulls;
		private Material usedmaterial;

		protected override void Initialize()
		{
			int NumCores = 1;
			if (MeshSource == null) return;

			activeHulls++;
			isInitialized = true;
			m_MeshModJobs = new UVWriter_Calculation[NumCores];
			m_JobHandles = new JobHandle[NumCores];
			m_JobFree = new bool[NumCores];
			for (int i = 0; i < m_JobFree.Length; i++)
			{
				m_JobFree[i] = true;
			}

			workIndex = new int[NumCores];

			Vector3 pos = new Vector3();
			Vector3 scale = new Vector3();

			if (BestFit)
			{
				pos = Vector3.one * engine.RootSize / 2;
				scale = Vector3.one * engine.RootSize;
			}
			else
			{
				pos = MeshPosition;
				scale = MeshScale;
			}
			
			for (int i = 0; i < m_MeshModJobs.Length; i++)
			{
				Mesh mesh_normalized = MeshUtilities.GetNormalizedMesh(MeshSource, true);
				MeshUtilities.ScaleMesh(mesh_normalized, scale);
				MeshUtilities.TranslateMesh(mesh_normalized, pos);

				if (AddTesselationColors)
				{
					m_MeshModJobs[i].CreateTesselationColors = 1;
				}
				else
				{
					m_MeshModJobs[i].CreateTesselationColors = 0;
				}

				m_MeshModJobs[i].Init(mesh_normalized, 1);
				DestroyImmediate(mesh_normalized);

				

				m_MeshModJobs[i].data = engine.Data[0];			

				if (TextureDimensionUV3 != -1 && TextureDimensionUV3 < engine.Data.Length)
				{
					m_MeshModJobs[i].texturedata_UV3 = engine.Data[TextureDimensionUV3];
				}

				if (TextureDimensionUV4 != -1 && TextureDimensionUV4 < engine.Data.Length)
				{
					m_MeshModJobs[i].texturedata_UV4 = engine.Data[TextureDimensionUV4];
				}

				if (TextureDimensionUV5 != -1 && TextureDimensionUV5 < engine.Data.Length)
				{
					m_MeshModJobs[i].texturedata_UV5 = engine.Data[TextureDimensionUV5];
				}

				if (TextureDimensionUV6 != -1 && TextureDimensionUV6 < engine.Data.Length)
				{
					m_MeshModJobs[i].texturedata_UV6 = engine.Data[TextureDimensionUV6];
				}
			}	

			int piececount = 1;
			
			skips = new int[piececount];
			Haschset = new bool[piececount];
			for (int index = 0; index < piececount; index++)
			{
				
				WorkerQueue.Enqueue(index);
				Haschset[index] = true;
			}

			CreateVoxelPieces(piececount, VoxelMaterial);			
		}

		public override void Rebuild()
		{
			for (int index = 0; index < 1; index++)
			{
				WorkerQueue.Enqueue(index);
				Haschset[index] = true;
			}
		}


		public override void PrepareWorks()
		{
			
			if (!isInitialized) return;
			if (!engine) return;
			if (m_MeshModJobs == null)
			{
				engine.CleanUp();
				return;
			}

			if (usedmaterial == null || usedmaterial != VoxelMaterial)
			{
				if (VoxelMeshes != null)
				{
					for (int i = 0; i < VoxelMeshes.Count; i++)
					{
						if (VoxelMeshes[i])
							VoxelMeshes[i].meshrenderer.sharedMaterial = VoxelMaterial;
					}
				}
				usedmaterial = VoxelMaterial;
			}

			for (int m = 0; m < m_MeshModJobs.Length; m++)
			{
				if (m_JobFree[m] == false) continue;
				if (WorkerQueue.Count == 0)
				{
					workIndex[m] = -1;
					continue;
				}
			
				int index = WorkerQueue.Dequeue();
				workIndex[m] = index;				

				m_JobHandles[m] = m_MeshModJobs[m].Schedule();
				m_JobFree[m] = false;
			}
		}

		public override void CompleteWorks()
		{
			if (!isInitialized) return;
			if (!engine) return;

			for (int m = 0; m < m_MeshModJobs.Length; m++)
			{
				if (workIndex[m] == -1) continue;
				if (!m_JobHandles[m].IsCompleted && skips[m] < 4)
				{
					skips[m]++;
					continue;
				}


				m_JobHandles[m].Complete();
				skips[m] = 0;

				int index = workIndex[m];

				if (VoxelMeshes[index] == null)
				{
					VoxelMeshes[index] = CreateVoxelPiece("__VOXELPIECE_", VoxelMaterial);
				}

				


				Mesh mesh = VoxelMeshes[index].meshfilter.sharedMesh;
				if (mesh == null)
				{
					mesh = new Mesh();
					mesh.MarkDynamic();
					mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
					VoxelMeshes[index].meshfilter.sharedMesh = mesh;
				}

				mesh.Clear();
				mesh.vertices = m_MeshModJobs[m].mesh_verticeArray.ToArray();
				mesh.triangles = m_MeshModJobs[m].mesh_triangleArray.ToArray();
				mesh.normals = m_MeshModJobs[m].mesh_normalArray.ToArray();
				mesh.uv = m_MeshModJobs[m].mesh_uvArray.ToArray();
				mesh.tangents = m_MeshModJobs[m].mesh_tangentsArray.ToArray();
				mesh.uv3 = m_MeshModJobs[m].uv3Array.ToArray();
				mesh.uv4 = m_MeshModJobs[m].uv4Array.ToArray();
				mesh.uv5 = m_MeshModJobs[m].uv5Array.ToArray();
				mesh.uv6 = m_MeshModJobs[m].uv6Array.ToArray();
				if (AddTesselationColors)
				{
					mesh.colors = m_MeshModJobs[m].colorArray.ToArray();
				}

				m_JobFree[m] = true;

				mesh.RecalculateBounds();

				

				if (!NoCollision)
				{
					VoxelMeshes[index].meshcollider.enabled = true;
					VoxelMeshes[index].meshcollider.sharedMesh = mesh;
				}
				Haschset[index] = false;


			}
		}

		protected override void setRegionsDirty(VoxelRegion region)
		{
			float cellSize = engine.RootSize / 1;

			Vector3Int cellRanges_min = new Vector3Int(0, 0, 0);
			Vector3Int cellRanges_max = new Vector3Int(0, 0, 0);

			cellRanges_max.x = (int)((region.RegionMax.x) / cellSize);
			cellRanges_max.y = (int)((region.RegionMax.y) / cellSize);
			cellRanges_max.z = (int)((region.RegionMax.z) / cellSize);

			cellRanges_min.x = (int)((region.RegionMin.x) / cellSize);
			cellRanges_min.y = (int)((region.RegionMin.y) / cellSize);
			cellRanges_min.z = (int)((region.RegionMin.z) / cellSize);

			int lenght = VoxelMeshes.Count;


			for (int x = cellRanges_min.x; x <= cellRanges_max.x; x++)
			{
				for (int y = cellRanges_min.y; y <= cellRanges_max.y; y++)
				{
					for (int z = cellRanges_min.z; z <= cellRanges_max.z; z++)
					{
						int ci = x;
						int cj = y;
						int ck = z;

						int index = ci + 1 * (cj + 1 * ck);

						if (index >= 0 && index < lenght)
						{
							if (!Haschset[index])
							{
								Haschset[index] = true;
								WorkerQueue.Enqueue(index);							
							}
						}

					}
				}
			}

			
		}


		public override void NodeChanged(NativeVoxelNode node)
		{
			if (MeshSource == null) return;

			float cellSize = engine.RootSize / 1;
			float nodesize = engine.Data[0].SizeTable[node.Depth];

			Vector3 cellpos = new Vector3(node.X % cellSize, node.Y % cellSize, node.Z % cellSize);
			float cellextent = nodesize * 0.6f;

			Vector3Int cellRanges_min = new Vector3Int(0, 0, 0);
			Vector3Int cellRanges_max = new Vector3Int(0, 0, 0);


			cellRanges_max.x = (int)((node.X + cellextent) / cellSize);
			cellRanges_max.y = (int)((node.Y + cellextent) / cellSize);
			cellRanges_max.z = (int)((node.Z + cellextent) / cellSize);

			cellRanges_min.x = (int)((node.X - cellextent) / cellSize);
			cellRanges_min.y = (int)((node.Y - cellextent) / cellSize);
			cellRanges_min.z = (int)((node.Z - cellextent) / cellSize);

			int lenght = VoxelMeshes.Count;

			int count = 0;
			for (int x = cellRanges_min.x; x <= cellRanges_max.x; x++)
			{
				for (int y = cellRanges_min.y; y <= cellRanges_max.y; y++)
				{
					for (int z = cellRanges_min.z; z <= cellRanges_max.z; z++)
					{
						int ci = x;
						int cj = y;
						int ck = z;

						int index = ci + 1 * (cj + 1 * ck);

						if (index >= 0 && index < lenght)
						{
							if (!Haschset[index])
							{
								Haschset[index] = true;
								WorkerQueue.Enqueue(index);
								count++;
							}
						}
					}
				}
			}
		}

		protected override void cleanup()
		{

			ClearVoxelPieces();
			WorkerQueue = new Queue<int>();

			if (m_MeshModJobs != null)
			{
				for (int i = 0; i < m_MeshModJobs.Length; i++)
				{
					m_JobHandles[i].Complete();
					m_MeshModJobs[i].CleanUp();
				}
			}

			m_JobFree = new bool[0];
			m_MeshModJobs = new UVWriter_Calculation[0];
			
			if (isInitialized)
			{
				activeHulls--; 			
			}
			isInitialized = false;

		}

		public override bool IsWorking()
		{
			if (m_JobFree == null) return false;
			for (int i = 0; i < m_JobFree.Length; i++)
			{
				if (!m_JobFree[i]) return true;
			}

			if (WorkerQueue != null)
			{
				if (WorkerQueue.Count > 0) return true;
			}

			return false;
		}

		protected override float GetChecksum()
		{
			float sum = 1 * 1000 + 1 * 100 + 1 * 10;
			
		

			return (int)sum;
		}

		

	

	}
}

