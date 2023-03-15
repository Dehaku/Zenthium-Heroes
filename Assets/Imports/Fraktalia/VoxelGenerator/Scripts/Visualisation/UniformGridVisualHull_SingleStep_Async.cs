using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	public class UniformGridVisualHull_SingleStep_Async : BasicNativeVisualHull
	{
		[Tooltip("When true, no collision is applied when chunks are far away from a chunk loading entity.")]
		public bool NoDistanceCollision;
		[Tooltip("Chunk is marked as far away if the LODDistance of the Voxel Generator is greater than this value.")]
		public int FarDistance;

		[PropertyKey("Resolution Settings", false)]
		[Range(2, 128)] [Tooltip("Resolution of individual Cells")]
		public int width = 8;
		[Range(1, 16)] [Tooltip("Amount of GameObjects used for generation")]
		public int Cell_Subdivision = 8;
		[Range(1, 8)]
		[Tooltip("Maximum CPU Cores which can be dedicated. Affects generation speed.")]
		public int NumCores = 4;	
		[Range(0, 20)] public int frameBudgetPerCell = 0;
		
		[PropertyKey("Appearance Settings", false)]
		public Material VoxelMaterial;
		
		public List<BasicSurfaceModifier> DetailGenerator = new List<BasicSurfaceModifier>();
		protected float cellSize;
		protected float voxelSize;
		private bool isInitialized = false;
		private Queue<int> WorkerQueue;
		protected bool[] Haschset;
		
		private int[] works;
		private Material usedmaterial;
		private FNativeList<Vector3> NULL_VERTICES;
		private FNativeList<int> NULL_TRIANGLES;
		private FNativeList<Vector3> NULL_NORMALS;
		private int PostProcesses = 0;
		private bool isWorking;


		protected override void Initialize()
		{
			base.Initialize();
			NULL_NORMALS = new FNativeList<Vector3>(0, Allocator.Persistent);
			NULL_VERTICES = new FNativeList<Vector3>(0, Allocator.Persistent);
			NULL_TRIANGLES = new FNativeList<int>(0, Allocator.Persistent);
			isInitialized = true;		
			int piececount = Cell_Subdivision * Cell_Subdivision * Cell_Subdivision;
			Haschset = new bool[piececount];
			works = new int[piececount];
			CreateVoxelPieces(piececount, VoxelMaterial);
			BasicSurfaceModifier.RemoveDuplicates(DetailGenerator);
			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if (DetailGenerator[i] && DetailGenerator[i].enabled)
				{
					DetailGenerator[i].Width = width;
					DetailGenerator[i].Cell_Subdivision = Cell_Subdivision;
					DetailGenerator[i].NumCores = 1;
					DetailGenerator[i].InitVisualHull(VoxelMeshes.Count);
				}
			}
			PostProcesses = 0;
			initializeCalculators();
		}

		protected virtual void initializeCalculators() { }

		public override void Rebuild()
		{
			for (int index = 0; index < Cell_Subdivision * Cell_Subdivision * Cell_Subdivision; index++)
			{
				WorkerQueue.Enqueue(index);
				Haschset[index] = true;
			}
		}

		protected override void cleanup()
		{
			if (engine && engine.IsInitialized)
			{
				int old_Asynchronity = frameBudgetPerCell;
				int asynchronity = frameBudgetPerCell;
				frameBudgetPerCell = 0;
				for (int i = 0; i < old_Asynchronity; i++)
				{
					engine.UpdateVoxelTreeRoutine.MoveNext();
				}
				frameBudgetPerCell = old_Asynchronity;
			}
			ClearVoxelPieces();
			WorkerQueue = new Queue<int>();
			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if (DetailGenerator[i])
					DetailGenerator[i].CleanUp();
			}
			PostProcesses = 0;
			cleanUpCalculation();
			isInitialized = false;
			if (NULL_NORMALS.IsCreated) NULL_NORMALS.Dispose();
			if (NULL_VERTICES.IsCreated) NULL_VERTICES.Dispose();
			if (NULL_TRIANGLES.IsCreated) NULL_TRIANGLES.Dispose();
		}

		private void OnDisable() { cleanup(); }

		protected virtual void cleanUpCalculation()
		{
			int old_Asynchronity = frameBudgetPerCell;
			if (engine && engine.IsInitialized)
			{
				int asynchronity = frameBudgetPerCell;
				frameBudgetPerCell = 0;
				for (int i = 0; i < old_Asynchronity; i++)
				{
					engine.UpdateVoxelTreeRoutine.MoveNext();
				}
				frameBudgetPerCell = old_Asynchronity;
			}
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
				if (DetailGenerator[i])
					details += DetailGenerator[i].GetChecksum();
			}
			return base.GetChecksum() + Cell_Subdivision * 1000 + width * 100 * 10 + details + NumCores;
		}

		public override bool IsWorking()
		{
			if (WorkerQueue != null)
			{
				if (WorkerQueue.Count > 0) return true;
			}

			return isWorking;
		}

		public override IEnumerator CalculateHullAsync()
		{			
			if (!isInitialized) yield return null;
			if (!engine) yield return null;
			if (usedmaterial == null || usedmaterial != VoxelMaterial)
			{
				if (VoxelMeshes != null)
				{
					for (int x = 0; x < VoxelMeshes.Count; x++)
					{
						if (VoxelMeshes[x])
							VoxelMeshes[x].meshrenderer.sharedMaterial = VoxelMaterial;
					}
				}
				usedmaterial = VoxelMaterial;
			}
			int synchronity = 0;

#if UNITY_2021_2_OR_NEWER
			synchronity = Math.Clamp((1000 - frameBudgetPerCell) / 10, 1, 10);
#else
			synchronity = (1000 - frameBudgetPerCell) / 10;
			if (synchronity <= 1) synchronity = 1;
			if (synchronity >= 10) synchronity = 10;
#endif

			if (WorkerQueue == null)
			{
				yield return null;
			}
			if (WorkerQueue.Count > 0)
			{
				isWorking = true;
				cellSize = engine.RootSize / Cell_Subdivision;
				voxelSize = cellSize / width;

				int synchronitylevel = frameBudgetPerCell;
				IEnumerator function = beginCalculationasync(WorkerQueue, cellSize, voxelSize);
				while (true)
				{
					function.MoveNext();
					if (function.Current == null) break;
					if (synchronitylevel >= 0)
					{
						synchronitylevel--;
						yield return new YieldInstruction();
					}
				}	
			}

			isWorking = false;
			yield return null;
		}

        public override void PostProcess()
        {
			if (PostProcesses > 0)
			{
				for (int x = 0; x < works.Length; x++)
				{
					if (NoPostProcess)
					{
						works[x] = 0;
					}
					else if (works[x] > 0)
					{
						if (!Haschset[x])
						{
							Haschset[x] = true;
							WorkerQueue.Enqueue(x);
							works[x]--;
						}
					}
				}
				PostProcesses--;
				if (NoPostProcess)
				{
					PostProcesses = 0;
					NoPostProcess = false;
				}
				engine.HullsDirty = true;
			}
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
        protected virtual IEnumerator beginCalculationasync(Queue<int> WorkerQueue, float cellSize, float voxelSize) { yield return null; }

		/// <summary>
		/// Function to finish the calculation. Is called right after beginCalculation.
		/// </summary>
		/// <param name="jobIndex">Index of the m_JobHandle/calculation being used. Important to get the calculator used.</param>
		/// <param name="piece">The mesh piece which should get all results assigned</param>
		/// <param name="vertices">Give me the final array. Required for post processing (place props and foliage)</param>
		/// <param name="triangles">Give me the final array. Required for post processing (place props and foliage)</param>
		/// <param name="normals">Give me the final array. Required for post processing (place props and foliage)</param>
		protected virtual void finishCalculation(int coreindex, VoxelPiece piece, out FNativeList<Vector3> vertices, out FNativeList<int> triangles, out FNativeList<Vector3> normals)
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
					works[0] = 2;
				}
			} else
			{
				float cellSize = engine.RootSize / Cell_Subdivision;
				Vector3Int cellRanges_min = new Vector3Int(0, 0, 0);
				Vector3Int cellRanges_max = new Vector3Int(0, 0, 0);
				cellRanges_max.x = (int) ((region.RegionMax.x) / cellSize);
				cellRanges_max.y = (int) ((region.RegionMax.y) / cellSize);
				cellRanges_max.z = (int) ((region.RegionMax.z) / cellSize);
				cellRanges_min.x = (int) ((region.RegionMin.x) / cellSize);
				cellRanges_min.y = (int) ((region.RegionMin.y) / cellSize);
				cellRanges_min.z = (int) ((region.RegionMin.z) / cellSize);

				if (cellRanges_max.x < 0 || cellRanges_max.y < 0 || cellRanges_max.z < 0) return;
				if (cellRanges_min.x >= Cell_Subdivision || cellRanges_min.y >= Cell_Subdivision || cellRanges_min.z >= Cell_Subdivision) return;
				cellRanges_max.x = Mathf.Min(Cell_Subdivision, cellRanges_max.x);
				cellRanges_max.y = Mathf.Min(Cell_Subdivision, cellRanges_max.y);
				cellRanges_max.z = Mathf.Min(Cell_Subdivision, cellRanges_max.z);
				cellRanges_min.x = Mathf.Max(0, cellRanges_min.x);
				cellRanges_min.y = Mathf.Max(0, cellRanges_min.y);
				cellRanges_min.z = Mathf.Max(0, cellRanges_min.z);

				Vector3Int cellsize = cellRanges_max - cellRanges_min + Vector3Int.one;

				int cellcount = cellsize.x * cellsize.y * cellsize.z;
				
				int requiredprocesses = 1;
				if(cellcount > 1)
                {
					requiredprocesses = 2;
				}


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

							int index = ci + Cell_Subdivision * (cj + Cell_Subdivision * ck);

							if (index < Haschset.Length)
							{
								if (!Haschset[index])
								{
									Haschset[index] = true;
									WorkerQueue.Enqueue(index);		
								}

								works[index] = requiredprocesses;
								PostProcesses = 2;

							}

							count++;
						}
					}
				}			
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
			} else
			{
				for (int i = 0; i < VoxelMeshes.Count; i++)
				{
					VoxelMeshes[i].EnableCollision(!NoCollision);
				}
			}
		}
	}
}