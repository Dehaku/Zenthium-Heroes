using System.Diagnostics;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Fraktalia.VoxelGen.Modify;
using Fraktalia.VoxelGen.Visualisation;
using Fraktalia.Core.FraktaliaAttributes;
using System.Collections;
using Fraktalia.VoxelGen.World;
using UnityEngine.XR;
using Fraktalia.VoxelGen.Modify.Procedural;
using System.Reflection;
using Fraktalia.Utility;
using System.Runtime.CompilerServices;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;
#endif


namespace Fraktalia.VoxelGen
{
	[ExecuteInEditMode]
	public unsafe class VoxelGenerator : MonoBehaviour
	{
		public const int MINCAPACITY = 1000;
		private static bool AsyncLock;

#if VOXEL_DEBUG
		public VoxelTreeDebugInformation[] DebugInformation;
#endif

		[BeginInfo("VOXELGENERATOR", order = -99999)]
		[InfoTitle("Voxel Generator", "Hello. I am the main component and my responsibility is to provide the data for hull generators, voxel modifiers, world generators and " +
		"all your scripts which want to read or write voxels. Therefore I provide access to the voxel trees and functions called GetVoxel and SetVoxel to modify the dataset. " +
		"I am not responsible for visualizing voxels as I am lazy and like to delegate work to hull generators and voxel modifiers.\n\n" +
		"<b>Also my internal parts use Burst so add it to your project by clicking on the install buttons I even provide for you.</b>\n\n" +
		"The prefabs contain the voxel engine, the save system and a voxel modifier. However I can work fully independent.", "VOXELGENERATOR")]
		[InfoSection1("How to use:", "When adding me to a game object, define the Volume I should occupy using the <b>Volume Size</b>. Then click the generate button " +
			"or call GenerateBlock from any script. Click or call CleanUp to destroy the volume.\n\n" +
			"In order to see any results, hull generators must be attached and set up correctly because the generator itself is not responsible for the visualisation. " +
			"Also it is possible to apply sub systems like the save system for saving/loading and world generator for procedural world generation. " +
			"", "VOXELGENERATOR")]
		[InfoSection2("General Settings:", "" +
			"<b>Volume Size:</b> Size of the volume.\n" +
			"<b>Subdivision Power:</b> Defines the layout of the voxel tree structure. 2 = lowest memory requirement, 4 = faster reading/writing of voxels.\n" +
			"<b>Dimension Count:</b> How many voxel trees the generator should have. First one is usually the density, second one can be used for texture value or other usages.\n" +
			"<b>Initial Value:</b> Initial value of the first tree. Initial value of additional trees is always 0. \n" +
			"", "VOXELGENERATOR")]
		[InfoSection3("Other Parameters::", "" +
			"<b>Hull Frame Skip:</b> Value will decrement itself each frame. Hull Generators are only updated when value is 0\n" +
			"<b>Lock Hull Updates:</b> Hull generators will not be updated when voxels are modified.\n" +
			"<b>Locked:</b> Nothing can modify voxels while it is locked. Hull Generators will not be updated.\n\n" +
			"Applied Subsystems are set automatically and show which subsystems are attached. LOD Distance can be used by hull generators and is set automatically based on the distance. \n\n" +
			"<b>Memory Optimize:</b> Optimizes the memory consumtion significantly but slightly lowers performance. Recommended = true\n\n" +
			"<color=blue>The voxel generator is the main component of the whole asset. There is a youtube tutorial video for every feature included. The button below will lead to the complete playlis.</color>", "VOXELGENERATOR")]
		[InfoVideo("https://www.youtube.com/watch?v=SmcrkAQBgTs&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=3&t=1s", false, "VOXELGENERATOR")]
		[InfoText("Voxel Generator "+VERSION.NUMBER, "VOXELGENERATOR")]

		[Tooltip("Hull generators attached as children to this generator. Is fetched automatically when block is initialized.")]	
		public BasicNativeVisualHull[] hullGenerators = new BasicNativeVisualHull[0];

		[TitleText("General Settings:", TitleTextType.Title)]
		[LabelText("Volume Size")] [Tooltip("Size of the volume in Unity units")]
		public float RootSize = 10;

		[Range(2, 8)][Tooltip("Defines the layout of the voxel tree structure. 2 = lowest memory requirement, 4 = faster reading/writing of voxels. When this is increased, Depth should be decreased.")]
		public int SubdivisionPower = 2;
		public NativeVoxelTree[] Data;

		[Range(1, 10)][Tooltip("How many separate voxel data. First one is usually the density which is used for geometry generation, second one can be used for texture value or other usages")]
		public int DimensionCount = 1;
		

		[Range(0, 255)][Tooltip(" Initial value of the first voxel data which can be used as baseline onto which WorldAlgorithm can carve out voxels. Initial value of additional trees is always 0.")]
		public int InitialValue = 160;

		[HideInInspector]
		public bool[] RestrictDimension;

		[TitleText("Lock Control Settings:", TitleTextType.Title)]
		[Tooltip("When ON, the hull generators will not be updated when voxels are modified.")]
		public bool LockHullUpdates = false;

		[NonSerialized]
		public bool Locked = false;

		[TitleText("Applied Subsystems:", TitleTextType.Title)]
		public VoxelSaveSystem savesystem;
		public WorldGenerator worldgenerator;

		private bool voxeltreedirty;
		public bool VoxelTreeDirty
		{
			get
			{
				return voxeltreedirty;
			}
			private set
			{
				voxeltreedirty = value;
			}
		}

		public bool IsUpdatingVoxelTree { get; private set; }
		public bool HullsDirty { get; set; }
		public bool VisualsHidden { get; private set; }

		public UpdateVoxelTree_Job[] setadditivejob;

		public NativeVoxelReservoir[] memoryReservoir = new NativeVoxelReservoir[0];

		[TitleText("Other Settings:", TitleTextType.Title)]
		[Tooltip("Distance index for LOD(Level of Detail) features. Can be used by hull generators to determine resolution.")]
		public int LODDistance = 0;
		[HideInInspector] [NonSerialized] public int CurrentLOD;

		[Tooltip("Optional chunk position Information which is used by world generators, save systems, etc. Usually represents the compute offset.")]
		public Vector3Int ChunkHash;

