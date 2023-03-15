using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif


namespace Fraktalia.VoxelGen.Visualisation
{
	public class DataVisualisation : BasicNativeVisualHull
	{
		public const int MaxVertices = 300000;
		public Dictionary<Vector3, VoxelPiece> VoxelDictionary;
		public FNativeQueue<NativeVoxelNode> HullGenerationQueue;

		public HashSet<Vector3> Haschset;

		[Header("Appearance Settings:")]
		[Range(2, 5)]
		public int MeshDepth = 3;
		public bool FullCubes = false;
		[Range(0.01f, 1)]
		public float ScaleMultiplicator = 1;

		[Range(1, 32)]
		public int NumCores = 4;

		public Material VoxelMaterial;
		[Tooltip("For an extra material layer, usually you'd place a grass shader here")]
		public Material VoxelMaterialShell;

		DataVisualisation_Calculation[] m_MeshModJobs;
		JobHandle[] m_JobHandles;

		List<NativeVoxelNode> voxellist = new List<NativeVoxelNode>();

		DataVisualisation_NodesChanged[] nodechangedjobs;
		JobHandle[] nodechangedjobshandle;

		public override void PrepareWorks()
		{
			
			if (!HullGenerationQueue.IsCreated) return;

			for (int i = 0; i < m_MeshModJobs.Length; i++)
			{
				if (!m_JobHandles[i].IsCompleted)
				{				
					return;
				}
			}
			
			for (int i = 0; i < m_MeshModJobs.Length; i++)
			{
				if (HullGenerationQueue.Count > 0)
				{				
					NativeVoxelNode voxel = HullGenerationQueue.Dequeue();
					Vector3 voxelPosition = new Vector3(voxel.X, voxel.Y, voxel.Z);

					if (!Haschset.Contains(voxelPosition))
					{
						voxellist.Add(new NativeVoxelNode());
						continue;
					}

					voxel.Refresh(out voxel);

					if (!voxel.IsValid() || voxel.IsDestroyed())
					{
						if (VoxelDictionary.ContainsKey(voxelPosition))
						{
							VoxelDictionary[voxelPosition].meshfilter.sharedMesh.Clear();
						}
						voxellist.Add(new NativeVoxelNode());
					}
					else
					{
						voxellist.Add(voxel);
						if (!VoxelDictionary.ContainsKey(voxelPosition))
						{
							VoxelDictionary[new Vector3(voxel.X, voxel.Y, voxel.Z)] = CreateVoxelPiece("VOXELPIECE", VoxelMaterial);
						}
						m_MeshModJobs[i].Voxel = voxel;
						m_MeshModJobs[i].MeshDepth = MeshDepth;
						m_MeshModJobs[i].Data = engine.Data[0];

						m_JobHandles[i] = m_MeshModJobs[i].Schedule();

					}

					Haschset.Remove(voxelPosition);

				}
			}
		}

		public override void CompleteWorks()
		{
			if (!HullGenerationQueue.IsCreated) return;

			for (int i = 0; i < m_JobHandles.Length; i++)
			{

				m_JobHandles[i].Complete();
				if (i < voxellist.Count)
				{
					if (voxellist[i].IsValid())
					{
						
						NativeVoxelNode voxel = voxellist[i];
						Vector3 voxelPosition = new Vector3(voxel.X, voxel.Y, voxel.Z);
					
						if(!VoxelDictionary.ContainsKey(voxelPosition))
						{
							VoxelDictionary[new Vector3(voxel.X, voxel.Y, voxel.Z)] = CreateVoxelPiece("VOXELPIECE", VoxelMaterial);
						}

						VoxelPiece voxelpiece = VoxelDictionary[voxelPosition];

						Mesh voxelmesh = voxelpiece.meshfilter.sharedMesh;
						voxelmesh.Clear();
		

						voxelmesh.vertices = m_MeshModJobs[i].m_Vertices.ToArray();
						voxelmesh.uv = m_MeshModJobs[i].m_uvs.ToArray();
						voxelmesh.triangles = m_MeshModJobs[i].m_triangles.ToArray();
						voxelmesh.RecalculateNormals();
						voxelmesh.RecalculateTangents();
						
						voxelpiece.EnableCollision(!NoCollision);
					}
				}
			}

			voxellist.Clear();
		}

		public override void NodeChanged(NativeVoxelNode node)
		{
			NativeVoxelNode current;
			node.Refresh(out current);
			int mergeID = node._voxelID;
			while (current.IsValid())
			{
				if (current.Depth % MeshDepth == 0)
				{
					NativeVoxelNode leftneighbour = current._LeftNeighbor(ref engine.Data[0], 0);
					if (leftneighbour.IsValid())
						NodeDestroyed(leftneighbour);
					NativeVoxelNode rightneighbour = current._RightNeighbor(ref engine.Data[0], 0);
					if (rightneighbour.IsValid())
						NodeDestroyed(rightneighbour);
					NativeVoxelNode downneighbour = current._DownNeighbor(ref engine.Data[0], 0);
					if (downneighbour.IsValid())
						NodeDestroyed(downneighbour);
					NativeVoxelNode upneighbour = current._UpNeighbor(ref engine.Data[0], 0);
					if (upneighbour.IsValid())
						NodeDestroyed(upneighbour);
					NativeVoxelNode frontneighbour = current._FrontNeighbor(ref engine.Data[0], 0);
					if (frontneighbour.IsValid())
						NodeDestroyed(frontneighbour);
					NativeVoxelNode backneighbour = current._BackNeighbor(ref engine.Data[0], 0);
					if (backneighbour.IsValid())
						NodeDestroyed(backneighbour);

					NodeDestroyed(current);
				}
				current.GetParent(out current);
			}
		}

