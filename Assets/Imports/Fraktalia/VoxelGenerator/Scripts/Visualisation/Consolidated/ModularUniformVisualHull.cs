using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Utility;
using Fraktalia.Core.Math;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
	public class NativeMeshData
    {
		public NativeArray<Vector3> positionOffset;

		public FNativeList<Vector3> verticeArray_original;
		public FNativeList<Vector3> normalArray_original;
		public FNativeList<int> triangleArray_original;

		public FNativeList<Vector3> verticeArray;	
		public FNativeList<Vector3> normalArray;

		public FNativeList<Vector2> uvArray;
		public FNativeList<Vector4> tangentsArray;
		public FNativeList<Color> colorArray;
		public FNativeList<Vector2> uv3Array;
		public FNativeList<Vector2> uv4Array;
		public FNativeList<Vector2> uv5Array;
		public FNativeList<Vector2> uv6Array;

		public void PrepareOriginalData()
        {
			verticeArray_original.Clear();		
			normalArray_original.Clear();
			triangleArray_original.Clear();
			uvArray.Clear();
		}

		public void PrepareMeshData()
		{
			verticeArray.Clear();
			verticeArray.AddRange(verticeArray_original);
			normalArray.Clear();
			normalArray.AddRange(normalArray_original);
		}


		public void Initialize()
        {
			verticeArray_original = new FNativeList<Vector3>(0, Allocator.Persistent);
			normalArray_original = new FNativeList<Vector3>(0, Allocator.Persistent);
			triangleArray_original = new FNativeList<int>(0, Allocator.Persistent);
			verticeArray = new FNativeList<Vector3>(0, Allocator.Persistent);
			normalArray = new FNativeList<Vector3>(0, Allocator.Persistent);			
			uvArray = new FNativeList<Vector2>(0, Allocator.Persistent);
			uv3Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			uv4Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			uv5Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			uv6Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			tangentsArray = new FNativeList<Vector4>(0, Allocator.Persistent);
			colorArray = new FNativeList<Color>(0, Allocator.Persistent);
			positionOffset = new NativeArray<Vector3>(1, Allocator.Persistent);
		}

		public void ClearAdditionalData()
        {	
		
			uv3Array.Clear();
			uv4Array.Clear();
			uv5Array.Clear();
			uv6Array.Clear();
			tangentsArray.Clear();		
			colorArray.Clear();
		}

		public void CleanUp()
		{
			if (uvArray.IsCreated) uvArray.Dispose();
			if (uv3Array.IsCreated) uv3Array.Dispose();
			if (uv4Array.IsCreated) uv4Array.Dispose();
			if (uv5Array.IsCreated) uv5Array.Dispose();
			if (uv6Array.IsCreated) uv6Array.Dispose();			
			if (tangentsArray.IsCreated) tangentsArray.Dispose();	
			if (colorArray.IsCreated) colorArray.Dispose();
			if (positionOffset.IsCreated) positionOffset.Dispose();
			if (verticeArray.IsCreated) verticeArray.Dispose();
			if (verticeArray_original.IsCreated) verticeArray_original.Dispose();
			if (normalArray.IsCreated) normalArray.Dispose();
			if (normalArray_original.IsCreated) normalArray_original.Dispose();
			if (triangleArray_original.IsCreated) triangleArray_original.Dispose();
		}
	}

	public enum SurfaceAlgorithm
	{
		None,
		MarchingCubes,
		Block,
		Crystal,
        Mesh
    }


	public class ModularUniformVisualHull : BasicNativeVisualHull
	{
		[HideInInspector]
		public bool DebugMode;

        public enum WorkType
        {
			Nothing = 0,
			PostProcessesOnly = 1,
			RequiresNonGeometryData = 2,
			GenerateGeometry = 3	
        }

		public class WorkInformation
        {
			public WorkType CurrentWorktype;
			public WorkType LastWorktype;
			public int works;
        } 

		public struct FractionalChecksum
        {
			public float geometryChecksum;
			public float nongeometryChecksum;
			public float postprocessChecksum;

			internal WorkType CalculateRequiredWorktype(ref FractionalChecksum fractionalChecksum)
            {
				if (!geometryChecksum.Equals(fractionalChecksum.geometryChecksum)) return WorkType.GenerateGeometry;
				if (!nongeometryChecksum.Equals(fractionalChecksum.nongeometryChecksum))
				{
					return WorkType.RequiresNonGeometryData;
				}
				if (!postprocessChecksum.Equals(fractionalChecksum.postprocessChecksum)) return WorkType.PostProcessesOnly;			
				return WorkType.Nothing;
            }
        }

		[BeginInfo("ModularUniformVisualHull")]
		[InfoTitle("Modular Hull Generator", "This is the fastest hull generator currently possible while providing the highest amount of flexibility", "ModularUniformVisualHull")]
		[InfoSection1("How to use:", "First you have to select the surface algorithm: MarchingCubes, Block(Minecraft), Crystal. \n" +
			"Second you can apply a variety of post process modules to give the result properies like multi material, UV coordinates and vertex colors.\n" +
			"Also you can add detail generators in order to add stuff like foliage. \n\n" +
			"Check Modular_Samples scenes to see all kind of variations possible! More modules are added in the future. \n\n" +
			"Overall this is the most complex hull generator so far but it is easier to extend with new features as it combines every feature in the asset to one component.", "ModularUniformVisualHull")]
		[InfoTextKey("Modular hull generator:", "ModularUniformVisualHull")]

		

		[PropertyKey("Resolution Settings", false)]
		[Range(2, 128)] [Tooltip("Resolution of individual Cells")]
		public int width = 8;
		[Range(1, 16)] [Tooltip("Amount of GameObjects used for generation")]
		public int Cell_Subdivision = 8;
		[Range(1, 8)]
        [Tooltip("Maximum CPU Cores which can be dedicated. Affects generation speed.")][SerializeField]
		private int NumCores = 4;

		private int currentNumCores;
		public int CurrentNumCores
        {
            get
            {
				return currentNumCores;
            }
        }



		[Range(0, 1000)] 
		public int frameBudgetPerCell = 0;

		[Range(0, 10)]
		public int SurfaceDimension;

		[PropertyKey("LOD Settings", false)]
		public int TargetLOD;
		[Tooltip("When true, no collision is applied when chunks are far away from a chunk loading entity.")]
		public bool NoDistanceCollision;
		[Tooltip("Chunk is marked as far away if the LODDistance of the Voxel Generator is greater than this value.")]
		public int FarDistance;

		[PropertyKey("Appearance Settings", false)]
		public Material VoxelMaterial;

		[Header("Surface Algorithm")]
		public SurfaceAlgorithm MeshSurfaceAlgorithm;



		[PropertyModule] public List<SurfaceModifierContainer> PostProcessModules = new List<SurfaceModifierContainer>();

		[PropertyModule] public List<ModularHullDetail> DetailGenerator = new List<ModularHullDetail>();
		protected float cellSize;
		protected float voxelSize;
		private bool isInitialized = false;
		private Queue<int> WorkerQueue;


		[NonSerialized] 
		public WorkInformation[] WorkInformations;
		
	
		private Material usedmaterial;
		private int PostProcesses = 0;
		private bool isWorking;


		[PropertyModule][SerializeField] private Module_MarchingCubes_GPU moduleMarchingCubes;
		[PropertyModule][SerializeField] private Module_Cubic moduleCubic;
		[PropertyModule] [SerializeField] private Module_Crystal moduleCrystal;
		[PropertyModule] [SerializeField] private Module_MeshOnly moduleMeshOnly;

		
		[SerializeField][HideInInspector]
		private HullGenerationModule currentSurfaceModule;

		[NonSerialized]
		public List<NativeMeshData> nativeMeshData = new List<NativeMeshData>();
		public NativeArray<float>[] UniformGrid;

		private int BorderedWidth;
		NativeCreateUniformGrid_V2[] calculators;
		[HideInInspector] public List<int> LODSize;
		private JobHandle voxeltogpuhandle;
		[HideInInspector]
		public int activeCores = 0;
		[HideInInspector]
		public int[] activeCells;
		[HideInInspector]
		public int synchronitylevel;
		public FractionalChecksum fractionalChecksum;

		protected override void Initialize()
		{
			#region DefineSuface
			CleanUp();
			if (currentSurfaceModule != null) currentSurfaceModule.CleanUp();
			switch (MeshSurfaceAlgorithm)
            {
                case SurfaceAlgorithm.None:
					currentSurfaceModule = new HullGenerationModule();
					break;
                case SurfaceAlgorithm.MarchingCubes:
					currentSurfaceModule = moduleMarchingCubes;
					break;
                case SurfaceAlgorithm.Block:
					currentSurfaceModule = moduleCubic;
                    break;
                case SurfaceAlgorithm.Crystal:
					currentSurfaceModule = moduleCrystal;
					break;
				case SurfaceAlgorithm.Mesh:
					currentSurfaceModule = moduleMeshOnly;
					break;
				default:
                    break;
            }
            #endregion

            #region General Parameters      
            NumCores = Mathf.Clamp(NumCores, 1, 8);
			currentNumCores = NumCores;
			if (Cell_Subdivision == 1)
            {
				currentNumCores = 1;
            }
			
			width = Mathf.ClosestPowerOfTwo(width);
			Cell_Subdivision = Mathf.ClosestPowerOfTwo(Cell_Subdivision);
			if (width < 4) width = 4;	
			BorderedWidth = width + 3;

			LODSize = new List<int>();
			int lodsize = width;
			while (lodsize >= 4)
			{
				LODSize.Add(lodsize);
				lodsize = lodsize / 2;
			}
			#endregion

			isInitialized = true;		
			int piececount = Cell_Subdivision * Cell_Subdivision * Cell_Subdivision;
			WorkInformations = new WorkInformation[piececount];
            for (int i = 0; i < WorkInformations.Length; i++)
            {
				WorkInformations[i] = new WorkInformation();
			}


			CreateVoxelPieces(piececount, VoxelMaterial);
			
			PostProcesses = 0;
	
			currentSurfaceModule.Initialize(this);

			activeCells = new int[CurrentNumCores];
			UniformGrid = new NativeArray<float>[CurrentNumCores];
			calculators = new NativeCreateUniformGrid_V2[CurrentNumCores];
			for (int i = 0; i < CurrentNumCores; i++)
            {
				UniformGrid[i] = ContainerStaticLibrary.GetArray_float(BorderedWidth * BorderedWidth * BorderedWidth, i);

				calculators[i] = new NativeCreateUniformGrid_V2();
				calculators[i].Width = width;
				if (SurfaceDimension < engine.Data.Length)
				{
					calculators[i].data = engine.Data[SurfaceDimension];
				}
				else
				{
					calculators[i].data = engine.Data[0];
				}
				calculators[i].UniformGridResult = UniformGrid[i];
			}

			nativeMeshData.Clear();
			cellSize = engine.RootSize / Cell_Subdivision;
			voxelSize = cellSize / width;
			for (int index = 0; index < piececount; index++)
            {
				int cellindex = index;
				int i = cellindex % Cell_Subdivision;
				int j = (cellindex - i) / Cell_Subdivision % Cell_Subdivision;
				int k = ((cellindex - i) / Cell_Subdivision - j) / Cell_Subdivision;
				float startX = i * cellSize;
				float startY = j * cellSize;
				float startZ = k * cellSize;

				NativeMeshData data = new NativeMeshData();				
				data.Initialize();
				data.positionOffset[0] = new Vector3(startX, startY, startZ);
				nativeMeshData.Add(data);
			}

			ModularHullDetail.RemoveDuplicates(DetailGenerator);
			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if (DetailGenerator[i] && DetailGenerator[i].enabled)
				{			
					DetailGenerator[i].InitVisualHull(this);
				}
			}
		}

		public override void Rebuild()
		{
			for (int index = 0; index < Cell_Subdivision * Cell_Subdivision * Cell_Subdivision; index++)
			{
				WorkerQueue.Enqueue(index);
				WorkInformations[index].CurrentWorktype = WorkType.GenerateGeometry;	
			}
		}

		public void FractionalRebuild(WorkType workType)
        {
			if (workType == WorkType.Nothing) return;

			for (int index = 0; index < Cell_Subdivision * Cell_Subdivision * Cell_Subdivision; index++)
			{
				if (WorkInformations[index].CurrentWorktype == WorkType.Nothing)
				{				
					WorkerQueue.Enqueue(index);
				}

				if((int)WorkInformations[index].CurrentWorktype < (int)workType)
                {
					WorkInformations[index].CurrentWorktype = workType;
					WorkInformations[index].LastWorktype = workType;
				}
				
				WorkInformations[index].works = 1;
			}

			engine.HullsDirty = true;
		
			PostProcesses = 1;
		}


		protected override void cleanup()
		{
			if (engine && engine.IsInitialized)
			{			
				while (isWorking)
				{
					engine.UpdateVoxelTreeRoutine.MoveNext();
				}		
			}
			ClearVoxelPieces();
			WorkerQueue = new Queue<int>();

			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if (DetailGenerator[i])
					DetailGenerator[i].CleanUp();
			}

			for (int i = 0; i < PostProcessModules.Count; i++)
			{
				if (PostProcessModules[i] != null)
					PostProcessModules[i].CleanUp();
			}
			PostProcesses = 0;
			cleanUpCalculation();
			isInitialized = false;

			for (int i = 0; i < nativeMeshData.Count; i++)
			{
				nativeMeshData[i].CleanUp();
			}
			nativeMeshData.Clear();

			if (currentSurfaceModule != null) currentSurfaceModule.CleanUp();

			
			isWorking = false;
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
			ModularHullDetail.RemoveDuplicates(DetailGenerator);
			for (int i = 0; i < DetailGenerator.Count; i++)
			{
				if (DetailGenerator[i])
					details += DetailGenerator[i].GetChecksum();
			}

			
			FractionalChecksum fractional = new FractionalChecksum();

			for (int i = 0; i < PostProcessModules.Count; i++)
			{
				var module = PostProcessModules[i];
				if (module.Modifier != null)
				{
					details += module.GetChecksum();
					module.Modifier.GetFractionalGeoChecksum(ref fractional, module);
				}
			}

            for (int i = 0; i < DetailGenerator.Count; i++)
            {
				if (DetailGenerator[i] != null)
				{
					details += DetailGenerator[i].GetChecksum();
					DetailGenerator[i].GetFractionalGeoChecksum(ref fractional);
				}
			}

			WorkType rebuildtype;
			rebuildtype = fractional.CalculateRequiredWorktype(ref fractionalChecksum);
			fractionalChecksum = fractional;
			FractionalRebuild(rebuildtype);

			if(rebuildtype != WorkType.Nothing)
            {
				SurfaceModifierContainer.RemoveDuplicates(PostProcessModules);
			}		

			if(usedmaterial != VoxelMaterial)
            {
				UpdateMaterial();
            }

			if(NumCores != currentNumCores)
            {
				CleanUp();
            }


			return base.GetChecksum() + Cell_Subdivision * 1000 + width * 100 * 10 + details + NumCores + ((int)MeshSurfaceAlgorithm) + currentSurfaceModule.GetChecksum() + Shrink;	
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
				UpdateMaterial();
			}

			if (WorkerQueue == null)
			{
				yield return null;
			}
			if (WorkerQueue.Count > 0)
			{
				isWorking = true;
				cellSize = engine.RootSize / Cell_Subdivision;
				voxelSize = cellSize / width;
				synchronitylevel = frameBudgetPerCell;

				activeCores = 0;
				for (int cores = 0; cores < calculators.Length; cores++)
				{
					if (WorkerQueue.Count == 0) break;
					activeCores++;
					activeCells[cores] = WorkerQueue.Dequeue();
				}

				#region Calculate Geometry           
				IEnumerator function = CalculateUniformGrid();
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

				function = currentSurfaceModule.beginCalculationasync(cellSize, voxelSize);
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

				for (int i = 0; i < activeCores; i++)
				{
					int cellIndex = activeCells[i];
					if (WorkInformations[cellIndex].CurrentWorktype == WorkType.GenerateGeometry)
					{
						WorkInformations[cellIndex].CurrentWorktype = WorkType.RequiresNonGeometryData;
					}
				}
				#endregion

				#region RequiresNonGeometryData             
				for (int i = 0; i < activeCores; i++)
				{
					int cellIndex = activeCells[i];
					nativeMeshData[cellIndex].PrepareMeshData();

					if (WorkInformations[cellIndex].CurrentWorktype == WorkType.RequiresNonGeometryData)
					{
						nativeMeshData[cellIndex].ClearAdditionalData();
					}


				}

				for (int i = 0; i < PostProcessModules.Count; i++)
				{				
					PostProcessModules[i].Initialize(this);
					
					SurfaceModifier modifier = PostProcessModules[i].Modifier;
					if (modifier == null || PostProcessModules[i].Disabled) continue;
					modifier.HullGenerator = this;
					function = modifier.beginCalculationasync(cellSize, voxelSize);
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

				for (int i = 0; i < activeCores; i++)
				{
					int cellIndex = activeCells[i];
					if (WorkInformations[cellIndex].CurrentWorktype == WorkType.RequiresNonGeometryData)
					{
						WorkInformations[cellIndex].CurrentWorktype = WorkType.PostProcessesOnly;
					}
				}
                #endregion

                #region AfterSurfaceGeneration
           
				for (int i = 0; i < DetailGenerator.Count; i++)
				{
					var detail = DetailGenerator[i];
					if (detail)
					{
						if (detail.IsSave())
						{

							detail.PrepareWorks();

							while (true)
							{
								if (detail.IsCompleted()) break;

								if (synchronitylevel >= 0)
								{
									synchronitylevel--;
									yield return new YieldInstruction();
								}
							}

							detail.CompleteWorks();

						}
					}
				}


                #endregion

                if (DebugMode)
                {
					string msg = "Required synchronity frames: " + (frameBudgetPerCell - synchronitylevel);				
					if (synchronitylevel < 0) msg += " Frame budget used up. This forces completion which causes performance spikes. Consider increasing frame budget to make it more asynchronous";
					Debug.Log(msg);
				}

				finishCalculation();
			}

			isWorking = false;
			yield return null;
		}

		private IEnumerator CalculateUniformGrid()
        {
			int lod = Mathf.Clamp(TargetLOD, 0, LODSize.Count - 1);
			float lodvoxelSize = voxelSize * Mathf.Pow(2, lod);
			int lodwidth = LODSize[lod];
			
			BorderedWidth = lodwidth + 3;
			int LODLength = BorderedWidth * BorderedWidth * BorderedWidth;
			int innerVoxelSize = NativeVoxelTree.ConvertLocalToInner(lodvoxelSize, engine.RootSize);

			if(!voxeltogpuhandle.IsCompleted)
            {
				voxeltogpuhandle.Complete();
            }
			for (int cores = 0; cores < activeCores; cores++)
			{
				int cellIndex = activeCells[cores];
				if(WorkInformations[cellIndex].CurrentWorktype != WorkType.GenerateGeometry) continue;

				if (DebugMode)
					Debug.Log("Generate Geometry");

				NativeMeshData data = nativeMeshData[cellIndex];
				Vector3 start = data.positionOffset[0];				
				data.PrepareOriginalData();						

				Vector3Int offset = new Vector3Int();
				offset.x = NativeVoxelTree.ConvertLocalToInner(start.x, engine.RootSize);
				offset.y = NativeVoxelTree.ConvertLocalToInner(start.y, engine.RootSize);
				offset.z = NativeVoxelTree.ConvertLocalToInner(start.z, engine.RootSize);
				calculators[cores].positionoffset = offset;
				calculators[cores].Width = BorderedWidth;
				calculators[cores].Shrink = (int)Shrink;
				calculators[cores].voxelSizeBitPosition = MathUtilities.RightmostBitPosition(innerVoxelSize);
				voxeltogpuhandle = calculators[cores].Schedule(LODLength, LODLength / SystemInfo.processorCount, voxeltogpuhandle);
			}


			while (!voxeltogpuhandle.IsCompleted)
			{
				yield return new YieldInstruction();
			}


			voxeltogpuhandle.Complete();
			yield return null;
		}

		private void finishCalculation()
		{
            for (int i = 0; i < activeCores; i++)
            {
				int cellIndex = activeCells[i];
				NativeMeshData data = nativeMeshData[cellIndex];

				
				WorkInformations[cellIndex].CurrentWorktype = WorkType.Nothing;
				VoxelPiece piece = VoxelMeshes[cellIndex];
				piece.Clear();
				if (data.verticeArray.Length != 0)
				{
					piece.SetVertices(data.verticeArray);
					piece.SetTriangles(data.triangleArray_original);
					piece.SetNormals(data.normalArray);

					//if (data.tangentsArray.Length == data.verticeArray.Length)
					piece.SetTangents(data.tangentsArray);

					if (data.uvArray.Length == 0 || data.uvArray.Length == data.verticeArray.Length)
						piece.SetUVs(0, data.uvArray);

					if (data.uv3Array.Length == 0 || data.uv3Array.Length == data.verticeArray.Length)
						piece.SetUVs(2, data.uv3Array);

					if (data.uv4Array.Length == 0 || data.uv4Array.Length == data.verticeArray.Length)
						piece.SetUVs(3, data.uv4Array);

					if (data.uv5Array.Length == 0 || data.uv5Array.Length == data.verticeArray.Length)
						piece.SetUVs(4, data.uv5Array);

					if (data.uv6Array.Length == 0 || data.uv6Array.Length == data.verticeArray.Length)
						piece.SetUVs(5, data.uv6Array);

					if (data.colorArray.Length == 0 || data.colorArray.Length == data.verticeArray.Length)
						piece.SetColors(data.colorArray);
				}
				
				if (!NoDistanceCollision || engine.CurrentLOD < FarDistance)
					piece.EnableCollision(!NoCollision);
			}


			
		}

		public override void PostProcess()
        {
			if (PostProcesses > 0)
			{
				for (int x = 0; x < WorkInformations.Length; x++)
				{
					if (NoPostProcess)
					{
						WorkInformations[x].works = 0;
					}
					else if (WorkInformations[x].works > 0)
					{
						if (WorkInformations[x].LastWorktype == WorkType.GenerateGeometry)
						{
							WorkInformations[x].CurrentWorktype = WorkType.GenerateGeometry;
							WorkerQueue.Enqueue(x);
							WorkInformations[x].works--;
						}

						if (WorkInformations[x].LastWorktype == WorkType.RequiresNonGeometryData)
						{
							WorkInformations[x].CurrentWorktype = WorkType.RequiresNonGeometryData;
							WorkerQueue.Enqueue(x);
							WorkInformations[x].works--;
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

		protected override void setRegionsDirty(VoxelRegion region)
		{
			WorkType requiredWorktype = DefineWorkType(region.DimensionModified);

			int lenght = VoxelMeshes.Count;
			if (lenght == 1)
			{
				if ((int)WorkInformations[0].CurrentWorktype < (int)requiredWorktype)
				{
					WorkInformations[0].CurrentWorktype = requiredWorktype;
					WorkInformations[0].LastWorktype = requiredWorktype;
					WorkerQueue.Enqueue(0);
				}
				int requiredprocesses = WorkInformations[0].works;
				requiredprocesses++;
				WorkInformations[0].works = Mathf.Min(2, requiredprocesses);
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

							if (index < WorkInformations.Length)
							{								
								if ((int)WorkInformations[index].CurrentWorktype < (int)requiredWorktype)
								{
									WorkInformations[index].CurrentWorktype = requiredWorktype;
									WorkInformations[index].LastWorktype = requiredWorktype;
									WorkerQueue.Enqueue(index);
								}
								WorkInformations[index].works = requiredprocesses;
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

			int lod = Mathf.Clamp(newLOD, 0, LODSize.Count - 1);

			if (TargetLOD != lod)
			{
				TargetLOD = lod;
				
				engine.Rebuild();
			}
		}

		public void UpdateMaterial()
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

		private WorkType DefineWorkType(int dimension)
        {
			WorkType requiredWorktype = WorkType.Nothing;
            for (int i = 0; i < DetailGenerator.Count; i++)
            {
				if(DetailGenerator[i] != null)
				requiredWorktype = (WorkType)Mathf.Max((int)requiredWorktype, (int)DetailGenerator[i].EvaluateWorkType(dimension));
			}

			for (int i = 0; i < PostProcessModules.Count; i++)
			{
				requiredWorktype = (WorkType)Mathf.Max((int)requiredWorktype, (int)PostProcessModules[i].Modifier.EvaluateWorkType(dimension));
			}

			if (dimension == SurfaceDimension || dimension == -1)
            {
				requiredWorktype = WorkType.GenerateGeometry;
			}
			return requiredWorktype;	
		}


        public override void OnDuplicate()
        {
            base.OnDuplicate();
            for (int i = 0; i < DetailGenerator.Count; i++)
            {
				if(DetailGenerator[i])
				DetailGenerator[i].OnDuplicate();
            }
        }
    }





#if UNITY_EDITOR

	[CustomEditor(typeof(ModularUniformVisualHull))]
	[CanEditMultipleObjects]
	public class UniformGridVisualHull_SingleStep_Async_ConsolidatedEditor : Editor
	{
		private InfoTextKeyAttribute infotext;

		public override void OnInspectorGUI()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 16;
			title.richText = true;
			title.alignment = TextAnchor.MiddleLeft;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;

			ModularUniformVisualHull mytarget = target as ModularUniformVisualHull;

			
		

			if (infotext != null)
			{
				EditorGUILayout.BeginVertical();
				Rect position = EditorGUILayout.BeginHorizontal(GUILayout.Height(50));

				if (FraktaliaEditorStyles.InfoTitle(position, infotext.labeltext))
				{
					InfoContent tutorial = BeginInfoAttribute.InfoContentDictionary[infotext.Key];
					TutorialWindow.Init(tutorial);
				}

				EditorGUILayout.Space();

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
			}
			bool isfoldout = true;
			int verticalgroups = 0;
			SerializedProperty prop = serializedObject.GetIterator();

			if (prop.NextVisible(true))
			{
				do
				{
					if (prop.name == "m_Script") continue;

					if (infotext == null)
					{
						infotext = FraktaliaEditorUtility.GetAttribute<InfoTextKeyAttribute>(prop, true);
					}

					PropertyKeyAttribute fold = FraktaliaEditorUtility.GetAttribute<PropertyKeyAttribute>(prop, true);
					if (fold != null)
					{
						if (isfoldout)
						{
							if (verticalgroups > 0)
							{
								verticalgroups--;
								EditorGUILayout.EndVertical();
							}
							EditorGUI.indentLevel = 0;
						}

						verticalgroups++;
						EditorGUILayout.BeginVertical();
						EditorGUILayout.BeginHorizontal();
						isfoldout = EditorGUILayout.Foldout(fold.State, fold.Key, true);
						EditorGUILayout.EndHorizontal();
						fold.State = isfoldout;

						if (!isfoldout)
						{
							verticalgroups--;
							EditorGUILayout.EndVertical();
						}
						else
						{
							EditorGUI.indentLevel = 1;
						}
					}

					if (isfoldout)
					{
						PropertyModuleAttribute ismodule = FraktaliaEditorUtility.GetAttribute<PropertyModuleAttribute>(prop, true);
						if(ismodule == null)
                        {
							EditorGUILayout.PropertyField(prop, true);
						}
						
						if (prop.name.Equals("NumCores"))
						{
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField("Required GPU cores: " + mytarget.CurrentNumCores * 64, bold);
							EditorGUILayout.LabelField("Cell count: " + Mathf.Pow(mytarget.Cell_Subdivision, 3), bold);
							EditorGUILayout.EndHorizontal();
						}
					}
				}
				while (prop.NextVisible(false));
			}
			
			if (verticalgroups > 0)
			{
				verticalgroups--;
				EditorGUILayout.EndVertical();
			}


			EditorGUI.indentLevel = 0;
			switch (mytarget.MeshSurfaceAlgorithm)
            {
                case SurfaceAlgorithm.None:
                    break;
                case SurfaceAlgorithm.MarchingCubes:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleMarchingCubes"), true);
					break;
                case SurfaceAlgorithm.Block:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleCubic"), true);
					break;
                case SurfaceAlgorithm.Crystal:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleCrystal"), true);

					break;
				case SurfaceAlgorithm.Mesh:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleMeshOnly"), true);

					break;
				default:
                    break;
            }

           


			EditorGUILayout.PropertyField(serializedObject.FindProperty("PostProcessModules"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("DetailGenerator"), true);

            Tuple<List<Type>, string[]> algorithms = FraktaliaEditorUtility.GetDerivedTypesForScriptSelection(typeof(ModularHullDetail), "Add algorithm...");

			EditorGUILayout.BeginHorizontal();

			int selectalgorithm = EditorGUILayout.Popup(0, algorithms.Item2);

			VoxelGenerator generator = mytarget.GetComponentInParent<VoxelGenerator>();
			if (generator)
			{
				if (GUILayout.Button("Scan existing details"))
				{
					mytarget.engine = generator;
					mytarget.DetailGenerator = new List<ModularHullDetail>();
					mytarget.DetailGenerator.AddRange(mytarget.engine.GetComponentsInChildren<ModularHullDetail>());

				}
			}
			EditorGUILayout.EndHorizontal();

			if (selectalgorithm > 0)
			{
				GameObject NewPostProcess = new GameObject("NEW_" + algorithms.Item2[selectalgorithm]);
				NewPostProcess.AddComponent(algorithms.Item1[selectalgorithm]);
				NewPostProcess.transform.parent = mytarget.transform.parent;
				mytarget.DetailGenerator.Add(NewPostProcess.GetComponent<ModularHullDetail>());
			}

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("DebugMode"), true);

			if (GUI.changed)
			{
				serializedObject.ApplyModifiedProperties();
				for (int i = 0; i < mytarget.PostProcessModules.Count; i++)
				{					
					mytarget.PostProcessModules[i].ConvertToDerivate();
				}
				
				EditorUtility.SetDirty(target);
			}



		}


	}


#endif

}