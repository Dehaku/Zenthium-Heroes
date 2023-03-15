using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	public class UniformGridVisualHull_SingleStep : BasicNativeVisualHull
	{	
	

		[Header("Resolution Settings:")]
		[Range(2, 128)]
		[Tooltip("Resolution of individual Cells")]
		public int width = 8;

		[Range(1, 16)]
		[Tooltip("Amount of GameObjects used for generation")]
		public int Cell_Subdivision = 8;

		[Range(1, 64)]
		[Tooltip("Maximum CPU Cores which can be dedicated. Affects generation speed.")]
		public int NumCores = 4;

		[Tooltip("For each post process, dirty cells are drawn an additional time afterwards in order to prevent holes.")]
		[Range(0, 5)]
		public int MaxPostprocesses = 2;

		[Header("LOD Settings:")]
		[Tooltip("When true, no collision is applied when chunks are far away from a chunk loading entity.")]
		public bool NoDistanceCollision;

		[Tooltip("Chunk is marked as far away if the LODDistance of the Voxel Generator is greater than this value.")]
		public int FarDistance;

		[Header("Appearance Settings:")]
		public Material VoxelMaterial;
		[Tooltip("For an extra material layer, usually you'd place a grass shader here")]
		public Material VoxelMaterialShell;

		public List<BasicSurfaceModifier> DetailGenerator = new List<BasicSurfaceModifier>();

		protected float cellSize;
		protected float voxelSize;
		protected JobHandle[] m_JobHandles;

		private Queue<int> WorkerQueue;
		private bool[] Haschset;	
		private bool[] m_JobFree;
		private int[] workIndex;
		private int[] works;
		
		private int PostProcesses = 0;
		private Material usedmaterial;

		private FNativeList<Vector3> NULL_VERTICES;
		private FNativeList<int> NULL_TRIANGLES;
		private FNativeList<Vector3> NULL_NORMALS;



		protected override void Initialize()
		{		
			base.Initialize();

			NULL_NORMALS  = new FNativeList<Vector3>(0, Allocator.Persistent);
			NULL_VERTICES = new FNativeList<Vector3>(0, Allocator.Persistent);
			NULL_TRIANGLES = new FNativeList<int>(0, Allocator.Persistent);
			
			m_JobHandles = new JobHandle[NumCores];
			m_JobFree = new bool[NumCores];
			for (int i = 0; i < m_JobFree.Length; i++)
			{
				m_JobFree[i] = true;
			}

			workIndex = new int[NumCores];
			
			int piececount = Cell_Subdivision * Cell_Subdivision * Cell_Subdivision;
			Haschset = new bool[piececount];
			works = new int[piececount];
			
			CreateVoxelPieces(piececount, VoxelMaterial, VoxelMaterialShell);
			BasicSurfaceModifier.RemoveDuplicates(DetailGenerator);
			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if (DetailGenerator[i])
				{
					DetailGenerator[i].Width = width;
					DetailGenerator[i].Cell_Subdivision = Cell_Subdivision;
					DetailGenerator[i].NumCores = NumCores;
					DetailGenerator[i].InitVisualHull(VoxelMeshes.Count);
				}
			}

			initializeCalculators();
		}

		protected virtual void initializeCalculators()
		{
		
		} 


		public override void Rebuild()
		{
			for (int index = 0; index < Cell_Subdivision * Cell_Subdivision * Cell_Subdivision; index++)
			{
				WorkerQueue.Enqueue(index);
				Haschset[index] = true;
			}

			if (PostProcesses < 3)
			{
				PostProcesses++;
			}
		}

		protected override void cleanup()
		{
			ClearVoxelPieces();

			WorkerQueue = new Queue<int>();

			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if (DetailGenerator[i])
					DetailGenerator[i].CleanUp();
			}

			m_JobFree = new bool[0];

			cleanUpCalculation();

		

			PostProcesses = 0;
		  
			if(NULL_NORMALS.IsCreated) NULL_NORMALS.Dispose();
			if(NULL_VERTICES.IsCreated) NULL_VERTICES.Dispose();
			if(NULL_TRIANGLES.IsCreated) NULL_TRIANGLES.Dispose();
		}	

		protected virtual void cleanUpCalculation()
		{
			
		}

		/// <summary>
		/// Check if public parameters has been changed. Changed parameters result in a different result. Therefore the generator must rebuild itself when the checksum is different.
		/// Otherwise the result would not match your settings.
		/// Further derived clases should also call the base function.
		/// </summary>
		/// <returns></returns>
		protected override float GetChecksum()
		{
			float details = 0;

			BasicSurfaceModifier.RemoveDuplicates(DetailGenerator);
			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if(DetailGenerator[i])
				details += DetailGenerator[i].GetChecksum();
			}

			return Cell_Subdivision * 1000 + width * 100 + NumCores * 10 + details;
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

		public override void CompleteWorks()
		{				
			if (!IsInitialized) return;
			if (!engine) return;

			bool idle = true;
			if (WorkerQueue.Count > 0) idle = false;

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

			cellSize = engine.RootSize / Cell_Subdivision;
			voxelSize = cellSize / width;

			for (int m = 0; m < m_JobHandles.Length; m++)
			{
				if (m_JobFree[m] == false)
				{
					idle = false;
					continue;
				}
				if (WorkerQueue.Count == 0)
				{
					workIndex[m] = -1;
					continue;
				}

				idle = false;
				int index = WorkerQueue.Dequeue();
				workIndex[m] = index;

				int i = index % Cell_Subdivision;
				int j = (index - i) / Cell_Subdivision % Cell_Subdivision;
				int k = ((index - i) / Cell_Subdivision - j) / Cell_Subdivision;
				
				float startX = i * cellSize;
				float startY = j * cellSize;
				float startZ = k * cellSize;

				beginCalculation(m, index, cellSize, voxelSize, startX, startY, startZ);

				m_JobFree[m] = false;
			}

			for (int m = 0; m < m_JobHandles.Length; m++)
			{
				if (workIndex[m] == -1) continue;
				
				m_JobHandles[m].Complete();
				
				int index = workIndex[m];
				VoxelPiece piece = VoxelMeshes[index];

				piece.Clear();

				FNativeList<Vector3> vertices;
				FNativeList<int> triangles;
				FNativeList<Vector3> normals;

				finishCalculation(m, piece, out vertices, out triangles, out normals);

				if (!NoDistanceCollision || engine.CurrentLOD < FarDistance)
					piece.EnableCollision(!NoCollision);

				for (int i = 0; i < DetailGenerator.Count; i++)
				{
					var detail = DetailGenerator[i];
					if (detail)
					{

						if (detail.IsSave())
						{
							detail.DefineSurface(piece, vertices, triangles, normals, index);
							detail.SetSlotDirty(index);
						}

					}
				}

				Haschset[index] = false;
				m_JobFree[m] = true;
			}

			if (idle && PostProcesses > 0)
			{
				for (int i = 0; i < works.Length; i++)
				{
					if (NoPostProcess)
					{
						works[i] = 0;
					}
					else if (works[i] > 0)
					{
						if (!Haschset[i])
						{
							Haschset[i] = true;
							WorkerQueue.Enqueue(i);
							works[i]--;
						}
					}
				}

				PostProcesses--;

				if (NoPostProcess)
				{
					PostProcesses = 0;
					NoPostProcess = false;
				}				
			}

			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				var detail = DetailGenerator[i];
				if (detail)
				{
					if (detail.IsSave())
					{
						detail.PrepareWorks();
						detail.CompleteWorks();
						
					}
				}
			}
			
			//IsIdle = idle;

		}

		/// <summary>
		/// Function to start calculate the hull.
		/// </summary>
		/// <param name="jobIndex">Index of the m_JobHandle/calculation being used.</param>
		/// <param name="cellIndex">The cell index which is updated. Each cell is a region defined by SubdivisionPower (example: SP of 2 means 8 cells). Used for mesh assignment</param>
		/// <param name="cellSize">Size of the cell.</param>
		/// <param name="voxelSize">Size of the individual voxel. Is defined by cellSize/Width (example: cS=32, width = 32, voxelSize = 1)</param>
		/// <param name="startX">Start location. Add this to the vertex parameter. Else all results will be in the bottem corner.</param>
		/// <param name="startY">Start location. Add this to the vertex parameter. Else all results will be in the bottem corner.</param>
		/// <param name="startZ">Start location. Add this to the vertex parameter. Else all results will be in the bottem corner.</param>
		protected virtual void beginCalculation(int jobIndex, int cellIndex, float cellSize, float voxelSize, float startX, float startY, float startZ)
		{

		}

		/// <summary>
		/// Function to finish the calculation. Is called right after beginCalculation.
		/// </summary>
		/// <param name="jobIndex">Index of the m_JobHandle/calculation being used. Important to get the calculator used.</param>
		/// <param name="piece">The mesh piece which should get all results assigned</param>
		/// <param name="vertices">Give me the final array. Required for post processing (place props and foliage)</param>
		/// <param name="triangles">Give me the final array. Required for post processing (place props and foliage)</param>
		/// <param name="normals">Give me the final array. Required for post processing (place props and foliage)</param>
		protected virtual void finishCalculation(int jobIndex, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
		{	
			vertices = NULL_VERTICES;
			triangles = NULL_TRIANGLES;
			normals = NULL_NORMALS;
		}


		protected override void setRegionsDirty(VoxelRegion region)
		{
			int lenght = VoxelMeshes.Count;
			if (lenght == 1)
			{
				if (!Haschset[0])
				{
					Haschset[0] = true;
					WorkerQueue.Enqueue(0);
					works[0] = 3;
				}

			}
			else
			{
				float cellSize = engine.RootSize / Cell_Subdivision;

				Vector3Int cellRanges_min = new Vector3Int(0, 0, 0);
				Vector3Int cellRanges_max = new Vector3Int(0, 0, 0);

				cellRanges_max.x = (int)((region.RegionMax.x) / cellSize);
				cellRanges_max.y = (int)((region.RegionMax.y) / cellSize);
				cellRanges_max.z = (int)((region.RegionMax.z) / cellSize);

				cellRanges_min.x = (int)((region.RegionMin.x) / cellSize);
				cellRanges_min.y = (int)((region.RegionMin.y) / cellSize);
				cellRanges_min.z = (int)((region.RegionMin.z) / cellSize);

				for (int x = cellRanges_min.x; x <= cellRanges_max.x; x++)
				{
					for (int y = cellRanges_min.y; y <= cellRanges_max.y; y++)
					{
						for (int z = cellRanges_min.z; z <= cellRanges_max.z; z++)
						{
							int ci = x;
							int cj = y;
							int ck = z;

							

							if (x >= 0 && y >= 0 && z >= 0 && x < Cell_Subdivision && y < Cell_Subdivision && z < Cell_Subdivision)
							{
								int index = ci + Cell_Subdivision * (cj + Cell_Subdivision * ck);
								if (!Haschset[index])
								{
									Haschset[index] = true;
									WorkerQueue.Enqueue(index);
									works[index] = 3;
								}


							}

						}
					}
				}
			}

			if (PostProcesses < MaxPostprocesses)
			{
				PostProcesses++;
			}
		}

		public override void UpdateLOD(int newLOD)
		{
			if (NoDistanceCollision && newLOD >= FarDistance)
			{
				for (int i = 0; i < VoxelMeshes.Count; i++)
				{
					VoxelMeshes[i].EnableCollision(false);
				}
				
			}
			else
			{
				for (int i = 0; i < VoxelMeshes.Count; i++)
				{
					VoxelMeshes[i].EnableCollision(!NoCollision);
				}
			}

		}
	}

}
