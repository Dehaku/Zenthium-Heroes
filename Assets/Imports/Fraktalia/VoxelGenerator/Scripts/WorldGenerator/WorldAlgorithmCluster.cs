using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fraktalia.Core.FraktaliaAttributes;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen.World
{
	public class WorldAlgorithmCluster : MonoBehaviour
	{
		[BeginInfo("WORLDALGORITHMCLUSTER")]
		[InfoTitle("World Algorithm Cluster", "This component is the root object of world algorithms and is used by the World Generator to calculate terrain." +
			"The World Generator is assigned automatically and will auto update the terrain whenever values change. Target Dimension and Depth is defined here " +
			"in order to define the target resolution and voxel dimension. World Algorithms attached to this game object are automatically assigned and used for the " +
			"calculation as long as they do not cause error.\n\n" +
			"It is important that at least one parent object (whole chain up) has at least one World Generator component. Also it is possible to use more than " +
			"one cluster (one for each used dimension). However if the target dimension of the second cluster is the same as the first one, the second one will " +
			"overwrite the result of the first one." +
			"", "WORLDALGORITHMCLUSTER")]
		[InfoVideo("https://www.youtube.com/watch?v=3KrPFj9hUcA&lc=UgzjAqdGrVsM77feBBN4AaABAg", false, "WORLDALGORITHMCLUSTER")]
		[InfoText("World Algorithm Cluster:", "WORLDALGORITHMCLUSTER")]
		public int TargetDimension;
		public int TargetDepth;

		public bool Additive;
		public List<WorldAlgorithm> Algorithms = new List<WorldAlgorithm>();

		[NonSerialized]
		public WorldGenerator generator;

		[NonSerialized]
		public float scale;

		[TitleText("At your own risk:", TitleTextType.H3)]
		public bool IgnoreWarnings;

		public void Initialize()
		{
			Algorithms.Clear();

			GetComponentsInChildren<WorldAlgorithm>(true, Algorithms);
		
			for (int i = 0; i < Algorithms.Count; i++)
			{
				Algorithms[i].worldGenerator = generator;
				Algorithms[i].Depth = TargetDepth;
				Algorithms[i].Initialize(generator.referenceGenerator);
				Algorithms[i].DefineApplyMode();
			}
		}

		public void CleanUp()
		{
			if (Algorithms != null)
			{
				for (int i = 0; i < Algorithms.Count; i++)
				{
					if (Algorithms[i])
						Algorithms[i].CleanUp();
				}
			}
		}


		public bool IsSafe(VoxelGenerator targetGenerator)
		{
			if (TargetDimension >= targetGenerator.DimensionCount)
			{
				return false;
			}

			if (IgnoreWarnings) return true;


			int voxels = targetGenerator.GetBlockCount(TargetDepth);
			return voxels < 10000000 && voxels > 0;
		}

		public void Finish(VoxelGenerator targetGenerator)
		{
			var data = generator.modificationReservoir.GetDataArray(TargetDepth);

			if (Additive)
			{
				targetGenerator._SetVoxelsAdditive_Inner(data, TargetDimension);
			}
			else
			{
				targetGenerator._SetVoxels_Inner(data, TargetDimension);
			}
		}

		public string ErrorCheck()
		{
			string Errors = "";
			if (generator)
			{
				VoxelGenerator reference = generator.referenceGenerator;
				if (reference)
				{
					int voxelcount = reference.GetBlockCount(TargetDepth);
					if (TargetDimension >= reference.DimensionCount)
					{
						Errors += "Target dimension is greater or equal the dimension count of the voxel generator\n";
					}


					if (TargetDimension < 0)
					{
						Errors += "Negative target dimensions are forbidden\n";
					}


					if (voxelcount <= 0)
					{
						Errors += "Negative or zero modification count is forbidden. Check your target depth which is probably way too high\n";
					}

					if (voxelcount > 10000000)
					{
						Errors += "You exceed the safety margin of 10000000 Voxels. This is not save anymore. Your target depth is way to high!\n\n";
						if (IgnoreWarnings)
						{
							Errors += "You are ignoring this warning. Be careful!\n";
							Errors += "Automatic updating when values change and loading on start is disabled";
						}
					}


				}
			}
			else
			{
				generator = GetComponentInParent<WorldGenerator>();
				Errors = "Parent Game Object has no World Generator component!";
			}

			return Errors;
		}
	}

#if UNITY_EDITOR
	[CanEditMultipleObjects]
	[CustomEditor(typeof(WorldAlgorithmCluster))]
	public class WorldAlgorithmClusterEditor : Editor
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



			WorldAlgorithmCluster myTarget = (WorldAlgorithmCluster)target;




			DrawDefaultInspector();

			if (myTarget.generator)
			{
				VoxelGenerator reference = myTarget.generator.referenceGenerator;
				if (reference)
				{

					int voxelcount = reference.GetBlockCount(myTarget.TargetDepth);
					EditorGUILayout.LabelField("Voxel Count " + voxelcount);
				}
			}

			string Errors = myTarget.ErrorCheck();
			if (Errors != "")
			{
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField("<color=red>Errors:</color>", bold);
					EditorGUILayout.TextArea(Errors);
				}
			}

			if (GUI.changed && Errors == "")
			{
				if (myTarget.generator)
				{
					if (myTarget.generator.referenceGenerator)
					{
						if (myTarget.generator.referenceGenerator.IsInitialized)
						{
							myTarget.generator.InitAlgorithms();
							myTarget.generator.Generate(myTarget.generator.referenceGenerator);
						}
					}

				}
			}

		}
	}
#endif


}
