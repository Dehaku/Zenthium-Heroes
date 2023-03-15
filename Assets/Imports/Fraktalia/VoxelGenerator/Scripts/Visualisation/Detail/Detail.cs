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
	public class Detail : BasicNativeVisualHull
	{


		[Header("Resolution Settings:")]
		[Range(2, 32)]
		[Tooltip("Resolution of individual Cells")]
		public int width = 8;

		[Range(2, 10)]
		[Tooltip("Amount of GameObjects used for generation")]
		public int Cell_Subdivision = 8;

		[Range(1, 32)]
		[Tooltip("Maximum CPU Cores which can be dedicated. Affects generation speed.")]
		public int NumCores = 1;


		[Header("Appearance Settings:")]
		public VoxelDetailObject[] DetailPrefabs;

		public Vector3 Offset_min = new Vector3(0, 0, 0);
		public Vector3 Offset_max = new Vector3(0, 0, 0);
		public Vector3 Scale_min = new Vector3(1, 1, 1);
		public Vector3 Scale_max = new Vector3(1, 1, 1);
		public float ScaleFactor_min = 1;
		public float ScaleFactor_max = 1;
		public Vector3 Rotation_min;
		public Vector3 Rotation_max;

		[Range(0, 0.5f)]
		public float Probability;


		public int Seed = 0;

		[Tooltip("Initial value for evaluating detail placement on every voxel. Is modified by requirements." +
			"Details are placed if the value is greater than 0")]
		public int InitialHealth;

		public DetailRequirement[] Requirements;

		private NativeArray<DetailRequirement> NativeDetailRequirements;
		private NativeArray<Vector3> Permutations;

		private Queue<int> WorkerQueue;
		private bool[] Haschset;
		private NativeDetail_Calculation[] m_MeshModJobs;
		private JobHandle[] m_JobHandles;
		private bool[] m_JobFree;
		private NativeDetail_NodesChanged nodechangedjob;
		private int[] workIndex;
		private bool isInitialized = false;
		private int[] passedFrames;

		private Dictionary<Vector3, VoxelDetailObject> DetailList;


		protected override void Initialize()
		{
			isInitialized = true;
			m_MeshModJobs = new NativeDetail_Calculation[NumCores];
			m_JobHandles = new JobHandle[NumCores];
			m_JobFree = new bool[NumCores];

			DetailList = new Dictionary<Vector3, VoxelDetailObject>();

			for (int i = 0; i < m_JobFree.Length; i++)
			{
				m_JobFree[i] = true;
			}

			workIndex = new int[NumCores];

			var oldseed = UnityEngine.Random.state;
			UnityEngine.Random.InitState(Seed);
			Permutations = new NativeArray<Vector3>(10000, Allocator.Persistent);
			for (int i = 0; i < 10000; i++)
			{
				Permutations[i] = UnityEngine.Random.insideUnitSphere;
			}

			if (Requirements == null) Requirements = new DetailRequirement[0];
			NativeDetailRequirements = new NativeArray<DetailRequirement>(Requirements, Allocator.Persistent);

			for (int i = 0; i < m_MeshModJobs.Length; i++)
			{
				m_MeshModJobs[i].Init(width);
				m_MeshModJobs[i].Permutations = Permutations;

				m_MeshModJobs[i].DetailNeighbourRequirement = NativeDetailRequirements;

				m_MeshModJobs[i].data = engine.Data[0];

				UnityEngine.Random.InitState(Seed + 10);

			}

			UnityEngine.Random.state = oldseed;

			Haschset = new bool[Cell_Subdivision * Cell_Subdivision * Cell_Subdivision];
			for (int index = 0; index < Cell_Subdivision * Cell_Subdivision * Cell_Subdivision; index++)
			{
				WorkerQueue.Enqueue(index);
				Haschset[index] = true;
			}

			nodechangedjob.Init(this);


		}

		public override void Rebuild()
		{
			for (int index = 0; index < Cell_Subdivision * Cell_Subdivision * Cell_Subdivision; index++)
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



				int i = index % Cell_Subdivision;
				int j = (index - i) / Cell_Subdivision % Cell_Subdivision;
				int k = ((index - i) / Cell_Subdivision - j) / Cell_Subdivision;
				float cellSize = engine.RootSize / Cell_Subdivision;
				float startX = i * cellSize;
				float startY = j * cellSize;
				float startZ = k * cellSize;


				m_MeshModJobs[m].cellSize = cellSize;
				m_MeshModJobs[m].voxelSize = cellSize / (width);
				m_MeshModJobs[m].positionoffset = new Vector3(startX, startY, startZ);


				m_MeshModJobs[m].RandomIndexOffset = i * 10 + j * 100 + k * 1000;
				m_MeshModJobs[m].Probability = Probability * 2 - 1;
				m_MeshModJobs[m].InitialHealth = InitialHealth;
				m_JobHandles[m] = m_MeshModJobs[m].Schedule();
				m_JobFree[m] = false;
			}
		}

		public override void CompleteWorks()
		{
			if (!isInitialized) return;
			if (!engine) return;
			if (DetailPrefabs == null) return;
			if (DetailPrefabs.Length == 0) return;


			var oldseed = UnityEngine.Random.state;

			UnityEngine.Random.InitState(Seed + DetailList.Count * 10000);
			for (int m = 0; m < m_MeshModJobs.Length; m++)
			{
				if (workIndex[m] == -1) continue;
				if (!m_JobHandles[m].IsCompleted)
				{
					continue;
				}


				m_JobHandles[m].Complete();


				int index = workIndex[m];
				Vector3[] results = m_MeshModJobs[m].detailresultArray.ToArray();

				for (int i = 0; i < results.Length; i++)
				{
					if (!DetailList.ContainsKey(results[i]))
					{

						int o = UnityEngine.Random.Range(0, DetailPrefabs.Length);
						if (DetailPrefabs[o] == null) continue;

						VoxelDetailObject newobject = Instantiate(DetailPrefabs[o], transform);
						Vector3 offset = new Vector3(0, 0, 0);
						offset.x = UnityEngine.Random.Range(Offset_min.x, Offset_max.x);
						offset.y = UnityEngine.Random.Range(Offset_min.y, Offset_max.y);
						offset.z = UnityEngine.Random.Range(Offset_min.z, Offset_max.z);
						newobject.transform.localPosition = results[i] + offset;

						Vector3 rot = new Vector3(0, 0, 0);
						rot.x = UnityEngine.Random.Range(Rotation_min.x, Rotation_max.x);
						rot.y = UnityEngine.Random.Range(Rotation_min.y, Rotation_max.y);
						rot.z = UnityEngine.Random.Range(Rotation_min.z, Rotation_max.z);
						newobject.transform.localRotation = Quaternion.Euler(rot);

						Vector3 scale = new Vector3(0, 0, 0);
						scale.x = UnityEngine.Random.Range(Scale_min.x, Scale_max.x);
						scale.y = UnityEngine.Random.Range(Scale_min.y, Scale_max.y);
						scale.z = UnityEngine.Random.Range(Scale_min.z, Scale_max.z);
						newobject.transform.localScale = scale * UnityEngine.Random.Range(ScaleFactor_min, ScaleFactor_max);
						DetailList.Add(results[i], newobject);


					}
				}




				m_JobFree[m] = true;
				Haschset[index] = false;
				nodechangedjob.hashindexmap[index] = 0;
				
			}
			UnityEngine.Random.state = oldseed;

		}

		

		public override void NodeChanged(NativeVoxelNode node)
		{


			float cellSize = engine.RootSize / Cell_Subdivision;
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

			int lenght = Cell_Subdivision * Cell_Subdivision * Cell_Subdivision;

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

		public override void NodesChanged(ref FNativeList<NativeVoxelNode> voxels)
		{
			nodechangedjob.voxels = voxels;

			JobHandle job = nodechangedjob.Schedule(voxels.Length, voxels.Length / NumCores);

			job.Complete();

			int[] works = nodechangedjob.output.ToArray();
			for (int i = 0; i < works.Length; i++)
			{
				if (works[i] == 1)
				{
					if (!Haschset[i])
					{
						Haschset[i] = true;
						WorkerQueue.Enqueue(i);
					}
					works[i] = 0;
				}
			}
			nodechangedjob.output.CopyFrom(works);

		}

		protected override void cleanup()
		{
			isInitialized = false;




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
			m_MeshModJobs = new NativeDetail_Calculation[0];

			if (Permutations.IsCreated) Permutations.Dispose();
			if (NativeDetailRequirements.IsCreated) NativeDetailRequirements.Dispose();

			DetailList = new Dictionary<Vector3, VoxelDetailObject>();
			VoxelDetailObject[] details = GetComponentsInChildren<VoxelDetailObject>();
			for (int i = 0; i < details.Length; i++)
			{
				DestroyImmediate(details[i].gameObject);
			}

			nodechangedjob.CleanUp();
		}

		public override bool IsWorking()
		{
			if (m_JobFree == null) return false;
			for (int i = 0; i < m_JobFree.Length; i++)
			{
				if (!m_JobFree[i]) return true;
			}

			return false;
		}

		protected override float GetChecksum()
		{
			float sum = Cell_Subdivision * 1000 + width * 100 + NumCores * 10;
			sum += Seed;
			sum += Offset_min.sqrMagnitude * 10000;
			sum += Offset_max.sqrMagnitude * 10000;
			sum += Scale_min.sqrMagnitude * 10000;
			sum += Scale_max.sqrMagnitude * 10000;
			sum += Rotation_min.sqrMagnitude * 10000;
			sum += Rotation_max.sqrMagnitude * 10000;
			sum += (ScaleFactor_min * 10000);
			sum += (ScaleFactor_max * 10000);
			sum += (Probability * 10000);
			sum += InitialHealth;

			for (int i = 0; i < Requirements.Length; i++)
			{
				DetailRequirement requirement = Requirements[i];

				sum += requirement.x * 10;
				sum += requirement.y * 10;
				sum += requirement.z * 10;
				sum += requirement.TargetID * 10;
				sum += requirement.IncorrectModifier * 10;
				sum += requirement.CorrectModifier * 10;
				sum += (int)requirement.CompMode * 10;

			}

			return sum;
		}

	}

}