		[Tooltip("Greatly optimizes engine for minimal memory requirement. " +
			"What it does: Keeps capacities of native lists in memory pool and set/setadditive modules below 1024. " +
			"Therefore slightly reduces performance when modifying more than 1024 voxels at the same time." +
			"Hull Generators are not affected by this!.\n\nYou trade about 1% performance for 50%+ less memory consumption.")]
		public bool MemoryOptimized = true;
		[Tooltip("Doesn't delete hulls on cleanup.")]
		public bool KeepHulls;
		[Tooltip("When true, hulls are generated from the init point outwards during initialisation.")]
		public bool UseInitPoint;
		public Vector3 Initpoint;		
		[Tooltip("Draws debug information if enabled.")]
		public bool DebugMode;

		[NonSerialized]
		public VoxelGenerator[] Neighbours;

		[HideInInspector]
		public int instanceID;

		[HideInInspector]
		public bool IsExtension;

		public IEnumerator UpdateVoxelTreeRoutine;
		public IEnumerator UpdateRegionsStepwise;
	
		/// <summary>
		/// Property which is true, if the voxel block has been generated. Else it is false.
		/// </summary>
		public bool IsInitialized { get; private set; }

		/// <summary>
		/// Immediately finish ongoing works. Causes performance spike and is supposed to be used during Scene loading
		/// </summary>
		[NonSerialized]
		public bool Forcefinish;

		/// <summary>
		/// If this is true, updating hull generators is suppressed for one frame.
		/// </summary>
		[NonSerialized]
		public bool SupressPostProcess;

		private JobHandle[] jobHandles;
		private bool[] jobHandlesProcessing;
		private Queue<VoxelRegion> regionstoupdate;
		private float currentRootSize;
		private bool currentWorking;
		private int idlecount = 0;

		/// <summary>
		/// Returns true if all attached hull generators are valid.
		/// </summary>
		public bool IsValid
		{
			get
			{
				if (hullGenerators == null) return false;

				for (int i = 0; i < hullGenerators.Length; i++)
				{
					if (hullGenerators[i] == null) return false;
					if (!hullGenerators[i].IsValid()) return false;
				}

				if (!currentRootSize.Equals(RootSize)) return false;

				return true;
			}
		}
		public bool HullGeneratorsWorking
		{
			get
			{
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					if (hullGenerators[i])
						if (hullGenerators[i].IsWorking()) return true;
				}
				return false;
			}
		}
		public bool IsIdle
		{
			get
			{
				return !HullGeneratorsWorking && IsInitialized && !VoxelTreeDirty && regionstoupdate.Count == 0;
			}
		}

        #region Initialisation  
        /// <summary>
        /// Generates a fresh voxel block using the initialValue as starting parameter
        /// </summary>
        public void GenerateBlock()
		{
			currentRootSize = RootSize;
			SubdivisionPower = Mathf.ClosestPowerOfTwo(SubdivisionPower);

			CurrentLOD = -1;
			RestrictDimension = new bool[DimensionCount];
			jobHandlesProcessing = new bool[DimensionCount];
			
			regionstoupdate = new Queue<VoxelRegion>();
			hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();
			
			if (hullGenerators == null) return;
			if (HullGeneratorsWorking) return;
			
			

			CleanUp();
	
			jobHandles = new JobHandle[DimensionCount];
			if (!IsExtension)
			{
				worldgenerator = GetComponent<WorldGenerator>();
				if (worldgenerator)
				{
					worldgenerator.Initialize(this);
				}
				savesystem = GetComponent<VoxelSaveSystem>();
				if (savesystem)
				{
					savesystem.nativevoxelengine = this;
					savesystem.IsDirty = false;
				}
			}
			UpdateVoxelTreeRoutine = updateVoxelTrees();
#if VOXEL_DEBUG
			DebugInformation = new VoxelTreeDebugInformation[DimensionCount];
#endif

			memoryReservoir = new NativeVoxelReservoir[DimensionCount];
            for (int i = 0; i < DimensionCount; i++)
            {
				memoryReservoir[i].Initialize(this);
			}
			
			Data = new NativeVoxelTree[DimensionCount];
			Data[0]._Initialize(memoryReservoir[0], RootSize, SubdivisionPower, (byte)InitialValue, 64);
			for (int i = 1; i < Data.Length; i++)
			{
				Data[i]._Initialize(memoryReservoir[i], RootSize, SubdivisionPower, (byte)0, 64);
			}

			float Rootpos = Data[0].RootSize;
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].engine = this;
				hullGenerators[i].gameObject.layer = gameObject.layer;
				hullGenerators[i].InitVisualHull();

				hullGenerators[i].transform.localPosition = new Vector3();
				
