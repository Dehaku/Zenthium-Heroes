using Fraktalia.VoxelGen;
using Fraktalia.VoxelGen.Modify;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen.Modify.Procedural
{


	[ExecuteInEditMode]
	public class ProceduralVoxelModifier : MonoBehaviour
	{
		
		[Header("General Settings:")]
		[NonSerialized]
		public FNativeList<NativeVoxelModificationData> ProceduralVoxelData;
		private FNativeList<NativeVoxelModificationData> PostProcessedVoxelData;


		public VoxelGenerator TargetGenerator;
		public int Depth;
		public int TargetDimension;

		public bool Additive = false;
		public bool ClearBlock = false;

		[HideInInspector]
		public string ErrorMessage = "This setup would modify more than 5 Million Voxels. You do not want to freeze Unity";
		[HideInInspector]
		[Tooltip("Bongo Bongo I am ignoring the safety feature if set to true. I do not whine if something goes wronk.")]
		public bool IgnoreErrors = false;

		protected float voxelsize;
		protected float halfvoxelsize;
		protected Vector3Int boundaryvoxelsize;
		protected int boundaryvoxelcount;

		protected float blocksize;
		protected Matrix4x4 targetgenerator_localtoworldmatrix;
		protected Matrix4x4 targetgenerator_worldtolocalmatrix;

		private List<VoxelGenerator> modifiedgenerators = new List<VoxelGenerator>();

		public virtual void OnDrawGizmos()
		{
#if UNITY_EDITOR
			if (EditorApplication.isCompiling)
			{
				CleanUp();
			}
#endif
		}

		public virtual void OnDrawGizmosSelected()
		{



			if (TargetGenerator)
			{
				targetgenerator_localtoworldmatrix = TargetGenerator.transform.localToWorldMatrix;
				targetgenerator_worldtolocalmatrix = TargetGenerator.transform.worldToLocalMatrix;


				Bounds bound = CalculateBounds();

				Gizmos.color = new Color32(0, 0, 0, 30);
				Gizmos.DrawCube(TargetGenerator.transform.localToWorldMatrix.MultiplyPoint3x4(bound.center), bound.size);


				float RootSize = TargetGenerator.RootSize;
				Gizmos.color = Color.blue;
				Gizmos.matrix = TargetGenerator.transform.localToWorldMatrix;
				Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize));
			}
		}


		public void CreateProceduralData()
		{
			if (!ProceduralVoxelData.IsCreated)
			{
				ProceduralVoxelData = new FNativeList<NativeVoxelModificationData>(Allocator.Persistent);
			}
			else
			{
				ProceduralVoxelData.Clear();
			}

			if (!PostProcessedVoxelData.IsCreated)
			{
				PostProcessedVoxelData = new FNativeList<NativeVoxelModificationData>(Allocator.Persistent);
			}
			else
			{
				PostProcessedVoxelData.Clear();
			}


			voxelsize = TargetGenerator.GetVoxelSize(Depth);
			halfvoxelsize = voxelsize / 2;

			blocksize = TargetGenerator.RootSize;
			targetgenerator_localtoworldmatrix = TargetGenerator.transform.localToWorldMatrix;
			targetgenerator_worldtolocalmatrix = TargetGenerator.transform.worldToLocalMatrix;

			Bounds bound = CalculateBounds();
			Vector3 start = bound.min;
			Vector3 end = bound.max;

			Vector3 difference = (end - start) / voxelsize;
			boundaryvoxelsize = new Vector3Int((int)difference.x, (int)difference.y, (int)difference.z);
			boundaryvoxelcount = boundaryvoxelsize.x * boundaryvoxelsize.y * boundaryvoxelsize.z;

			EvaluateVoxelInfo(start, end);
		}

		public virtual Bounds CalculateBounds()
		{
			Bounds output = new Bounds();
			output.min = new Vector3(0, 0, 0);
			output.max = Vector3.one * TargetGenerator.RootSize;
			return output;
		}

		public virtual void EvaluateVoxelInfo(Vector3 start, Vector3 end)
		{
			var output = new NativeVoxelModificationData();
			output.Depth = 0;
			output.ID = TargetGenerator.InitialValue;
			output.X = 0;
			output.Y = 0;
			output.Z = 0;
			ProceduralVoxelData.Add(output);
		}

		public virtual void FinishApplication()
		{

		}


		/// <summary>
		/// Applies procedural generated data to the Voxel Generator. Note: Possible save system will not be set dirty by procedural content generation.
		/// </summary>
		public void ApplyProceduralModifier(bool inversed = false)
		{
			if (!TargetGenerator) return;
			if (!IsSave() && !IgnoreErrors) return;
			if (!TargetGenerator.IsInitialized) TargetGenerator.GenerateBlock();

			modifiedgenerators.Clear();

			CreateProceduralData();
			Bounds boundary = CalculateBounds();
			modifiedgenerators.Add(TargetGenerator);
			
			if (inversed)
			{
				VoxelUtility.InvertModification(ProceduralVoxelData, ProceduralVoxelData);
			}


			for (int i = 0; i < modifiedgenerators.Count; i++)
			{
				VoxelGenerator selectedGenerator = modifiedgenerators[i];

				if (ClearBlock)
				{
					selectedGenerator._SetVoxel(new Vector3(0, 0, 0), 0, 0, TargetDimension);
					selectedGenerator.SetAllRegionsDirty();
				}
				if (Additive)
				{
					
					selectedGenerator._SetVoxelsAdditive(ProceduralVoxelData, TargetDimension);
					
				}
				else
				{				
					selectedGenerator._SetVoxels(ProceduralVoxelData, TargetDimension);					
				}

				ProceduralVoxelData.Clear();
				ProceduralVoxelData.Capacity = 0;
				PostProcessedVoxelData.Clear();
				PostProcessedVoxelData.Capacity = 0;
			
				selectedGenerator.SetRegionsDirty(boundary.center, boundary.extents, boundary.extents, TargetDimension);
				
				if (selectedGenerator.savesystem) selectedGenerator.savesystem.IsDirty = true;
			}
		}

		public virtual bool IsSave()
		{
			return true;
		}

		public virtual void CleanUp()
		{
			if (ProceduralVoxelData.IsCreated) ProceduralVoxelData.Dispose();
			if (PostProcessedVoxelData.IsCreated) PostProcessedVoxelData.Dispose();
		}

		private void OnDestroy()
		{
			if (ProceduralVoxelData.IsCreated) ProceduralVoxelData.Dispose();
			if (PostProcessedVoxelData.IsCreated) PostProcessedVoxelData.Dispose();
		}
	}

