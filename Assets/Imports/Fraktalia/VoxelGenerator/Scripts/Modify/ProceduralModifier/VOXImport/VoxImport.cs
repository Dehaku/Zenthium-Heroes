using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.VoxelGen;
using Fraktalia.VoxelGen.Modify.Import;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Fraktalia.Core.Math;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public class VoxImport : ProceduralVoxelModifier
	{
		public enum TextureChannel
		{
			Red,
			Green,
			Blue,
			Alpha
		}


		[HideInInspector]
		public float PositionMultiplier = 1;

		[BeginInfo("VOXIMPORTER")]
		[InfoTitle("VOX Importer", "This script allows you to import .VOX files. " +
			"Select the path to the .VOX file and hit the Import Vox File button. Then you can apply the result like a conventional procedural modifier. \n" +
			"Some imports also contain multiple chunks. You can select the important chunk using the Chunk Child Index parameter.\n\n" +
			"Note: SomeInformation like Animation, Color Data and Block Behavior data is currently ignored.", "VOXIMPORTER")]
		[InfoSection1("How to use:", "When adding me to a game object, define the Volume I should occupy using the <b>Volume Size</b>. Then click the generate button " +
			"or call GenerateBlock from any script. Click or call CleanUp to destroy the volume.\n\n" +
			"In order to see any results, hull generators must be attached and set up correctly because the generator itself is not responsible for the visualisation. " +
			"Also it is possible to apply sub systems like the save system for saving/loading and world generator for procedural world generation. " +
			"", "VOXIMPORTER")]
		[InfoText("VOX Importer", "VOXIMPORTER")]
		public float BoundaryMultiplier = 1;

		public VOXObject VOXFile;

		private VoxFileData CurrentData;
		public int ChunkChildIndex;

		public bool ChunkwiseGeneration;
		public bool IncrementChildIndex;
		public List<Vector3Int> ChunkOffsets;


		[Range(0, 2)]
		public int Smoothing;


		[Tooltip("Final Multiplier of the modifier. Negative values would inverse the result")]
		public float finalMultiplier = 1;


		private bool AfterEvaluation;

		public override void OnDrawGizmosSelected()
		{
			if (TargetGenerator)
			{
				targetgenerator_localtoworldmatrix = TargetGenerator.transform.localToWorldMatrix;
				targetgenerator_worldtolocalmatrix = TargetGenerator.transform.worldToLocalMatrix;


				Bounds bound = CalculateBounds();


				Gizmos.color = new Color32(0, 0, 0, 30);
				Gizmos.matrix = transform.localToWorldMatrix;
				//Gizmos.DrawCube(bound.center, bound.size);

				PositionMultiplier = 1 / BoundaryMultiplier;

				
				if (CurrentData != null && CurrentData.chunkChild.Count > 0)
				{
					Gizmos.color = Color.yellow;

					int chunkIndex = Mathf.Clamp(ChunkChildIndex, 0, CurrentData.chunkChild.Count-1);
					var data = CurrentData.chunkChild[chunkIndex];

					Vector3 chunkOffset = Vector3.zero;
					if (ChunkwiseGeneration)
					{
						int chunkoffsetIndex = Mathf.Clamp(ChunkChildIndex, 0, ChunkOffsets.Count - 1);
						if (chunkoffsetIndex >= 0 && chunkoffsetIndex < ChunkOffsets.Count)
						{
							chunkOffset = new Vector3(data.xyzi.voxels.maxX, data.xyzi.voxels.maxY, data.xyzi.voxels.maxZ) * TargetGenerator.GetVoxelSize(Depth) * BoundaryMultiplier;
							chunkOffset.Scale(ChunkOffsets[chunkoffsetIndex]);
						}

					}

					Vector3 slicebounds = TargetGenerator.GetVoxelSize(Depth) * new Vector3(data.xyzi.voxels.maxX, data.xyzi.voxels.maxY, data.xyzi.voxels.maxZ) / PositionMultiplier;

					Gizmos.DrawWireCube(slicebounds / 2 + chunkOffset, slicebounds);
				}
				


				Gizmos.matrix = Matrix4x4.identity;

				float RootSize = TargetGenerator.RootSize;
				Gizmos.color = Color.blue;
				Gizmos.matrix = TargetGenerator.transform.localToWorldMatrix;
				Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize));



			}
		}

		public override void EvaluateVoxelInfo(Vector3 start, Vector3 end)
		{
			if (VOXFile) CurrentData = VOXFile.CurrentData;
			if (CurrentData == null) return;

			Vector3 voxelPosition;

			int chunkindex = Mathf.Clamp(ChunkChildIndex, 0, CurrentData.chunkChild.Count - 1);
			var chunkdata = CurrentData.chunkChild[chunkindex];

			


			int index = 0;
			Vector3 Start = start;

			for (voxelPosition.x = start.x; voxelPosition.x <= end.x; voxelPosition.x += voxelsize)
			{
				for (voxelPosition.y = start.y; voxelPosition.y <= end.y; voxelPosition.y += voxelsize)
				{
					for (voxelPosition.z = start.z; voxelPosition.z <= end.z; voxelPosition.z += voxelsize)
					{
						Vector3Int position = MathUtilities.Convert1DTo3D(index, chunkdata.xyzi.voxels.maxX, chunkdata.xyzi.voxels.maxY, chunkdata.xyzi.voxels.maxZ);

						int z = position.z;
						int y = position.y;
						int x = position.x;

						//Vector3 localPosition = voxelPosition + Vector3.one * halfvoxelsize;
						Vector3 localPosition = Start + new Vector3(x * voxelsize, y * voxelsize, z * voxelsize);

						Vector3 worldPos = targetgenerator_localtoworldmatrix.MultiplyPoint3x4(localPosition);
						var output = new NativeVoxelModificationData();
						output.Depth = (byte)Depth;

						
						int ID = 0;
						if (x < chunkdata.xyzi.voxels.maxX && y < chunkdata.xyzi.voxels.maxY && z < chunkdata.xyzi.voxels.maxZ)
						{
							int voxelID = chunkdata.xyzi.voxels.GetVoxel(x, y, z);
							//if(voxelID != int.MaxValue)
							{
								ID = voxelID;
							}

							
						}
						else
						{
							ID = int.MinValue;
						}



						output.ID = (int)(ID * finalMultiplier);
						output.ID = Mathf.Clamp(output.ID, 0, 255);

						output.X = localPosition.x;
						output.Y = localPosition.y;
						output.Z = localPosition.z;

						ProceduralVoxelData.Add(output);
						index++;
						
					}
				}
			}

			AfterEvaluation = true;
		}

		public override void FinishApplication()
		{
			if (IncrementChildIndex)
			{
				ChunkChildIndex++;
			}
		}

		public override Bounds CalculateBounds()
		{
			if(VOXFile) CurrentData = VOXFile.CurrentData;
			if (CurrentData == null) return new Bounds();

			int Index = ChunkChildIndex;
			if (AfterEvaluation)
			{
				if (IncrementChildIndex)
				{
					if(Index > 0)
					{
						Index--;
					}
				}
				AfterEvaluation = false;
			}

			Index = Mathf.Clamp(Index, 0, CurrentData.chunkChild.Count-1);
			if (CurrentData.chunkChild.Count == 0) return new Bounds();
			var data = CurrentData.chunkChild[Index];

			Bounds bound = new Bounds();
			Vector3 offset = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);

			

			Vector3 chunkOffset = Vector3.zero;
			if (ChunkwiseGeneration)
			{
				int chunkoffsetIndex = Mathf.Clamp(ChunkChildIndex, 0, ChunkOffsets.Count - 1);
				if(chunkoffsetIndex >= 0 && chunkoffsetIndex < ChunkOffsets.Count)
				{
					chunkOffset = new Vector3(data.xyzi.voxels.maxX, data.xyzi.voxels.maxY, data.xyzi.voxels.maxZ) * BoundaryMultiplier * TargetGenerator.GetVoxelSize(Depth);
					chunkOffset.Scale(ChunkOffsets[chunkoffsetIndex]);
				}
				
			}

			bound.min = Vector3.zero + offset + chunkOffset;
			bound.max = TargetGenerator.GetVoxelSize(Depth) * new Vector3(data.xyzi.voxels.maxX, data.xyzi.voxels.maxY, data.xyzi.voxels.maxZ) * BoundaryMultiplier + offset + chunkOffset;

			return bound;
		}

		public override bool IsSave()
		{


			bool issave = true;
			return issave;
		}

		public void ImportVOXFromFile(string path)
		{
			CurrentData = VoxFileImport.Load(path);
		}
	}

#if UNITY_EDITOR

	[CanEditMultipleObjects]
	[CustomEditor(typeof(VoxImport), true)]
	public class VoxImportEditor : Editor
	{
		Vector3Int BoxLayout;

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



			VoxImport myTarget = (VoxImport)target;



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

					if (!Application.isPlaying)
						EditorUtility.SetDirty(myTarget.TargetGenerator);

				}

				if (GUILayout.Button("Apply Inversed Procedural Modifier"))
				{
					myTarget.ApplyProceduralModifier(true);

					if (!Application.isPlaying)
						EditorUtility.SetDirty(myTarget.TargetGenerator);

				}

				BoxLayout = EditorGUILayout.Vector3IntField("Box Layout", BoxLayout);
				if(GUILayout.Button("Create Box Layout"))
				{
					myTarget.ChunkOffsets = new List<Vector3Int>();
					for (int x = 0; x < BoxLayout.x; x++)
					{
						for (int y = 0; y < BoxLayout.y; y++)
						{
							for (int z = 0; z < BoxLayout.z; z++)
							{
								myTarget.ChunkOffsets.Add(new Vector3Int(x, y, z));
							}
						}
					}
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