				if (hullGenerators[i].LockAfterInit) hullGenerators[i].Locked = true;
			}
			NativeVoxelNode root;
			Data[0]._GetVoxel(0, 0, 0, 0, out root);
			setadditivejob = new UpdateVoxelTree_Job[DimensionCount];
			for (int i = 0; i < Data.Length; i++)
			{
				setadditivejob[i].data = Data[i];
				setadditivejob[i].Init(this);
			}

			if (Neighbours != null)
			{
				VoxelGenerator[] OldNeighbours = new VoxelGenerator[Neighbours.Length];
				for (int i = 0; i < OldNeighbours.Length; i++)
				{
					OldNeighbours[i] = Neighbours[i];
				}
				for (int i = 0; i < OldNeighbours.Length; i++)
				{
					if (OldNeighbours[i] != null)
					{
						SetNeighbor(i, OldNeighbours[i]);
					}
				}
			}
			else
			{
				Neighbours = new VoxelGenerator[27];
			}
			
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].NodeChanged(root);
			}
			
			if (UseInitPoint)
			{
				UpdateRegionsStepwise = SetAllDirty_Stepwise(Initpoint);
			}
			else
				SetAllRegionsDirty();
			IsInitialized = true;

            ContainerStaticLibrary.OnBeforeCleanUp += ContainerStaticLibrary_OnBeforeCleanUp;
		}

       

        private void initVisualHulls()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].engine = this;
				hullGenerators[i].InitVisualHull();
				if (hullGenerators[i].LockAfterInit) hullGenerators[i].Locked = true;
			}
		}
		#endregion

		#region VoxelManipulation
		public void ResetData()
		{

			for (int i = 0; i < setadditivejob.Length; i++)
			{
				setadditivejob[i].changedata_additive.Clear();
				setadditivejob[i].changedata_set.Clear();
				setadditivejob[i].changedata_set_inner.Clear();
				setadditivejob[i].changedata_additive_inner.Clear();
			}

			if (IsUpdatingVoxelTree)
			{
				UnityEngine.Debug.LogWarning("Reset data request rejected! Wait until the generator has finished updating his voxel tree! This would have crashed your app!");
				return;
			}
			for (int i = 0; i < Data.Length; i++)
			{
				NativeVoxelReset_Job reset_Job = new NativeVoxelReset_Job();
				reset_Job.data = Data[i];
				reset_Job.InitialID = 0;
				reset_Job.Schedule().Complete();
			}
		}
		public void ResetDimension(int dimension, byte ID)
		{
			if (!IsInitialized) return;
			if (dimension < 0 || dimension >= Data.Length)
			{
				throw new IndexOutOfRangeException("Dimension " + dimension + " does not exist on generator " + this);
			}

			for (int i = 0; i < setadditivejob.Length; i++)
			{
				setadditivejob[i].changedata_additive.Clear();
				setadditivejob[i].changedata_set.Clear();
				setadditivejob[i].changedata_set_inner.Clear();
				setadditivejob[i].changedata_additive_inner.Clear();
			}

			if (IsUpdatingVoxelTree)
			{
				UnityEngine.Debug.LogWarning("Reset data request rejected! Wait until the generator has finished updating his voxel tree! This would have crashed your app!");
				return;
			}

			NativeVoxelReset_Job reset_Job = new NativeVoxelReset_Job();
			reset_Job.data = Data[dimension];
			reset_Job.InitialID = ID;
			reset_Job.Schedule().Complete();

			Rebuild();
		}
		public void _SetVoxel(Vector3 localposition, byte depth, byte id, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			NativeVoxelModificationData change;
			change.X = localposition.x;
			change.Y = localposition.y;
			change.Z = localposition.z;
			change.Depth = depth;
			change.ID = id;

			setadditivejob[dimension].changedata_set.Add(change);
			VoxelTreeDirty = true;
		}
		public void _SetVoxel(NativeVoxelModificationData voxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_set.Add(voxeldata);
			VoxelTreeDirty = true;
		}
		//Slow method as it must build the modification data.
		public void _SetVoxels(List<Vector3> localposition, List<byte> depth, List<int> id, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;

			for (int i = 0; i < localposition.Count; i++)
			{
				NativeVoxelModificationData change;
				change.X = localposition[i].x;
				change.Y = localposition[i].y;
				change.Z = localposition[i].z;
				change.Depth = depth[i];
				change.ID = id[i];

				setadditivejob[dimension].changedata_set.Add(change);
			}
			VoxelTreeDirty = true;
		}
		//Fast method as it just copies array over into the changedata.
		public void _SetVoxels(NativeVoxelModificationData[] rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;

			fixed (void* start = &rawvoxeldata[0])
			{
				setadditivejob[dimension].changedata_set.AddRange(start, rawvoxeldata.Length);
			}

			VoxelTreeDirty = true;
		}
		public unsafe void _SetVoxels(byte[] rawvoxeldata, int voxelcount, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;

			fixed (void* start = &rawvoxeldata[0])
			{
				setadditivejob[dimension].changedata_set.AddRange(start, voxelcount);
			}

			VoxelTreeDirty = true;
		}
		public void _SetVoxels(NativeArray<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_set.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}

		public void _SetVoxels(FNativeList<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_set.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}

#if COLLECTION_EXISTS
        public void _SetVoxels(NativeList<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_set.AddRange(rawvoxeldata.AsArray());
			VoxelTreeDirty = true;
		}

		public void _SetVoxels_Inner(NativeList<NativeVoxelModificationData_Inner> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_set_inner.AddRange(rawvoxeldata.AsArray());
			VoxelTreeDirty = true;
		}
