using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using System;
using Unity.Burst;
using Fraktalia.Core.FraktaliaAttributes;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen.World
{
	[ExecuteInEditMode]
	public class WorldAlgorithm : MonoBehaviour
	{
		[BeginInfo("WORLDALGORITHM")]
		[InfoTitle("World Algorithm", "This component is a world algorithm and should be attached as child to the game object containing the WorldAlgorithmCluster component. " +
			"The World Generator parameter is assigned automatically and is accessed when auto updating whenever values change.\n\n" +
			"The main parameter is the <b>Apply Function</b> which defines how the generated values are mixed together. Processing the world is done in a top down fashion so the " +
			"child order matters. Usually the first algorithm should have the Apply Function defined at Set. Then the other following algorithms can use any apply mode.\n\n" +
			"The second parameter is the Post Process Function. The Post Process Function is applied after calculating the value. " +
			"For example NoNegatives prevents negative values which may cause unexpected results.", "WORLDALGORITHM")]
		[InfoSection1("Apply Functions:", "" +
		"<b>Set:</b> Set the calculated value, overwrites previous values.\n" +
		"<b>Add:</b> Adds the value to the value calucaled by the previous algorithm. Keep in mind that negative values are possible.\n" +
		"<b>Subtract:</b> Subtracts the value to the value calucaled by the previous algorithm. Keep in mind that negative values are possible.\n" +
		"<b>Min:</b> Overwrites the smaller value (previous vs current). Ideal for cave creation\n" +
		"<b>Max:</b> Overwrites the higher value (previous vs current).\n" +
		"<b>Invert Set/Add/Subtract/Min/Max:</b> Applies the inverted value. Inversion is 255 - value. Beware negative numbers can drastically change the result!\n", "WORLDALGORITHM")]

		[InfoSection2("Post Process:", "" +
		"<b>None:</b> Nothing is applied.\n" +
		"<b>No Negatives:</b> Negative values are set to 0.\n", "WORLDALGORITHM")]
		[InfoVideo("https://www.youtube.com/watch?v=3KrPFj9hUcA&lc=UgzjAqdGrVsM77feBBN4AaABAg", false, "WORLDALGORITHM")]
		[InfoText("World Algorithm:", "WORLDALGORITHM")]
		public WorldGenerator worldGenerator;
		
		public ApplyMode ApplyFunction;
		public PostProcess PostProcessFunction;


		[NonSerialized]
		public float scale;

		[NonSerialized]
		public int Depth;

		
		public bool Disabled;
		protected FunctionPointer<WorldAlgorithm_Mode> ApplyFunctionPointer;
		protected FunctionPointer<WorldAlgorithm_PostProcess> PostProcessFunctionPointer;



		private void OnDrawGizmosSelected()
		{
			
		}

		public virtual void Initialize(VoxelGenerator template)
		{
			
		}

		public void DefineApplyMode()
		{
			switch (ApplyFunction)
			{
				case ApplyMode.Set:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.Set);
					break;
				case ApplyMode.Add:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.Add);
					break;
				case ApplyMode.Subtract:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.Subtract);
					break;
				case World.ApplyMode.Min:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.Min);
					break;
				case World.ApplyMode.Max:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.Max);
					break;
				case World.ApplyMode.InvertSet:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.InvertSet);
					break;
				case World.ApplyMode.InvertAdd:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.InvertAdd);
					break;
				case ApplyMode.InvertSubtract:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.InvertSubtract);
					break;
				case World.ApplyMode.InvertMin:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.InvertMin);
					break;
				case World.ApplyMode.InvertMax:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.InvertMax);
					break;
				default:
					ApplyFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_Mode>(WorldAlgorithm_Modes.Set);
					break;
			}

			switch (PostProcessFunction)
			{
				case PostProcess.None:
					PostProcessFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_PostProcess>(WorldAlgorithm_PostProcesses.Nothing);
					break;
				case PostProcess.NoNegatives:
					PostProcessFunctionPointer = BurstCompiler.CompileFunctionPointer<WorldAlgorithm_PostProcess>(WorldAlgorithm_PostProcesses.NoNegatives);
					break;
				default:
					break;
			}
		}


		public virtual JobHandle Apply(Vector3 hash, VoxelGenerator targetGenerator, ref JobHandle handle)
		{

			return handle;


		}

		public virtual void CleanUp()
		{

		}	

		private void OnDestroy()
		{
			Disabled = true;
			UpdateWorld();
		}

		private void Start()
		{
			if (worldGenerator == null) worldGenerator = GetComponentInParent<WorldGenerator>();
			UpdateWorld();
		}

		public void UpdateWorld()
		{	
			if (worldGenerator)
			{
				if (worldGenerator.referenceGenerator)
				{
					if (worldGenerator.referenceGenerator.IsInitialized)
					{
						worldGenerator.InitAlgorithms();
						worldGenerator.Generate(worldGenerator.referenceGenerator);
					}
				}
			}
		}
	}

	public enum ApplyMode
	{
		Set,
		Add,
		Subtract,
		Min,
		Max,
		InvertSet,
		InvertAdd,
		InvertSubtract,
		InvertMin,
		InvertMax,
	}

	public enum PostProcess
	{
		None,
		NoNegatives
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(WorldAlgorithm), true)]
	public class WorldAlgorithmEditor : Editor
	{
		private void OnEnable()
		{	
			Undo.undoRedoPerformed += UpdateWorld;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UpdateWorld;
		}

		private void UpdateWorld()
		{					
			WorldAlgorithm myTarget = (WorldAlgorithm)target;

			if (myTarget.worldGenerator)
			{
				if (myTarget.worldGenerator.referenceGenerator)
				{
					if (myTarget.worldGenerator.referenceGenerator.IsInitialized)
					{
						myTarget.worldGenerator.InitAlgorithms();
						myTarget.worldGenerator.Generate(myTarget.worldGenerator.referenceGenerator);
					}
				}

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






			DrawDefaultInspector();

			if (GUI.changed)
			{
				UpdateWorld();
			}

		}
	}
#endif
}
