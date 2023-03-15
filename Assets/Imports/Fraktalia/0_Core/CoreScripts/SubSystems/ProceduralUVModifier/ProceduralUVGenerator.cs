using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fraktalia.Core.FraktaliaAttributes;
using Unity.Collections;
using Fraktalia.Utility.NativeNoise;
using Unity.Collections.LowLevel.Unsafe;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Core.ProceduralUVCreator
{
	[ExecuteInEditMode]
	public unsafe class ProceduralUVGenerator : MonoBehaviour
	{	
		[Range(3,8)]
		public int TargetUVCoordinate;
		public int Seed;
		
		[NonSerialized]
		public Transform Container;
		[NonSerialized]
		public Mesh TargetMesh;

		[Header("Automatic Assignment")]
		public bool TargetSpecificSlot;
		public List<int> TargetSlots = new List<int>();	

		[Header("Manual Assignment")]
		public MeshFilter TargetFilter;
		

		public ProceduralUV[] Algorithms = new ProceduralUV[0];
		public PermutationTable_Native PermutationTable;

		private NativeArray<Vector3> PositionData;
		public NativeArray<Vector2> UVData;

		[NonSerialized]
		public bool IsInitialized;

		private void OnDrawGizmos()
		{
#if UNITY_EDITOR
			if (EditorApplication.isCompiling)
			{
				CleanUp();
			}
#endif
		}

		public void Initialize(Mesh target, Transform container)
		{
			Container = container;
			TargetMesh = target;

			Algorithms = GetComponentsInChildren<ProceduralUV>(true);
			
			if (!PermutationTable.IsCreated)
			{
				PermutationTable = new PermutationTable_Native(4096,255, Seed);
			}

			IsInitialized = true;
		}

		public void Apply()
		{
			if (!IsInitialized)
			{
				if (TargetMesh)
				{
					Initialize(TargetMesh, Container);
				}
				else if (TargetFilter)
				{
					if (TargetFilter.sharedMesh)
					{
						Initialize(TargetFilter.sharedMesh, TargetFilter.transform);
					}
				}

				if(!IsInitialized)
					return;
				

			}
			if (!TargetMesh) return;


		
			PositionData = new NativeArray<Vector3>(TargetMesh.vertices, Allocator.TempJob);	
			UVData = new NativeArray<Vector2>(PositionData.Length, Allocator.Persistent);

			for (int i = 0; i < Algorithms.Length; i++)
			{
				if (Algorithms[i].Inactive) continue;
				Algorithms[i].Generator = this;
				Algorithms[i].LaunchAlgorithm(ref PositionData, ref UVData);
			}

			ApplyAdditionalUVToMesh();
			PositionData.Dispose();
			UVData.Dispose();
		}

		//Prevents garbage which causes performance spike due to GC collection if you already have the positiondata
		public void ApplyFast(NativeArray<Vector3> PositionData)
		{
			if (!IsInitialized)
			{
				if (TargetMesh)
				{
					Initialize(TargetMesh, Container);
				}
				else if (TargetFilter)
				{
					if (TargetFilter.sharedMesh)
					{
						Initialize(TargetFilter.sharedMesh, TargetFilter.transform);
					}
				}

				if (!IsInitialized)
					return;


			}
			if (!TargetMesh) return;

			UVData = new NativeArray<Vector2>(PositionData.Length, Allocator.Persistent);

			for (int i = 0; i < Algorithms.Length; i++)
			{
				if (Algorithms[i].Inactive) continue;
				Algorithms[i].Generator = this;
				Algorithms[i].LaunchAlgorithm(ref PositionData, ref UVData);
			}

			ApplyAdditionalUVToMesh();		
			UVData.Dispose();
		}


		public void ApplyToMeshFilter(MeshFilter filter)
		{
			Initialize(filter.sharedMesh, filter.transform);
			Apply();
		}

		public void CleanUp()
		{
			IsInitialized = false;			
			if (PermutationTable.IsCreated) PermutationTable.CleanUp();
		}

		private void ApplyAdditionalUVToMesh()
		{
			if (!IsInitialized) return;

			if (TargetUVCoordinate >= 0 && TargetUVCoordinate < 8)
				TargetMesh.SetUVs(TargetUVCoordinate - 1, UVData);	
		}

		private void OnDestroy()
		{
			CleanUp();
		}

		private void Start()
		{
			if (Application.isPlaying)
			{
				if (TargetFilter)
				{
					if (TargetFilter.sharedMesh)
					{
						Apply();
					}
				}
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(ProceduralUVGenerator), true)]
	public class ProceduralUVGeneratorEditor : Editor
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


			ProceduralUVGenerator myTarget = (ProceduralUVGenerator)target;




			DrawDefaultInspector();
			EditorGUILayout.LabelField( "Is Initialized " + myTarget.IsInitialized);
			
				if (GUILayout.Button("Apply"))
				{
					myTarget.Apply();
				}
			
		}
	}
#endif
}