#endif

		public void _SetVoxels_Inner(NativeArray<NativeVoxelModificationData_Inner> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_set_inner.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}

		public void _SetVoxels_Inner(FNativeList<NativeVoxelModificationData_Inner> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_set_inner.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}
		public void _SetVoxels_Inner(List<NativeVoxelModificationData_Inner> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			for (int i = 0; i < rawvoxeldata.Count; i++)
			{
				setadditivejob[dimension].changedata_set_inner.Add(rawvoxeldata[i]);
			}
			
			VoxelTreeDirty = true;
		}
		public void _SetVoxelAdditive(Vector3 localposition, byte depth, int id, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			NativeVoxelModificationData change;
			change.X = localposition.x;
			change.Y = localposition.y;
			change.Z = localposition.z;
			change.Depth = depth;
			change.ID = id;

			setadditivejob[dimension].changedata_additive.Add(change);

			VoxelTreeDirty = true;
		}
		public void _SetVoxelsAdditive(List<NativeVoxelModificationData> localposition, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			for (int i = 0; i < localposition.Count; i++)
			{
				setadditivejob[dimension].changedata_additive.Add(localposition[i]);
			}
			VoxelTreeDirty = true;
		}
		public void _SetVoxelsAdditive(NativeArray<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_additive.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}
		public void _SetVoxelsAdditive(FNativeList<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_additive.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}

		public void _SetVoxelsAdditive_Inner(NativeArray<NativeVoxelModificationData_Inner> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_additive_inner.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}

		public void _SetVoxelsAdditive_Inner(FNativeList<NativeVoxelModificationData_Inner> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata_additive_inner.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}
		public void _SetVoxelsAdditive_Inner(List<NativeVoxelModificationData_Inner> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			for (int i = 0; i < rawvoxeldata.Count; i++)
			{
				setadditivejob[dimension].changedata_additive_inner.Add(rawvoxeldata[i]);
			}			
			VoxelTreeDirty = true;
		}
		#endregion

		#region Managed Voxel Read
		public int GetVoxelValueAt(Vector3 worldPosition, int dimension,int shrink = 0,  int border = 0)
        {
			Vector3Int inner = ConvertWorldToInner(worldPosition, RootSize);		
			return Data[dimension]._PeekVoxelId_InnerCoordinate(inner.x, inner.y, inner.z, 16, shrink, border);
		}
		#endregion

		#region Neighbors		
		public void SetNeighbor(int ID, VoxelGenerator neighbor)
		{
			Neighbours[ID] = neighbor;

			for (int i = 0; i < Data.Length; i++)
			{
				if (neighbor.Data != null)
				{
					Data[i].SetNeighbor(ID, ref neighbor.Data[i]);
				}
			}
		}
		public void RemoveNeighbor(int ID)
		{
			Neighbours[ID] = null;

			for (int i = 0; i < Data.Length; i++)
			{
				Data[i].RemoveNeighbor(ID);
			}
		}

		public void RemoveAllNeighbors()
		{
			if (Neighbours == null) return;
			for (int i = 0; i < Neighbours.Length; i++)
			{
				if (Neighbours[i])
				{
					Neighbours[i].RemoveNeighbor(26 - i);
				}
				RemoveNeighbor(i);
			}
		}
		#endregion

		#region RegionDirty
		public void Rebuild()
		{
			if (!IsInitialized) return;
			SetAllRegionsDirty();
		}

		/// <summary>
		/// Marks regions as dirty so the visuals get updated. Coordinates are in local position.
		/// </summary>
		/// <param name="center"></param>
		/// <param name="extends_min"></param>
		/// <param name="extends_max"></param>
		/// <param name="IgnoreNeighbor"></param>
		public void SetRegionsDirty(Vector3 center, Vector3 extends_min, Vector3 extends_max, int dimensionModified, bool IgnoreNeighbor = false)
		{
			VoxelRegion region;
			region.RegionMin = center - extends_min * 1.2f;
			region.RegionMax = center + extends_max * 1.2f;
			region.IgnoreNeighbor = IgnoreNeighbor;
			region.DimensionModified = dimensionModified;
			if (DebugMode)
			{
				Matrix4x4 matrix = transform.localToWorldMatrix;


				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMin), matrix.MultiplyPoint3x4(region.RegionMax), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMin), matrix.MultiplyPoint3x4(region.RegionMin + new Vector3(region.RegionMax.x - region.RegionMin.x, 0, 0)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMin), matrix.MultiplyPoint3x4(region.RegionMin + new Vector3(0, region.RegionMax.y - region.RegionMin.y, 0)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMin), matrix.MultiplyPoint3x4(region.RegionMin + new Vector3(0, 0, region.RegionMax.z - region.RegionMin.z)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMax), matrix.MultiplyPoint3x4(region.RegionMax - new Vector3(region.RegionMax.x - region.RegionMin.x, 0, 0)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMax), matrix.MultiplyPoint3x4(region.RegionMax - new Vector3(0, region.RegionMax.y - region.RegionMin.y, 0)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMax), matrix.MultiplyPoint3x4(region.RegionMax - new Vector3(0, 0, region.RegionMax.z - region.RegionMin.z)), Color.yellow, 5);
			}

			regionstoupdate.Enqueue(region);
		}

		public IEnumerator SetAllDirty_Stepwise(Vector3 center, int steps = 4)
		{


			Vector3 inclement = Vector3.one * RootSize / steps;





			for (int i = 0; i < steps; i++)
			{



				SetRegionsDirty(center, inclement * i, inclement * i, -1, true);
				yield return null;
				while (regionstoupdate.Count > 0)
				{
					yield return null;
				}
			}


		}

		public void SetRegionsDirty(Vector3 region_min, Vector3 region_max, int dimension, bool IgnoreNeighbor = false)
		{
			Bounds bound = new Bounds();
			bound.min = region_min;
			bound.max = region_max;

			SetRegionsDirty(bound.center, bound.size/2, bound.size/2, dimension, IgnoreNeighbor);
		}

		public void SetAllRegionsDirty()
		{
			regionstoupdate.Clear();
			SetRegionsDirty(Vector3.zero, Vector3.one * RootSize,-1, true);
		}
		#endregion

		#region Export
		[HideInInspector]
		public bool __export_foldout = false;
		[HideInInspector]
		public string __export_savePath = "Assets/";
		[HideInInspector]
		public string __export_meshName = "ExportedMesh";
		[HideInInspector]
		public Vector3 __export_center = new Vector3(-0.5f, -0.5f, -0.5f);

		public Mesh ExtractHull(BasicNativeVisualHull hullgenerator, Vector3 center)
		{
			Mesh output = new Mesh();
			output.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

			MeshFilter[] pieces = hullgenerator.GetComponentsInChildren<MeshFilter>();

			CombineInstance[] meshes = new CombineInstance[pieces.Length];
			for (int i = 0; i < pieces.Length; i++)
			{
				meshes[i].mesh = pieces[i].sharedMesh;

				Vector3 offset = center * RootSize;

				Matrix4x4 offsetmatrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);

				meshes[i].transform = pieces[i].transform.localToWorldMatrix * hullgenerator.transform.worldToLocalMatrix * offsetmatrix;
			}
			output.CombineMeshes(meshes);
			output.Optimize();
			output.OptimizeIndexBuffers();
			output.OptimizeReorderVertexBuffer();

			if (output.vertexCount > 0)
			{
#if UNITY_EDITOR
				Unwrapping.GenerateSecondaryUVSet(output);
#endif
			}
			return output;
		}
        #endregion

        #region VoxelDataInformation  
        public float GetVoxelSize(int depth)
		{
			return RootSize / Mathf.Pow(SubdivisionPower, depth);
		}

		public int GetInnerVoxelSize(int depth)
		{
			return NativeVoxelTree.INNERWIDTH / (int)Mathf.Pow(SubdivisionPower, depth);
		}

		public int GetBlockCount(int depth)
		{
			int size = (int)Mathf.Pow(SubdivisionPower, depth);

			return size * size * size;
		}

		public int GetBlockWidth(int depth)
		{
			int size = (int)Mathf.Pow(SubdivisionPower, depth);

			return size;
		}
		#endregion

		#region UpdateGenerator  
		void OnDrawGizmosSelected()
		{
			if (IsExtension) return;

			hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();

#if UNITY_EDITOR
			OnDuplicate();
#endif


			if (!IsInitialized)
			{
				CleanUp();
			}
			Gizmos.color = Color.blue;
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize));
		}

		private void OnDrawGizmos()
		{

#if UNITY_EDITOR

			if (Mathf.ClosestPowerOfTwo(SubdivisionPower) != SubdivisionPower)
			{
				Gizmos.color = Color.red;
				Gizmos.matrix = transform.localToWorldMatrix;
				for (int i = 0; i < 5; i++)
				{
					Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize) * (1.01f + i * 0.03f));
				}

			}


			if (EditorApplication.isCompiling)
			{
				CleanUp();

				CleanUpStatics();
			}

			/*
			if (PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.Regular)
			{
				PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);


				hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					if (PrefabUtility.GetPrefabAssetType(hullGenerators[i]) == PrefabAssetType.Regular)
					{
						PrefabUtility.UnpackPrefabInstance(hullGenerators[i].gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
					}

				}
			}
			*/

			if (!worldgenerator) worldgenerator = GetComponent<WorldGenerator>();
			if (!savesystem) savesystem = GetComponent<VoxelSaveSystem>();

