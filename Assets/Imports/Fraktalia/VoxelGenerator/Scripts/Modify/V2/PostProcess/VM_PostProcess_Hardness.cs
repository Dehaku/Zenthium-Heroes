using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen.Modify
{
	public class VM_PostProcess_Hardness : VM_PostProcess
	{
		[BeginInfo("HARDNESS")]
		[InfoTitle("Hardness System", "This post-processing system implements hardness to voxel modifier V2. Hardness only has an effect when Additive/Subtractive modes are used. " +
		"The hardness itself is a multiplier, and the ID of the modification data is multiplied by the hardness. <b>This means that a hardness of 0 makes the voxel indestructible.</b> " +
		"A value of 1 or greater makes the voxel soft and will be modified very quickly. The default value is 1.\n\n" +
		"<b>Histogram:</b>\n\n" +
		"In order to implement hardness, a histogram is required which contains the hardness factor for each individual ID a voxel can have and is between 0 and 255.", "HARDNESS")]
		[InfoSection1("How to use:", "You have to define the hardness dimension first. Usually it is the texture dimension, like dimension 1. " +
			"Then you have to define the histogram either by directly manipulating the values in the histogram array or by using the histogram designing helpers. " +
			"The animation curve window allows you to define the hardness by changing the key points. " +
			"You can also define the MinID and MaxID and Min Max Hardness and then click on the [Apply MinID/MaxID hardness]", "HARDNESS")]
		[InfoSection2("Other information:", "" +
			"<b>Negative Hardness:</b> If the hardness value is negative, material will be added when Subtraction is applied and removed when Addition is applied. " +
			"Negative values basically reverse the modification. Set [No Negatives] to true in order to prevent negative numbers.\n\n" +
			"For easier manipulation of the histogram curve, the voxel ID range of [0-255] is remapped to [0-1]. The values on the Y axis represents the hardness.", "HARDNESS")]
		[InfoText("Hardness System:", "HARDNESS")]
		public int HardnessDimension=1;
		
		public bool NoNegatives;
		public bool NoGreaterOne;

		
	
		public float[] Histogramm = new float[256];

		
		[Tooltip("For easier design of that Histogramm. Range(X axis) = ID between (0-1) and is mapped to (0,256). Y is the hardness value.")]
		[HideInInspector]
		public AnimationCurve HistogrammCurve;
		[Header("Histogramm designing tools:")]
		[Range(0,255)]
		public int MinID;
		[Range(0, 255)]
		public int MaxID;
		public float MinMaxHardness;

		public override void ApplyPostprocess(FNativeList<NativeVoxelModificationData_Inner> modifierData, VoxelGenerator generator, VoxelModifier_V2 modifier)
		{
			var nhistogram = Fraktalia.Utility.ContainerStaticLibrary.GetArray_float(256);
			
			nhistogram.CopyFrom(Histogramm);
			HardnessJob job = new HardnessJob();
			job.histogramm = nhistogram;
			job.modifierData = modifierData;
			job.data = generator.Data[HardnessDimension];
			job.NoNegatives = NoNegatives ? 1 : 0;
			job.NoGreaterOne = NoGreaterOne ? 1 : 0;

			job.Schedule(modifierData.Length, modifierData.Length / SystemInfo.processorCount).Complete();
		}

		public void CreateHistogramm(AnimationCurve curve)
		{
			if (Histogramm == null || Histogramm.Length != 256)
			{
				Histogramm = new float[256];
			}

			for (int i = 0; i < 256; i++)
			{
				float value = curve.Evaluate(i / 256.0f);
				Histogramm[i] = value;

			}			
		}

		public void SetHardness(int MinID, int MaxID, float Hardness)
		{
			for (int i = MinID; i <= MaxID; i++)
			{
				if(i >= 0 && i < Histogramm.Length)
				{
					Histogramm[i] = Hardness;
				}
			}

			UpdateHistogrammCurve();
		}

		public void SetAllHardness(float Hardness)
		{
			SetHardness(0, 256, Hardness);
		}


		public void UpdateHistogrammCurve()
		{
			HistogrammCurve = new AnimationCurve();
			for (int i = 0; i < 256; i++)
			{
				Keyframe frame = new Keyframe(i / 256.0f, Histogramm[i]);
				frame.weightedMode = WeightedMode.None;
				HistogrammCurve.AddKey(frame);
			
			}
		}
	}

	[BurstCompile]
	public struct HardnessJob : IJobParallelFor
	{

		public NativeVoxelTree data;
		
		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> modifierData;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<float> histogramm;

		public int NoNegatives;
		public int NoGreaterOne;

		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner modifier = modifierData[index];
			
			int Value = data._PeekVoxelId_InnerCoordinate(modifier.X, modifier.Y, modifier.Z, 20, 0, 128);
			float factor = histogramm[Value];

			if(NoNegatives == 1)
			{
				factor = Mathf.Max(factor, 0);
			}

			if (NoGreaterOne == 1)
			{
				factor = Mathf.Min(factor, 1);
			}


			modifier.ID = (int)(modifier.ID * factor);
			modifierData[index] = modifier;
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(VM_PostProcess_Hardness))]
	[CanEditMultipleObjects]
	public class VoxelModifier_HardnessEditor : Editor
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
			VM_PostProcess_Hardness mytarget = target as VM_PostProcess_Hardness;

			if (mytarget.HistogrammCurve == null) mytarget.SetAllHardness(1);

			DrawDefaultInspector();

			if (GUI.changed)
			{
				//mytarget.UpdateHistogrammCurve();
				EditorUtility.SetDirty(target);
			}

			
			EditorGUILayout.PropertyField(serializedObject.FindProperty("HistogrammCurve"));
			

			if (GUILayout.Button("Apply Histogramm from Curve"))
			{
				mytarget.CreateHistogramm(mytarget.HistogrammCurve);
			}

			if (GUILayout.Button("Apply MinID/MaxID hardness"))
			{	
				mytarget.SetHardness(mytarget.MinID, mytarget.MaxID, mytarget.MinMaxHardness);
			}


			if (GUI.changed)
			{
				serializedObject.ApplyModifiedProperties();
				mytarget.CreateHistogramm(mytarget.HistogrammCurve);
				mytarget.UpdateHistogrammCurve();
				EditorUtility.SetDirty(target);
			}



		}
	}

#endif


}
