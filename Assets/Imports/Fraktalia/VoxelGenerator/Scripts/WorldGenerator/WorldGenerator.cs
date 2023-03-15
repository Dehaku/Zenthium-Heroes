using Fraktalia.Utility.NativeNoise;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Fraktalia.Core.FraktaliaAttributes;
using System;
using System.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen.World
{
	public class WorldGenerator : MonoBehaviour
	{
		const int WORLD_PERMUTATIONS = 1024;

		public PermutationTable_Native Permutation;


		[BeginInfo("WORLDGENERATOR")]
		[InfoTitle("World Generator", "The world generator is a sub system similar to the save system and is the main tool for procedural world generator. " +
			"This component can be directly attached to the game object containing the Voxel Generator and it is applied automatically like the save system. " +
			"But it is also possible to directly apply it to your desired Voxel Generator by calling <color=blue>Generate(VoxelGenerator targetGenerator)</color>.", "WORLDGENERATOR")]
		[InfoSection1("How to use:", "The world generator is a root object of a world generation hierarchy using <b>WorldAlgorithmCluster</b>. Every cluster handles one target " +
			"dimension and target depth. Ideally you have one cluster for each voxel dimension.\n\nEvery WorldAlgorithmCluster contains as many WorldAlgorithms as desired. " +
			"The WorldAlgorithms itself are the main calculation scripts for procedural world generation and can be mixed together using a variety of apply modes.\n\n" +
			"Randomness is fully deterministic using the seed value as initial parameter. Also the World Generator has a built in work system which is important for " +
			"multi block setups. Every time <color=blue>Generate(VoxelGenerator targetGenerator)</color> is called, a generation request is added and will be processed " +
			"asynchronly in the background.\n\n" +
			"When generating multi block setups, the world generator uses the hash value provided by the Voxel Generator to define the position of the chunk.", "WORLDGENERATOR")]
		[InfoSection2("General Settings:", "" +
		"<b>World Seed:</b> Seed for initial randomness generation.\n" +
		"<b>Algorithm Clusters:</b> Displays which algorithm clusters are applied.\n" +
		"<b>Reference Generator:</b> Reference Generator used for safety system.\n" +
		"<b>Scale Invariant:</b> When true, volume size of the Voxel Generator does not influence generated terrain. \n" +
		"", "WORLDGENERATOR")]
		[InfoSection3("Additional Information", "When the Reference Voxel Generator is initialized, the result will be updated automatically whenever algorithm parameter change. " +
			"The security system prevents auto update if the modification count exceeds a safe amount or if algorithms and cluster contain errors. " +
			"A detailed explaination is impossible here. <b>Therefore watching the tutorial video is highly recommended.</b>", "WORLDGENERATOR")]
		[InfoVideo("https://www.youtube.com/watch?v=3KrPFj9hUcA&lc=UgzjAqdGrVsM77feBBN4AaABAg", false, "WORLDGENERATOR")]
		[InfoText("World Generator:", "WORLDGENERATOR")]
		public string WorldSeed;

		public WorldAlgorithmCluster[] AlgorithmCluster;

		[Fraktalia.Core.FraktaliaAttributes.ReadonlyText]
		public int Works;

		public VoxelGenerator referenceGenerator;
		[Tooltip("If true, generation will always adapt to volume size. Else generation is pure world space and volume size defines the measured region.")]
		public bool ScaleInvariant = false;
		public bool GenerateOnStart;
		[Tooltip("Automatically loads inside editor when not initialized.")]
		public bool AutoLoadInEditor;

		public Stack<VoxelGenerator> workStack = new Stack<VoxelGenerator>();

		public IEnumerator ActiveCoroutine;

		[NonSerialized]
		public bool IsInitialized = false;

		private int skips = 0;
		public VoxelModificationReservoir modificationReservoir;

		[NonSerialized]
		private bool hasloaded = false;

		private string currentSeed = "";


		private void OnDrawGizmos()
		{
#if UNITY_EDITOR
			if (EditorApplication.isCompiling)
			{
				return;	
			}
#endif



			if (Application.isPlaying) return;
			if (!referenceGenerator) return;

			if (IsInitialized) return;


			if (AutoLoadInEditor && IsSafe() && enabled)
			{
				if (!hasloaded)
				{
					if (referenceGenerator.savesystem == null || !referenceGenerator.savesystem.AutoLoad || 
						!referenceGenerator.savesystem.enabled || !referenceGenerator.savesystem.SaveDataExists)
					{
						InitializeGeneratorAndApplyWorld();
					}
				}

				hasloaded = true;
			}

		}


		public void Initialize(VoxelGenerator generator)
		{
			modificationReservoir = new VoxelModificationReservoir(generator);

			referenceGenerator = generator;
			if (!Permutation.IsCreated)
			{
				Permutation = new PermutationTable_Native(WORLD_PERMUTATIONS, 255, WorldSeed.GetHashCode());
			}

			currentSeed = WorldSeed;

			AlgorithmCluster = GetComponentsInChildren<WorldAlgorithmCluster>();
			for (int i = 0; i < AlgorithmCluster.Length; i++)
			{			
				AlgorithmCluster[i].generator = this;
				AlgorithmCluster[i].Initialize();
			}

			ActiveCoroutine = processWorldGeneration();
			IsInitialized = true;
		}

		public void InitAlgorithms()
		{
			AlgorithmCluster = GetComponentsInChildren<WorldAlgorithmCluster>();
			for (int i = 0; i < AlgorithmCluster.Length; i++)
			{
				AlgorithmCluster[i].generator = this;
				AlgorithmCluster[i].Initialize();
			}
		}


		public void Generate(VoxelGenerator targetGenerator)
		{
			if (targetGenerator == null) return;
			if (targetGenerator.Locked) return;
			if (targetGenerator.VoxelTreeDirty) return;
			//if (!targetGenerator.IsGarbageFree) return;

			if (!targetGenerator.IsInitialized)
			{
				targetGenerator.GenerateBlock();
			}
			targetGenerator.Locked = true;

			workStack.Push(targetGenerator);
		}

		public void UpdateRoutines()
		{
			ActiveCoroutine.MoveNext();
			Works = workStack.Count;
		}

		private IEnumerator processWorldGeneration()
		{
			VoxelGenerator targetGenerator = null;
			while (IsInitialized)
			{
                if (!currentSeed.Equals(WorldSeed))
                {
					Permutation.CleanUp();
					Permutation = new PermutationTable_Native(WORLD_PERMUTATIONS, 255, WorldSeed.GetHashCode());
					currentSeed = WorldSeed;
					if (referenceGenerator)
                    {
						if(IsSafe())
                        {
							workStack.Push(referenceGenerator);
						} 
					}
						
				}


				if (workStack.Count > 0)
				{
					targetGenerator = workStack.Peek();
				}
				else
				{
					yield return null;
					continue;
				}

				if (targetGenerator)
				{
					float scale = ScaleInvariant ? targetGenerator.RootSize : 1;
								
					for (int i = 0; i < AlgorithmCluster.Length; i++)
					{
						WorldAlgorithmCluster cluster = AlgorithmCluster[i];
						
						if (cluster.IsSafe(targetGenerator))
						{							
							cluster.scale = scale;
							JobHandle handle = new JobHandle();
						
							for (int y = 0; y < cluster.Algorithms.Count; y++)
							{
								var algorithm = cluster.Algorithms[y];
								if (algorithm.Disabled) continue;
								algorithm.scale = scale;

								handle = algorithm.Apply(targetGenerator.ChunkHash, targetGenerator, ref handle);
								
							}

							while (!handle.IsCompleted)
							{
								skips++;
								if (skips < 3)
								{
									yield return null;
								}
							}
							handle.Complete();
							
							cluster.Finish(targetGenerator);
				
						}

					}
					
					targetGenerator.Locked = false;
					targetGenerator.SetAllRegionsDirty();
				}
				targetGenerator = null;
				skips = 0;

				
				targetGenerator = workStack.Pop();
			}
		}

		public void CleanUp()
		{
			if (AlgorithmCluster != null)
			{
				for (int i = 0; i < AlgorithmCluster.Length; i++)
				{
					AlgorithmCluster[i].CleanUp();
				}
			}

			Permutation.CleanUp();
			workStack.Clear();
			if (modificationReservoir != null) modificationReservoir.CleanData();

		}

		public bool IsActive
		{
			get
			{
				if (!referenceGenerator) return false;
				if (referenceGenerator.IsInitialized) return true;

				return false;
			}
		}

		public void Start()
		{
			if (GenerateOnStart && IsSafe())
			{
				if (referenceGenerator.savesystem == null || !referenceGenerator.savesystem.LoadOnStart || !referenceGenerator.savesystem.enabled)
				{
					InitializeGeneratorAndApplyWorld();
				}						
			}
		}

		public void InitializeGeneratorAndApplyWorld()
		{
			if (referenceGenerator)
			{
				Generate(referenceGenerator);
			}
		}		

		public bool IsSafe()
		{
			AlgorithmCluster = GetComponentsInChildren<WorldAlgorithmCluster>();
			for (int i = 0; i < AlgorithmCluster.Length; i++)
			{
				if (AlgorithmCluster[i].IgnoreWarnings) return false;
			}

			return true;
		}

		public string ErrorCheck()
		{
			string Errors = "";
			AlgorithmCluster = GetComponentsInChildren<WorldAlgorithmCluster>();
			for (int i = 0; i < AlgorithmCluster.Length; i++)
			{
				string check = AlgorithmCluster[i].ErrorCheck();
				if(check != "")
				{
					Errors += "Errors: " + AlgorithmCluster[i].name + "\n";
					Errors += check;
					Errors += "\n\n";
				}


			}

			return Errors;
		}
	}

#if UNITY_EDITOR
	[CanEditMultipleObjects]
	[CustomEditor(typeof(WorldGenerator))]
	public class WorldGeneratorEditor : Editor
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



			WorldGenerator myTarget = (WorldGenerator)target;

			


			DrawDefaultInspector();


			myTarget.referenceGenerator = myTarget.GetComponentInChildren<VoxelGenerator>();

			if (GUILayout.Button("Generate World"))
			{
				myTarget.InitializeGeneratorAndApplyWorld();
			}

			string Errors = "";
			if (!myTarget.IsSafe())
			{
				
					Errors += "One or more algorithm clusters are ignoring warnings. This is unsafe.\n\n" +
					"Automatic updating and loading at startup is disabled as long as warnings are ignored. You can create the world manually by clicking on generate world or by " +
					"your own scripts.";
					
			}

			Errors += myTarget.ErrorCheck();

			if (Errors != "")
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("<color=red>Errors:</color>", bold);
				EditorGUILayout.TextArea(Errors);
			}
		}
	}
#endif

}