#endif



			if (!IsExtension)
			{
				if (IsInitialized && !Application.isPlaying)
				{
					Update();
				}
			}
		}

		public void Update()
		{
			if (IsInitialized && !IsValid)
			{
				hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();

				for (int i = 0; i < hullGenerators.Length; i++)
				{
					hullGenerators[i].transform.localPosition = new Vector3();
					hullGenerators[i].gameObject.layer = gameObject.layer;

				}

				for (int i = 0; i < Data.Length; i++)
				{
					Data[i].UpdateScale(RootSize);
					setadditivejob[i].data = Data[i];
				}

				currentRootSize = RootSize;

				cleanHulls();
				cleanVisuals();
				initVisualHulls();
				Rebuild();

				
			}



			if (!IsInitialized) return;

			if (!IsExtension)
			{
				if (worldgenerator)
				{
					if (!worldgenerator.IsInitialized) worldgenerator.Initialize(this);

					worldgenerator.UpdateRoutines();
				}

				if (savesystem)
				{
					savesystem.UpdateRoutines();
				}
			}



			if (Locked) return;


#if VOXEL_DEBUG
			for (int i = 0; i < DimensionCount; i++)
			{
				DebugInformation[i] = Data[i].DebugInformation[0];
			}
#endif



			if (UpdateVoxelTreeRoutine == null)
			{
				return;
			}

			if (UpdateRegionsStepwise != null)
			{
				UpdateRegionsStepwise.MoveNext();
			}

			UpdateVoxelTreeRoutine.MoveNext();

		}

		public void updateRegions()
		{
			if (VoxelTreeDirty) return;

			while (regionstoupdate.Count > 0)
			{
				HullsDirty = true;
				VoxelRegion region = regionstoupdate.Dequeue();
				for (int k = 0; k < hullGenerators.Length; k++)
				{
					if (hullGenerators[k].Locked) continue;
					hullGenerators[k].SetRegionsDirty(region);
				}

				if (!region.IgnoreNeighbor)
				{
					Vector3Int index_min = new Vector3Int(0, 0, 0);
					Vector3Int index_max = new Vector3Int(0, 0, 0);

					//MAXIMUM
					if (region.RegionMax.x >= RootSize)
					{
						index_max.x = 1;
					}

					if (region.RegionMax.y >= RootSize)
					{
						index_max.y = 1;

					}

					if (region.RegionMax.z >= RootSize)
					{
						index_max.z = 1;
					}


					//MINIMUM
					if (region.RegionMin.x < 0)
					{
						index_min.x = -1;

					}

					if (region.RegionMin.y < 0)
					{
						index_min.y = -1;

					}

					if (region.RegionMin.z < 0)
					{
						index_min.z = -1;

					}


					for (int x = index_min.x; x <= index_max.x; x++)
					{
						for (int y = index_min.y; y <= index_max.y; y++)
						{
							for (int z = index_min.z; z <= index_max.z; z++)
							{
								VoxelRegion neighborregion = region;
								neighborregion.RegionMin.x += -x * RootSize;
								neighborregion.RegionMin.y += -y * RootSize;
								neighborregion.RegionMin.z += -z * RootSize;
								neighborregion.RegionMax.x += -x * RootSize;
								neighborregion.RegionMax.y += -y * RootSize;
								neighborregion.RegionMax.z += -z * RootSize;

								neighborregion.IgnoreNeighbor = true;

								VoxelGenerator neighbour = Neighbours[13 + (x + y * 3 + z * 9)];
								if (neighbour)
								{
									neighbour.regionstoupdate.Enqueue(neighborregion);
								}
							}
						}
					}
				}
			}
		}
		public IEnumerator updateVoxelTrees()
		{
			

			while (IsInitialized)
			{

				if(idlecount > 2 && idlecount < 5)
                {
					if (MemoryOptimized && IsIdle)
					{
						for (int i = 0; i < memoryReservoir.Length; i++)
						{
							memoryReservoir[i].ResetGarbage();
						}
					}

					if (IsIdle)
					{
						for (int i = 0; i < hullGenerators.Length; i++)
						{
							hullGenerators[i].PostProcess();

							if(!hullGenerators[i].IsWorking())							
							{
								hullGenerators[i].IsIdle = true;
							}
						}			
					}
				}
			
				if (IsIdle)
				{
					idlecount++;
				}

				if (!VoxelTreeDirty || HullsDirty)
				{
					yield return null;
				}
                else
                {
					idlecount = 0;
				}
				
				

				for (int i = 0; i < Data.Length; i++)
				{
					UpdateVoxelTree_Job updatevoxeltree = setadditivejob[i];
					updatevoxeltree.data = Data[i];
					if (updatevoxeltree.HasWork())
                    {
						jobHandlesProcessing[i] = true;					
						updatevoxeltree.Prepare();
						jobHandles[i] = updatevoxeltree.Schedule();					
					}
				}

				for (int i = 0; i < Data.Length; i++)
				{
					UpdateVoxelTree_Job updatevoxeltree = setadditivejob[i];
					if (jobHandlesProcessing[i])
					{
						while (!jobHandles[i].IsCompleted)
						{
							IsUpdatingVoxelTree = true;
							yield return null;
						}
						jobHandles[i].Complete();

						jobHandlesProcessing[i] = false;
						for (int k = 0; k < hullGenerators.Length; k++)
						{
							hullGenerators[k].NodesDestroyed(ref Data[i].destroyedNodes);
							hullGenerators[k].NodesChanged(ref updatevoxeltree.result_final);
						}

						updatevoxeltree.result_final.Clear();
						updatevoxeltree.result_set.Clear();
						updatevoxeltree.result_final_inner.Clear();
						updatevoxeltree.result_set_inner.Clear();

						if (MemoryOptimized)
						{
							updatevoxeltree.result_final.Capacity = MINCAPACITY;
							updatevoxeltree.result_set.Capacity = MINCAPACITY;
							updatevoxeltree.result_final_inner.Capacity = MINCAPACITY;
							updatevoxeltree.result_set_inner.Capacity = MINCAPACITY;
						}
					}

					Data[i].destroyedNodes.Clear();
				}

				

				VoxelTreeDirty = false;
				IsUpdatingVoxelTree = false;

				updateRegions();

				
				IEnumerator function = updateHulls();
				while (true)
				{
					function.MoveNext();
					if (function.Current == null)
					{
						break;
					}
					else yield return null;
				}
				updateLOD();
			}
		}

		public IEnumerator updateHulls()
		{
			if (!HullsDirty || LockHullUpdates)
			{
				yield return null;
			}
			idlecount = 0;

			if (SupressPostProcess)
			{
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					hullGenerators[i].NoPostProcess = true;
				}
				SupressPostProcess = false;
			}

			if (Forcefinish)
			{
				finishVisuals();
				Forcefinish = false;
			}

			while (AsyncLock && IsInitialized)
			{
				yield return new YieldInstruction();
			}

			for (int i = 0; i < hullGenerators.Length; i++)
			{
				if (hullGenerators[i].IsWorking())
				{
					hullGenerators[i].IsIdle = false;			
				}
				hullGenerators[i].PrepareWorks();
			}

			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].CompleteWorks();
			}

			AsyncLock = true;
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				BasicNativeVisualHull hull = hullGenerators[i];
				IEnumerator function = hull.CalculateHullAsync();
				while (true)
				{
					function.MoveNext();

					if (function.Current == null)
					{
						break;
					}
					else
					{

						yield return new YieldInstruction();
					}
				}
			}
			AsyncLock = false;

			bool hullsdirty = false;
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				if (hullGenerators[i].IsWorking())
				{
					
					hullsdirty = true;
				}			
			}
			HullsDirty = hullsdirty;

			yield return null;
		}

		public void updateLOD()
		{
			if (CurrentLOD != LODDistance)
			{
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					hullGenerators[i].UpdateLOD(LODDistance);
				}

				CurrentLOD = LODDistance;
			}
		}
		#endregion

        #region Visualisation   
        public void HideVisuals()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].HideMeshes();
			}
			VisualsHidden = true;
		}

		public void ShowVisuals()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].ShowMeshes();
			}
			VisualsHidden = false;
		}

		public void SetLOD(int lod)
		{
			LODDistance = lod;
		}
        #endregion

        


        #region Cleaning	

        private void ContainerStaticLibrary_OnBeforeCleanUp()
		{
			CleanUp();
			ContainerStaticLibrary.OnBeforeCleanUp -= ContainerStaticLibrary_OnBeforeCleanUp;
		}

		public static void CleanUpStatics()
		{
			ContainerStaticLibrary.CleanUp();
			VoxelUtility.CleanUp();
			MarchingCubes_Data.DisposeMarchingCubeInformation();
			Cubic_Data.DisposeStaticInformation();
			VoxelSaveSystem.CleanStatics();
			VoxelModifier_V2.CleanAll();
			VoxelUndoSystem.Dispose();
		}
		public void CleanUp()
		{
			if (IsInitialized)
			{
				RemoveAllNeighbors();
			}

			IsInitialized = false;

			finishRoutines();


			if (this != null)
				hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();

			if (hullGenerators != null)
			{
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					if (hullGenerators[i].LockAfterInit) hullGenerators[i].Locked = false;
					hullGenerators[i].CleanUp();
				}
				cleanVisuals();
			}

			if (Data != null)
			{
				for (int i = 0; i < Data.Length; i++)
				{
					if (Data[i].IsCreated) Data[i].Dispose(this);

#if VOXEL_DEBUG
					if (Data[i].DebugInformation.IsCreated)
					{
						DebugInformation[i] = Data[i].DebugInformation[0];
						Data[i].DebugInformation.Dispose();
					}
#endif
				}
			}

			if (setadditivejob != null)
			{
				for (int i = 0; i < setadditivejob.Length; i++)
				{
					setadditivejob[i].CleanUp();
				}
			}

			if (!IsExtension)
			{
				if (worldgenerator) worldgenerator.CleanUp();
			}

            for (int i = 0; i < memoryReservoir.Length; i++)
            {
				memoryReservoir[i].CleanUp();
			}			
		}
		public void ClearMeshes()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].ClearMeshes();
			}
		}
		private void cleanHulls()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].CleanUp();
			}
		}
		private void cleanVisuals()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].CleanVisualisation();
			}
		}
		private void finishVisuals()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].FinishAllWorks();
			}
		}
		public void finishRoutines()
		{
			if (UpdateVoxelTreeRoutine != null)
			{
				while (UpdateVoxelTreeRoutine.MoveNext())
				{
					IsInitialized = false;
				}
			}
		}
		private void OnDestroy()
		{
			CleanUp();
		}
		public void OnDuplicate()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)//if in the editor
			{

				//if the instance ID doesnt match then this was copied!
				if (instanceID != gameObject.GetInstanceID())
				{
					UnityEngine.Object orig = EditorUtility.InstanceIDToObject(instanceID);

					try
					{
						GameObject original = (GameObject)orig;
						if (original != null)
						{
							VoxelGenerator originalgenerator = original.GetComponent<VoxelGenerator>();

							for (int i = 0; i < hullGenerators.Length; i++)
							{
								hullGenerators[i].OnDuplicate();
							}

							RawVoxelData olddata = VoxelSaveSystem.ConvertRaw(originalgenerator);
							VoxelSaveSystem.ApplyRawVoxelData(this, olddata);

							VoxelSaveSystem oldsave = originalgenerator.GetComponent<VoxelSaveSystem>();
							if (oldsave && oldsave.ModuleScriptableObject._VoxelMap != null)
							{
								VoxelMap voxeldata = oldsave.ModuleScriptableObject._VoxelMap;

								if (oldsave.EditorSaveMode == EditorVoxelSaveMode.ScriptableObject && oldsave.ModuleScriptableObject.DuplicateMapOnClone)
								{
									string path = AssetDatabase.GetAssetPath(voxeldata);
									string name = voxeldata.name;
									string newpath = path.Replace(name, "VoxelMap_" + GetInstanceID());

									AssetDatabase.CopyAsset(path, newpath);
									AssetDatabase.SaveAssets();
									GetComponent<VoxelSaveSystem>().ModuleScriptableObject._VoxelMap = AssetDatabase.LoadAssetAtPath<VoxelMap>(newpath);
									oldsave.Save();
								}
							}



						}
					}
					catch
					{

					}

					instanceID = gameObject.GetInstanceID();
				}
			}