#if UNITY_EDITOR

	[CanEditMultipleObjects]
	[CustomEditor(typeof(ProceduralVoxelModifier), true)]
	public class ProceduralVoxelModifierEditor : Editor
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



			ProceduralVoxelModifier myTarget = (ProceduralVoxelModifier)target;



			DrawDefaultInspector();

			if (myTarget.TargetGenerator)
			{

				float nodesize = VoxelUtility.CalculateVoxelSize(myTarget.TargetGenerator, myTarget.Depth);
				float blocksize = myTarget.TargetGenerator.RootSize;
				Vector3 voxelPosition = new Vector3(0, 0, 0);
				Bounds bound = myTarget.CalculateBounds();


				int voxellength_x = (int)(bound.size.x / nodesize);
				int voxellength_y = (int)(bound.size.y / nodesize);
				int voxellength_z = (int)(bound.size.z / nodesize);

				int voxels = voxellength_x * voxellength_y * voxellength_z;
				EditorGUILayout.LabelField("Number Voxels: " + voxels);
				if (!myTarget.IsSave())
				{
					EditorGUILayout.LabelField("<color=red>Errors:</color>", bold);
					EditorGUILayout.TextArea(myTarget.ErrorMessage);


					EditorGUILayout.PropertyField(serializedObject.FindProperty("IgnoreErrors"), new GUIContent("I know what I am doing"));
					if (GUI.changed)
					{
						serializedObject.ApplyModifiedProperties();
					}

					if (!myTarget.IgnoreErrors)
					{
						return;
					}
				}


				if (PrefabUtility.GetPrefabAssetType(myTarget.TargetGenerator) == PrefabAssetType.Regular)
				{
					EditorGUILayout.LabelField("<color=blue>Super DUPER Error:</color>", bold);
					EditorGUILayout.TextArea("You have assigned a voxel generator which is not in the scene! " +
						"Please assign a generator from the scene and not from the database.");
				}

				if (GUI.changed)
				{
					serializedObject.ApplyModifiedProperties();
				}



				if (GUILayout.Button("Apply Procedural Modifier"))
				{
					myTarget.ApplyProceduralModifier();
					myTarget.FinishApplication();

					if (!Application.isPlaying)
						EditorUtility.SetDirty(myTarget.TargetGenerator);

				}

				if (GUILayout.Button("Apply Inversed Procedural Modifier"))
				{
					myTarget.ApplyProceduralModifier(true);
					myTarget.FinishApplication();
					if (!Application.isPlaying)
						EditorUtility.SetDirty(myTarget.TargetGenerator);

				}
			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}



		}
	}
#endif

}