		public override void NodesChanged(ref FNativeList<NativeVoxelNode> voxels)
		{


			for (int i = 0; i < nodechangedjobs.Length; i++)
			{
				nodechangedjobs[i].voxels = voxels;
				nodechangedjobs[i].Data = engine.Data[0];
				nodechangedjobs[i].MeshDepth = MeshDepth;
				nodechangedjobs[i].NumCores = NumCores;
				nodechangedjobs[i].coreID = i;

				nodechangedjobshandle[i] = nodechangedjobs[i].Schedule();

			}

			for (int i = 0; i < nodechangedjobs.Length; i++)
			{
				nodechangedjobshandle[i].Complete();
			}

			for (int i = 0; i < nodechangedjobs.Length; i++)
			{
				FNativeList<NativeVoxelNode> output = nodechangedjobs[i].output;
				int count = output.Length;
				for (int k = 0; k < count; k++)
				{
					NativeVoxelNode voxel = output[k];
					Vector3 voxelPosition = new Vector3(voxel.X, voxel.Y, voxel.Z);
					if (!Haschset.Contains(voxelPosition))
					{
						Haschset.Add(voxelPosition);
						HullGenerationQueue.Enqueue(voxel);
					}
				}


			}




		}

		public override void NodeDestroyed(NativeVoxelNode node)
		{
			if (node.Depth % MeshDepth == 0)
			{
				Vector3 nodePosition = new Vector3(node.X, node.Y, node.Z);
				if (!Haschset.Contains(nodePosition))
				{
					Haschset.Add(nodePosition);
					HullGenerationQueue.Enqueue(node);
				}

			}
		}

		protected override void Initialize()
		{
			HullGenerationQueue = new FNativeQueue<NativeVoxelNode>(Allocator.Persistent);
			HullGenerationQueue.Clear();
			Haschset = new HashSet<Vector3>();

			VoxelDictionary = new Dictionary<Vector3, VoxelPiece>();
			
			voxellist.Clear();
			


			m_MeshModJobs = new DataVisualisation_Calculation[NumCores];
			m_JobHandles = new JobHandle[NumCores];
			nodechangedjobs = new DataVisualisation_NodesChanged[NumCores];
			nodechangedjobshandle = new JobHandle[NumCores];
		

			for (int i = 0; i < m_MeshModJobs.Length; i++)
			{
				m_MeshModJobs[i].Init(MeshDepth, engine.SubdivisionPower);
				m_MeshModJobs[i].CoreID = i;
				m_MeshModJobs[i].FullCubes = FullCubes;
				m_MeshModJobs[i].ScaleMultiplicator = ScaleMultiplicator;

				nodechangedjobs[i].output = new FNativeList<NativeVoxelNode>(Allocator.Persistent);
			}
		}

		protected override void cleanup()
		{
			if (HullGenerationQueue.IsCreated) HullGenerationQueue.Dispose();
			Haschset?.Clear();

			voxellist.Clear();

			if (VoxelDictionary != null)
			{
				foreach (var item in VoxelDictionary)
				{
					if (item.Value)
					{
						DestroyImmediate(item.Value.gameObject);
					}
				}
				VoxelDictionary.Clear();
			}

			if (m_MeshModJobs != null)
			{
				for (int i = 0; i < m_MeshModJobs.Length; i++)
				{
					m_JobHandles[i].Complete();
					m_MeshModJobs[i].CleanUp();
					nodechangedjobs[i].CleanUp();
				}
			}


		}
		
		public override bool IsWorking()
		{
			if (!HullGenerationQueue.IsCreated) return false;

			return HullGenerationQueue.Count > 0 || voxellist.Count > 0;
		}

		protected override float GetChecksum()
		{
			float value = MeshDepth * 1000 + ScaleMultiplicator * 100 + NumCores * 10;


			return (int)value;
		}

		public override void Rebuild()
		{
			cleanup();
			Initialize();

			for (int i = 0; i < engine.DimensionCount; i++)
			{
				RawVoxelData data = VoxelSaveSystem.ConvertRaw(engine);
				VoxelSaveSystem.ApplyRawVoxelData(engine, data);

			}

		}	
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(DataVisualisation))]
	public class NativeCubic_MultithreadedEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			DataVisualisation myTarget = (DataVisualisation)target;

			
			DrawDefaultInspector();

			if (myTarget.engine)
			{
				int power = myTarget.engine.SubdivisionPower;
				int MaxBlocks = 1 + (int)Mathf.Pow(power * power * power, myTarget.MeshDepth);

				EditorStyles.textField.wordWrap = true;

				EditorGUILayout.LabelField("Maximum Vertex Count = " + MaxBlocks);
				
			}

		}
	}

	

#endif
}