#endif
		}
		#endregion

		#region COORDINATE CONVERTERS
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ConvertLocalToInner(float localCoordinate, float rootSize)
		{
			return (int)((localCoordinate / rootSize) * NativeVoxelTree.INNERWIDTH);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ConvertInnerToLocal(int innerCoordinate, float rootSize)
		{
			return (float)innerCoordinate / (float)NativeVoxelTree.INNERWIDTH * rootSize;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3Int ConvertLocalToInner(Vector3 localCoordinate, float rootSize)
		{
			return new Vector3Int(
				(int)((localCoordinate.x / rootSize) * NativeVoxelTree.INNERWIDTH),
				(int)((localCoordinate.y / rootSize) * NativeVoxelTree.INNERWIDTH),
				(int)((localCoordinate.z / rootSize) * NativeVoxelTree.INNERWIDTH)
				);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ConvertInnerToLocal(Vector3Int innerCoordinate, float rootSize)
		{
			return new Vector3(
				(float)innerCoordinate.x / (float)NativeVoxelTree.INNERWIDTH * rootSize,
				(float)innerCoordinate.y / (float)NativeVoxelTree.INNERWIDTH * rootSize,
				(float)innerCoordinate.z / (float)NativeVoxelTree.INNERWIDTH * rootSize);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3Int ConvertWorldToInner(Vector3 worldCoordinate, float rootSize)
		{
			Vector3 localCoordinate = transform.worldToLocalMatrix.MultiplyPoint3x4(worldCoordinate);

			return new Vector3Int(
				(int)((localCoordinate.x / rootSize) * NativeVoxelTree.INNERWIDTH),
				(int)((localCoordinate.y / rootSize) * NativeVoxelTree.INNERWIDTH),
				(int)((localCoordinate.z / rootSize) * NativeVoxelTree.INNERWIDTH)
				);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 ConvertInnerToWorld(Vector3Int innerCoordinate, float rootSize)
		{
			return transform.localToWorldMatrix.MultiplyPoint3x4( new Vector3(
				(float)innerCoordinate.x / (float)NativeVoxelTree.INNERWIDTH * rootSize,
				(float)innerCoordinate.y / (float)NativeVoxelTree.INNERWIDTH * rootSize,
				(float)innerCoordinate.z / (float)NativeVoxelTree.INNERWIDTH * rootSize));
		}



		#endregion

		#region Misc
		public void ClearRestrictions()
		{
			for (int i = 0; i < RestrictDimension.Length; i++)
			{
				RestrictDimension[i] = false;
			}
		}
		public void SetCapacity(int count)
		{
			for (int i = 0; i < setadditivejob.Length; i++)
			{
				setadditivejob[i].SetCapacity(count);
			}
		}
		#endregion
	}

    public struct VoxelRegion
	{
		public Vector3 RegionMin;
		public Vector3 RegionMax;
		public bool IgnoreNeighbor;
		public int DimensionModified;
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(VoxelGenerator))][CanEditMultipleObjects]
	public class VoxelGeneratorEditor : Editor
	{
		

		private void OnSceneGUI()
		{
			VoxelGenerator myTarget = target as VoxelGenerator;

			if (myTarget.__export_foldout)
			{
				Vector3 pos = myTarget.__export_center + Vector3.one * 0.5f;
				Handles.DrawWireCube(myTarget.transform.position + pos * myTarget.RootSize, Vector3.one * myTarget.RootSize);
			}

		}

		public override void OnInspectorGUI()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;


			EditorStyles.textField.wordWrap = true;

			VoxelGenerator myTarget = (VoxelGenerator)target;


			HandleErrors();

			if (myTarget.Locked)
			{
				EditorGUILayout.LabelField("<color=red>GENERATOR IS LOCKED!:</color>", title);
				EditorGUILayout.TextArea("This generator was locked by external scripts." +
					" Usually the generator is locked when content affecting the whole generator is processed, where interruption is forbidden." +
					" Generator is locked when the save system is loading or saving data or when a world generator is generating data for this generator. " +
					" \n\nYou cannot Create/Modify or Destroy the generator while locked!");

				if (GUILayout.Button("Force Unlock! (Can cause errors)"))
				{
					myTarget.Locked = false;
				}
			}

			myTarget.SubdivisionPower = Mathf.ClosestPowerOfTwo(myTarget.SubdivisionPower);



			DrawDefaultInspector();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Information:", bold);
			EditorGUILayout.LabelField("Is Initialized: " + myTarget.IsInitialized);
			if (myTarget.IsInitialized)
			{
				EditorGUILayout.LabelField("Hull Generators Working: " + myTarget.HullGeneratorsWorking);
			}
			EditorGUILayout.LabelField("Hulls Dirty: " + myTarget.HullsDirty);
			EditorGUILayout.LabelField("Is Idle: " + myTarget.IsIdle);
			EditorGUILayout.LabelField("Is Extension: " + myTarget.IsExtension);

			if (myTarget.IsInitialized)
			{
				if(myTarget.DimensionCount != myTarget.memoryReservoir.Length)
                {
					UnityEngine.Debug.LogError("Changing DimensionCount while initialized is forbidden.");
					myTarget.DimensionCount = myTarget.memoryReservoir.Length;
                }

				if(myTarget.SubdivisionPower != myTarget.Data[0].SubdivisionPower)
                {
					UnityEngine.Debug.LogError("Changing SubdivisionPower while initialized is forbidden.");
					myTarget.SubdivisionPower = myTarget.Data[0].SubdivisionPower;
				}

				for (int i = 0; i < myTarget.DimensionCount; i++)
				{
					EditorGUILayout.LabelField("Dimension:" + i + " Garbage Size: " + myTarget.memoryReservoir[i].GarbageSize);
				}
			}

			EditorGUILayout.LabelField("Is Locked: " + myTarget.Locked);
			EditorGUILayout.LabelField("Is updating voxel tree:" + myTarget.IsUpdatingVoxelTree);




			EditorGUILayout.Space();

			EditorGUILayout.Space();

			if (!myTarget.Locked)
			{
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(new GUIContent("Generate Block ID 0", "Fills the entire volume with initialValue 0.")))
				{
					myTarget.InitialValue = 0;
					myTarget.GenerateBlock();
				}

				if (GUILayout.Button(new GUIContent("Generate Block", "Fills the entire volume with initialValue.")))
				{
					myTarget.GenerateBlock();
				}

				if (GUILayout.Button(new GUIContent("Generate Block ID 255", "Fills the entire volume with initialValue 255.")))
				{
					myTarget.InitialValue = 255;
					myTarget.GenerateBlock();
				}
				EditorGUILayout.EndHorizontal();


				if (myTarget.worldgenerator)
				{
					if (GUILayout.Button(new GUIContent("Generate World", "Fills the entire volume with the calculation from all the attached World Algoriths.")))
					{
						myTarget.worldgenerator.InitializeGeneratorAndApplyWorld();
					}
				}

				if(myTarget.savesystem)
                {
					EditorGUILayout.BeginHorizontal();
					if (GUILayout.Button(new GUIContent("Save Data")))
					{
						myTarget.savesystem.Save();
					}
					if (GUILayout.Button(new GUIContent("Load Data")))
					{
						myTarget.savesystem.Load();
					}
					EditorGUILayout.EndHorizontal();
				}

				if (GUILayout.Button(new GUIContent("Reset", "Clears the volume.")))
				{
					myTarget.CleanUp();

				}

				if (GUILayout.Button(new GUIContent("Rebuild Hulls", "Rebuilds all the hulls.")))
				{
					myTarget.Rebuild();
				}
			}

			if (myTarget.IsInitialized)
			{
				EditorGUILayout.BeginHorizontal();
				for (int i = 0; i < myTarget.Data.Length; i++)
				{
					if (i % 3 == 0)
					{
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
					}
					if (GUILayout.Button("Kill D" + i))
					{
						myTarget.ResetDimension(i, 0);
					}
				}
				EditorGUILayout.EndHorizontal();
			}


			EditorGUILayout.Space();



#if !BURST_EXISTS
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=green>Burst Package Missing:</color>", title);

		
			EditorGUILayout.TextArea("Burst is the magical wand which boosts performance up to x100 for free.");

			if (GUILayout.Button("Install Burst Package"))
			{
				Client.Add("com.unity.burst@1.4.7");
			}
#endif

			if (myTarget.__export_foldout = EditorGUILayout.Foldout(myTarget.__export_foldout, "Export Options"))
			{
				BasicNativeVisualHull[] hulls = myTarget.GetComponentsInChildren<BasicNativeVisualHull>();

				myTarget.__export_meshName = EditorGUILayout.TextField("Export Name", myTarget.__export_meshName);
				myTarget.__export_savePath = EditorGUILayout.TextField("Path", myTarget.__export_savePath);
				myTarget.__export_center = EditorGUILayout.Vector3Field("Pivot Position", myTarget.__export_center);

				for (int i = 0; i < hulls.Length; i++)
				{
					EditorGUILayout.LabelField(hulls[i].name);
					EditorGUILayout.BeginHorizontal(
						new GUILayoutOption[]{
								GUILayout.MaxWidth(240)

					});

					if (GUILayout.Button("Export to .asset"))
					{

						BasicNativeVisualHull mf = hulls[i];
						if (mf)
						{
							string Path = myTarget.__export_savePath + myTarget.__export_meshName + i + ".asset";
							UnityEngine.Debug.Log("Saved Mesh to:" + Path);

							Mesh export = myTarget.ExtractHull(mf, myTarget.__export_center);
							AssetDatabase.CreateAsset(Instantiate<Mesh>(export), Path);
						}
						AssetDatabase.Refresh();

					}

					/*
                    if (GUILayout.Button("Export to .obj"))
                    {
                        MeshFilter mf = meshes[i];
                        MeshRenderer re = mf.GetComponent<MeshRenderer>();
                        if (mf && re)
                        {
                            mytarget.MeshToFile(mf.sharedMesh, re.sharedMaterials, meshName + i + ".obj", savePath);
                            string Path = savePath + meshName + i + ".obj";
                            Debug.Log("Saved Mesh to:" + Path);
                        }
                        AssetDatabase.Refresh();
                    }
                    */
					EditorGUILayout.EndHorizontal();

				}


			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}



		}

		public void HandleErrors()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;

			if (UnityEditor.SceneView.lastActiveSceneView != null)
			{
				if (!UnityEditor.SceneView.lastActiveSceneView.drawGizmos)
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField("<color=red>Gizmos is disabled:</color>", title);
					EditorGUILayout.TextArea("Nothing can be generated during edit mode if gizmos are disabled!");

					if (GUILayout.Button("Enable Gizmos"))
					{
						UnityEditor.SceneView.lastActiveSceneView.drawGizmos = true;
					}
				}
			}

			VoxelGenerator generator = (VoxelGenerator)target;
			string warnings = "";

			if (PrefabUtility.GetPrefabAssetType(generator) == PrefabAssetType.Regular)
			{		
				warnings += "- Generator is a prefab or part of a prefab! Unexpected behavior could occur.\n";
			}

#if UNITY_2021_2_OR_NEWER
			if (PrefabStageUtility.GetCurrentPrefabStage())
			{
				warnings += "- You are inside the prefab modification stage. Unexpected behavior could occur.";
			}
#else
			if (UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage())
			{
				warnings += "- You are inside the prefab modification stage. Unexpected behavior could occur.";
			}
			warnings += "- You are using a lower Unity version than the minimum. Downward compatibility is partially possible but not everywhere. Absolute minimum Version is 2021.2.xxx";
#endif

			if (warnings != "")
			{
				EditorGUILayout.LabelField("<color=orange>Warning:</color>", bold);
				EditorGUILayout.TextArea(warnings);
			}

#if BURST_150 && !BURST_154 && !UNITY_64
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=red>Burst and 32 Bit Architecture:</color>", title);
			EditorGUILayout.TextArea("Burst version between 1.5.0 and 1.5.3 does not work on 32bit architecture. " +
				"Using those versions on 32bit builds will crash the app. Either use a lower or higher burst version. " +
				"Also after installing Burst, restart Unity.");

			if (GUILayout.Button("Set Burst to 1.4.7"))
			{
				Client.Add("com.unity.burst@1.4.7");
			}

			if (GUILayout.Button("Set Burst to 1.5.4"))
			{
				Client.Add("com.unity.burst@1.5.4");
			}

#endif
		}
	}

	// ensure class initializer is called whenever scripts recompile
	[InitializeOnLoad]
	public static class VoxelGenerator_JobCleaner
	{
		// register an event handler when the class is initialized
		static VoxelGenerator_JobCleaner()
		{
			EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
			EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
			EditorSceneManager.sceneClosing += EditorSceneManager_sceneClosing;
		}

		private static void EditorSceneManager_sceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
		{
			VoxelGenerator[] objects = GameObject.FindObjectsOfType<VoxelGenerator>();
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].CleanUp();
			}
		}


		private static void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
		{
			VoxelGenerator[] objects = GameObject.FindObjectsOfType<VoxelGenerator>();

			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].instanceID = objects[i].gameObject.GetInstanceID();
			}
		}

		private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
		{
			CleanNativeArray(obj);
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			CleanNativeArray(PlayModeStateChange.ExitingPlayMode);



			
		}

		private static void CleanNativeArray(PlayModeStateChange state)
		{

			if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
			{
				VoxelGenerator[] objects = GameObject.FindObjectsOfType<VoxelGenerator>();

				for (int i = 0; i < objects.Length; i++)
				{
					objects[i].CleanUp();				
				}

				VoxelUtility.CleanUp();


				var objects2 = GameObject.FindObjectsOfType<ProceduralVoxelModifier>();

				for (int i = 0; i < objects2.Length; i++)
				{
					objects2[i].CleanUp();
				}


				VoxelGenerator.CleanUpStatics();
			}

			


		}



	}

#endif
}
