using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Utility;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
	public class Surface_Detail : BasicSurfaceModifier
	{
		public enum GenerationMode
		{
			Crystallic,
			IndividualObject,
			Both
		}

		public NativeArray<Vector3> Permutations;

		[Header("Detail Mesh Settings")]
		public GenerationMode Mode;

		public Transform DetailObject;
		public Mesh CrystalMesh;
		public Material CrystalMaterial;

		public Vector2 TrianglePos_min = new Vector3(0, 0, 0);
		public Vector2 TrianglePos_max = new Vector3(0, 0, 0);
		public float Angle_Min = 0;
		public float Angle_Max = 180;
		public Vector3 Angle_UpwardVector = new Vector3(0,0,1);

		public PlacementManifest CrystalPlacement;
		public float CrystalNormalInfluence = 1;
		public PlacementManifest ObjectPlacement;
		public float ObjectNormalInfluence = 1;

		[Header("Placement Settings")]
		[Range(0, 1)]
		public float CrystalProbability;
		[Range(0, 0.2f)][Tooltip("Object Probability is used for object placement")]
		public float ObjectProbability;
		[Range(0, 20)]
		public int Density;

		[Header("Requirement Settings")]
		public int RequirementDimension;
		public int LifeDimension;


		[Tooltip("Data structure for detail placement. Contains rules about how and when to place props.")]
		public DetailPlacement Placement;

		

		
		private Surface_Detail_Calculation[] m_MeshModJobs;
		private JobHandle[] m_JobHandles;

		private List<JobHandle> m_JobSequence = new List<JobHandle>();

		private bool isInitialized = false;	
		private int[] skips;

		private Stack<Transform>[] ObjectPool;

		[ReadOnly]
		public FNativeList<Vector3> mesh_verticeArray;
		[ReadOnly]
		public FNativeList<int> mesh_triangleArray;
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
		[ReadOnly]
		public FNativeList<Vector3> mesh_normalArray;
		[ReadOnly]
		public FNativeList<Vector4> mesh_tangentsArray;
		[ReadOnly]
		public FNativeList<Color> mesh_colorArray;

		protected override void Initialize()
		{
			if (CrystalMesh == null) return;

			isInitialized = true;
			m_MeshModJobs = new Surface_Detail_Calculation[Slots];
			m_JobHandles = new JobHandle[Slots];
		
			Permutations = ContainerStaticLibrary.GetPermutationTable("awdwadwa", 10000);
			
			VoxelMath.CreateNativeCrystalInformation(CrystalMesh, ref mesh_verticeArray, ref mesh_triangleArray, ref mesh_uvArray,
				ref mesh_uv3Array, ref mesh_uv4Array, ref mesh_uv5Array, ref mesh_uv6Array,
				ref mesh_normalArray, ref mesh_tangentsArray, ref mesh_colorArray);


			for (int i = 0; i < m_MeshModJobs.Length; i++)
			{
				m_MeshModJobs[i].Init();
				m_MeshModJobs[i].Permutations = Permutations;

				m_MeshModJobs[i].mesh_verticeArray = mesh_verticeArray;
				m_MeshModJobs[i].mesh_triangleArray = mesh_triangleArray;
				m_MeshModJobs[i].mesh_uvArray = mesh_uvArray;
				m_MeshModJobs[i].mesh_uv3Array = mesh_uv3Array;
				m_MeshModJobs[i].mesh_uv4Array = mesh_uv4Array;
				m_MeshModJobs[i].mesh_uv5Array = mesh_uv5Array;
				m_MeshModJobs[i].mesh_uv6Array = mesh_uv6Array;
				m_MeshModJobs[i].mesh_normalArray = mesh_normalArray;
				m_MeshModJobs[i].mesh_tangentsArray = mesh_tangentsArray;
				m_MeshModJobs[i].mesh_colorArray = mesh_colorArray;

				m_MeshModJobs[i].MODE = (int)Mode;
				m_MeshModJobs[i].slotIndex = i;
				m_MeshModJobs[i].Placement = Placement;

				if (RequirementDimension >= 0 && RequirementDimension < engine.Data.Length)
				{
					m_MeshModJobs[i].requirementData = engine.Data[RequirementDimension];
					m_MeshModJobs[i].requirementvalid = 1;
				}
				else m_MeshModJobs[i].requirementvalid = 0;

				if (LifeDimension >= 0 && LifeDimension < engine.Data.Length)
				{
					m_MeshModJobs[i].lifeData = engine.Data[LifeDimension];
					m_MeshModJobs[i].lifevalid = 1;
				}
				else m_MeshModJobs[i].lifevalid = 0;
			}

			int piececount = Slots;

			skips = new int[piececount];

			CreateVoxelPieces(piececount, CrystalMaterial);

			ObjectPool = new Stack<Transform>[piececount];
			for (int i = 0; i < piececount; i++)
			{
				ObjectPool[i] = new Stack<Transform>();
			}
		}



		public override void DefineSurface(VoxelPiece piece,FNativeList<Vector3> surface_verticeArray, FNativeList<int> surface_triangleArray, FNativeList<Vector3> surface_normalArray, int slot)
		{
			m_MeshModJobs[slot].surface_verticeArray = surface_verticeArray;
			m_MeshModJobs[slot].surface_normalArray = surface_normalArray;
			m_MeshModJobs[slot].surface_triangleArray = surface_triangleArray;

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

			while (SlotsDirty.Count > 0)
			{
				int m = SlotsDirty.Pop();
				int i = m % Cell_Subdivision;
				int j = (m - i) / Cell_Subdivision % Cell_Subdivision;
				int k = ((m - i) / Cell_Subdivision - j) / Cell_Subdivision;
				float cellSize = engine.RootSize / Cell_Subdivision;


				float startX = i * cellSize;
				float startY = j * cellSize;
				float startZ = k * cellSize;


				m_MeshModJobs[m].voxelSize = cellSize / (Width);
				m_MeshModJobs[m].halfSize = (cellSize / (Width)) / 2;
				m_MeshModJobs[m].cellSize = cellSize;
				m_MeshModJobs[m].positionoffset = new Vector3(startX, startY, startZ);
				m_MeshModJobs[m].TrianglePos_min = TrianglePos_min;
				m_MeshModJobs[m].TrianglePos_max = TrianglePos_max;
				m_MeshModJobs[m].Angle_Min = Angle_Min;
				m_MeshModJobs[m].Angle_Max = Angle_Max;
				m_MeshModJobs[m].Angle_UpwardVector = Angle_UpwardVector;
				m_MeshModJobs[m].CrystalManifest = CrystalPlacement;
				m_MeshModJobs[m].CrystalNormalInfluence = CrystalNormalInfluence;
				m_MeshModJobs[m].ObjectManifest = ObjectPlacement;
				m_MeshModJobs[m].ObjectNormalInfluence = ObjectNormalInfluence;
				m_MeshModJobs[m].CrystalProbability = CrystalProbability;
				m_MeshModJobs[m].ObjectProbability = ObjectProbability;
				m_MeshModJobs[m].Density = Density;

				m_MeshModJobs[m].Placement = Placement;

				m_JobHandles[m] = m_MeshModJobs[m].Schedule();
				SlotsProcessing.Push(m);
			}
		}

		public override void CompleteWorks()
		{
			if (!isInitialized) return;
			if (!engine) return;

			while (SlotsProcessing.Count > 0)
			{
				int m = SlotsProcessing.Pop();
				m_JobHandles[m].Complete();
				skips[m] = 0;
				int index = m;

				SetLock(m, LockAfterGeneration);

				VoxelPiece piece = VoxelMeshes[index];
				piece.Clear();
				Surface_Detail_Calculation usedcalculator = m_MeshModJobs[m];
				if (usedcalculator.verticeArray.Length != 0)
				{
					piece.SetVertices(usedcalculator.verticeArray);
					piece.SetTriangles(usedcalculator.triangleArray);
					piece.SetNormals(usedcalculator.normalArray);
					piece.SetTangents(usedcalculator.tangentsArray);
					piece.SetUVs(0, usedcalculator.uvArray);
					piece.SetUVs(2, usedcalculator.uv3Array);
					piece.SetUVs(3, usedcalculator.uv4Array);
					piece.SetUVs(4, usedcalculator.uv5Array);
					piece.SetUVs(5, usedcalculator.uv6Array);
					piece.SetColors(usedcalculator.colorArray);
				}

				piece.voxelMesh.RecalculateBounds();
				piece.EnableCollision(!NoCollision);

				if (DetailObject != null)
				{
					if (usedcalculator.objectArray.Length != 0)
					{
						Transform piececontainter = piece.transform;
						int childcount = piececontainter.childCount;

						for (int i = 0; i < childcount; i++)
						{
							Transform detailObject = piececontainter.GetChild(i);
							ObjectPool[index].Push(detailObject);
						}


						for (int i = 0; i < usedcalculator.objectArray.Length; i++)
						{
							Transform detailObject;
							if (ObjectPool[index].Count > 0)
							{
								detailObject = ObjectPool[index].Pop();
								detailObject.gameObject.SetActive(true);
							}
							else
							{
								detailObject = Instantiate(DetailObject, piececontainter);
							}

							Matrix4x4 matrix = usedcalculator.objectArray[i];


							detailObject.localPosition = matrix.GetColumn(3);
							detailObject.localRotation = matrix.rotation;
							detailObject.localScale = matrix.lossyScale;
						}

						while (ObjectPool[index].Count > 0)
						{
							Transform detailObject = ObjectPool[index].Pop();
							detailObject.gameObject.SetActive(false);
						}
					}
					else
					{
						Transform piececontainter = piece.transform;
						int childcount = piececontainter.childCount;

						for (int i = 0; i < childcount; i++)
						{
							piececontainter.GetChild(i).gameObject.SetActive(false);

						}
					}
				}


			}
		}





		public override bool IsSave()
		{
			if (CrystalMesh == null)
			{
				ErrorMessage = "No DetailMesh assigned!";
				return false;
			}
			if (CrystalMesh.vertexCount > 1000)
			{
				ErrorMessage = "Vertex Count of detail mesh is greater than 1000. Use individual objects for high poly details.";
				return false;
			}

			return true;
		}

		public override void CleanUp()
		{
			DestroyMeshes();
			

			if (SlotsDirty != null && SlotsDirty.Count > 0)
			{
				PrepareWorks();
				CompleteWorks();

			}
			if (m_MeshModJobs != null)
			{
				for (int i = 0; i < m_MeshModJobs.Length; i++)
				{
					m_JobHandles[i].Complete();
					m_MeshModJobs[i].CleanUp();					
				}
			}

			

			m_MeshModJobs = new Surface_Detail_Calculation[0];		
			if (mesh_verticeArray.IsCreated) mesh_verticeArray.Dispose();
			if (mesh_triangleArray.IsCreated) mesh_triangleArray.Dispose();
			if (mesh_uvArray.IsCreated) mesh_uvArray.Dispose();
			if (mesh_normalArray.IsCreated) mesh_normalArray.Dispose();
			if (mesh_tangentsArray.IsCreated) mesh_tangentsArray.Dispose();
			if (mesh_colorArray.IsCreated) mesh_colorArray.Dispose();
			if (mesh_uv3Array.IsCreated) mesh_uv3Array.Dispose();
			if (mesh_uv4Array.IsCreated) mesh_uv4Array.Dispose();
			if (mesh_uv5Array.IsCreated) mesh_uv5Array.Dispose();
			if (mesh_uv6Array.IsCreated) mesh_uv6Array.Dispose();

			isInitialized = false;

		}


		internal override float GetChecksum()
		{
			float sum = Cell_Subdivision + Width + NumCores;		
			sum += CrystalPlacement._GetChecksum();
			sum += ObjectPlacement._GetChecksum();		
			sum += (TrianglePos_min.sqrMagnitude);
			sum += (TrianglePos_max.sqrMagnitude);
			sum += (CrystalProbability*100 + ObjectProbability*100);
			sum += Density;
			sum += Placement.GetChecksum();
			sum += CrystalNormalInfluence + ObjectNormalInfluence;
			sum += Angle_Min + Angle_Max + Angle_UpwardVector.sqrMagnitude;
			return sum;
		}

		internal override void OnDuplicate()
		{
			VoxelPiece[] Pieces = GetComponentsInChildren<VoxelPiece>();
			for (int i = 0; i < Pieces.Length; i++)
			{
				if(Pieces[i].meshfilter.sharedMesh != null)
				Pieces[i].meshfilter.sharedMesh = Instantiate(Pieces[i].meshfilter.sharedMesh);
			}

		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(Surface_Detail))]
	[CanEditMultipleObjects]
	public class Surface_DetailEditor : Editor
	{



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



			Surface_Detail myTarget = (Surface_Detail)target;
			EditorGUILayout.Space();

			DrawDefaultInspector();

			if (!myTarget.IsSave())
			{
				EditorGUILayout.LabelField("<color=red>Errors:</color>", bold);
				EditorGUILayout.TextArea(myTarget.ErrorMessage);
			}


			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}



		}
	}
#endif

}
