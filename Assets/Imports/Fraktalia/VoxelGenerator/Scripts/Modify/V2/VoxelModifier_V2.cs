using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Fraktalia.Core.FraktaliaAttributes;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.VoxelGen.Modify
{
	public enum VoxelModifierMode
	{
		Set,
		Additive,
		Subtractive,
		Smooth
	}

	[ExecuteInEditMode]	
	public class VoxelModifier_V2 : MonoBehaviour
	{



		public const int SAFETYLIMIT = 100000;

		[Range(0, 5)]
		public int TargetDimension;
		[Range(1, NativeVoxelTree.MaxDepth)]
		[Tooltip("Default Depth when ModifyAtPos is called")]
		public int Depth = 7;

		
		public VoxelModifierMode Mode;
		[Range(0,1)]
		public float Opacity = 1;

		
		public VoxelModifier_Target TargetingModule;
		public VoxelShape_Base ShapeModule;
		public List<VM_PostProcess> PostProcessModule = new List<VM_PostProcess>();
		public bool FetchPostProcessModules;

		
		public bool RequireVoxelData;

		[Tooltip("Adds a half voxel size to the offset in order to center the result")]
		public bool MarchingCubesOffset = true;

		[NonSerialized]
		public string ErrorMessage = "";

		public VoxelGenerator ReferenceGenerator
		{
			get
			{
				VoxelGenerator reference = null;
				if (TargetingModule)
				{
					reference = TargetingModule.Reference;
				}

				if (!reference) reference = GetComponent<VoxelGenerator>();
				return reference;
			}
		}


		private List<VoxelGenerator> targets = new List<VoxelGenerator>();
		public int VoxelCount { get; private set; }


		private FNativeList<NativeVoxelModificationData_Inner> modifierData;
		private FNativeList<NativeVoxelModificationData_Inner> preVoxelData;
		private FNativeList<NativeVoxelModificationData_Inner> postVoxelData;

		public Queue<IEnumerator> UpdateModificationProcess = new Queue<IEnumerator>();
		IEnumerator CurrentModificationProcess;


		private RepositionSmoothJob smooth;
		private GatherInformationJob gatherpredata;
		private RepositionJob reposition;
		private OpacityJob setopacity;

		private void OnDrawGizmosSelected()
		{
			if(ShapeModule && TargetingModule)
			{
				VoxelGenerator reference = ReferenceGenerator;
				if (reference)
				{
					VoxelCount = ShapeModule.GetVoxelModificationCount(this, reference);
					DrawEditorPreview(transform.position, Vector3.up);
				}
			}
		}

		private void OnDrawGizmos()
		{
			if (!Application.isPlaying)
			{
				Update();
			}
		}


		public void DrawEditorPreview(Vector3 worldPosition, Vector3 normal)
		{
			if(ShapeModule) ShapeModule.DrawEditorPreview(VoxelCount >= 0 && VoxelCount < SAFETYLIMIT, worldPosition, normal);
		}

		public void ApplyVoxelModifier(Vector3 worldPosition)
		{
			if(FetchPostProcessModules)
			{
				PostProcessModule.Clear();
				PostProcessModule.AddRange(GetComponentsInChildren<VM_PostProcess>());
			}

			EvaluateTarget(worldPosition);
			for (int i = 0; i < targets.Count; i++)
			{
				int modificationcount = ShapeModule.GetVoxelModificationCount(this, targets[i]);
				if(modificationcount > 0 && modificationcount < SAFETYLIMIT)
				{
					UpdateModificationProcess.Enqueue(ModifyGenerator(targets[i], worldPosition, Mode));
					Update();
				}			
			}

			
		}

		public Vector3 GetGameIndicatorSize()
		{
			if (!ShapeModule) return Vector3.zero;
			return ShapeModule.GetGameIndicatorSize();
		}


		private void EvaluateTarget(Vector3 worldPosition)
		{
			targets.Clear();
		
			if (TargetingModule)
			{
				TargetingModule.fetchGenerators(targets, worldPosition);
			}
			else
			{
				VoxelGenerator target = GetComponent<VoxelGenerator>();
				if (target)
				{
					targets.Add(target);
				}
			}
		}

		private IEnumerator ModifyGenerator(VoxelGenerator generator, Vector3 worldPosition, VoxelModifierMode mode)
		{
			if (!generator.IsInitialized)
			{
				CurrentModificationProcess = null;
				yield break;
			}
			if (!modifierData.IsCreated) modifierData = new FNativeList<NativeVoxelModificationData_Inner>(Allocator.Persistent);
			if (!preVoxelData.IsCreated) preVoxelData = new FNativeList<NativeVoxelModificationData_Inner>(Allocator.Persistent);
			if (!postVoxelData.IsCreated) postVoxelData = new FNativeList<NativeVoxelModificationData_Inner>(Allocator.Persistent);

			generator.savesystem?.SetDirty();
			if (ShapeModule.RequiresRecalculation(this, generator))
			{
				ShapeModule.CalculateDisplacement(worldPosition, this, generator);
				ShapeModule.CreateModifierTemplate(this, generator);
			}

			if (modifierData.Length != ShapeModule.ModifierTemplateData.Length)
			{
				modifierData.Resize(ShapeModule.ModifierTemplateData.Length, NativeArrayOptions.UninitializedMemory);
			}

			Vector3 worldoffset = worldPosition + ShapeModule.GetOffset(this, generator);	
			Vector3Int offset = generator.ConvertWorldToInner(worldoffset, generator.RootSize);

			var manifest = VoxelUndoSystem.GetManifestElement();
			manifest.AffectedTarget = generator;
			manifest.Dimension = TargetDimension;
			VoxelUndoSystem.AddManifestElement(manifest);

			if (mode == VoxelModifierMode.Smooth)
			{			 
				smooth.template = ShapeModule.ModifierTemplateData;
				smooth.results = modifierData;
				smooth.Offset_X = offset.x;
				smooth.Offset_Y = offset.y;
				smooth.Offset_Z = offset.z;
				smooth.Depth = (byte)Depth;
				smooth.Opacity = Opacity;
				smooth.data = generator.Data[TargetDimension];
				smooth.BoxWidth = 1;
				smooth.InnerVoxelSize = generator.GetInnerVoxelSize(Depth);
				smooth.Schedule(ShapeModule.ModifierTemplateData.Length, ShapeModule.ModifierTemplateData.Length / SystemInfo.processorCount).Complete();

				if (RequireVoxelData || !Application.isPlaying)
				{

					preVoxelData.Resize(ShapeModule.ModifierTemplateData.Length, NativeArrayOptions.UninitializedMemory);
					postVoxelData.Resize(ShapeModule.ModifierTemplateData.Length, NativeArrayOptions.UninitializedMemory);
					
					gatherpredata.data = generator.Data[TargetDimension];
					gatherpredata.readvoxeldata = modifierData;
					gatherpredata.resultvoxeldata = preVoxelData;
					gatherpredata.Schedule(modifierData.Length, modifierData.Length / SystemInfo.processorCount).Complete();

					manifest.PreviousData_Inner.AddRange(gatherpredata.resultvoxeldata);

				}

				for (int i = 0; i < PostProcessModule.Count; i++)
				{
					VM_PostProcess process = PostProcessModule[i];
					if (process && process.Enabled)
					{
						process.ApplyPostprocess(modifierData, generator, this);
					}						
				}

				manifest.ModificationData_Inner.AddRange(modifierData);
				generator._SetVoxels_Inner(modifierData, TargetDimension);

			}
			else
			{
				
				reposition.template = ShapeModule.ModifierTemplateData;
				reposition.results = modifierData;
				reposition.Offset_X = offset.x;
				reposition.Offset_Y = offset.y;
				reposition.Offset_Z = offset.z;
				reposition.Depth = (byte)Depth;		
				reposition.Schedule(ShapeModule.ModifierTemplateData.Length, ShapeModule.ModifierTemplateData.Length / SystemInfo.processorCount).Complete();

				if (RequireVoxelData || !Application.isPlaying)
				{

					preVoxelData.Resize(ShapeModule.ModifierTemplateData.Length, NativeArrayOptions.UninitializedMemory);
					postVoxelData.Resize(ShapeModule.ModifierTemplateData.Length, NativeArrayOptions.UninitializedMemory);
					GatherInformationJob gatherpredata = new GatherInformationJob();
					gatherpredata.data = generator.Data[TargetDimension];
					gatherpredata.readvoxeldata = modifierData;
					gatherpredata.resultvoxeldata = preVoxelData;
					gatherpredata.Schedule(modifierData.Length, modifierData.Length / SystemInfo.processorCount).Complete();

					manifest.PreviousData_Inner.AddRange(gatherpredata.resultvoxeldata);
				}

				
				setopacity.template = modifierData;
				setopacity.results = modifierData;
				setopacity.Opacity = Opacity;
				if (mode == VoxelModifierMode.Subtractive)
				{
					setopacity.Opacity = -Opacity;
				}
				setopacity.Schedule(ShapeModule.ModifierTemplateData.Length, ShapeModule.ModifierTemplateData.Length / SystemInfo.processorCount).Complete();


				for (int i = 0; i < PostProcessModule.Count; i++)
				{
					VM_PostProcess process = PostProcessModule[i];
					if (process && process.Enabled)
					{
						process.ApplyPostprocess(modifierData, generator, this);
					}
				}

				manifest.ModificationData_Inner.AddRange(modifierData);
				
				if (mode == VoxelModifierMode.Additive || Mode == VoxelModifierMode.Subtractive)
				{
					manifest.Additive = true;
					generator._SetVoxelsAdditive_Inner(modifierData, TargetDimension);
				}
				if (mode == VoxelModifierMode.Set)
				{
					generator._SetVoxels_Inner(modifierData, TargetDimension);
				}
			}

			ShapeModule.SetGeneratorDirty(this, generator, worldPosition);

			if (RequireVoxelData)
			{
				if (Application.isPlaying)
				{
					while (generator.HullsDirty)
					{
						yield return null;
					}
					
					gatherpredata.data = generator.Data[TargetDimension];
					gatherpredata.readvoxeldata = modifierData;
					gatherpredata.resultvoxeldata = postVoxelData;
					gatherpredata.Schedule(modifierData.Length, modifierData.Length / SystemInfo.processorCount).Complete();
				}
			}

			for (int i = 0; i < PostProcessModule.Count; i++)
			{
				VM_PostProcess process = PostProcessModule[i];
				if (process && process.Enabled)
				{
					process.FinalizeModification(modifierData, preVoxelData, postVoxelData, generator, this);
				}
			}

			CurrentModificationProcess = null;
		}

		public void CleanUp()
		{
			if (modifierData.IsCreated) modifierData.Dispose();
			if (preVoxelData.IsCreated) preVoxelData.Dispose();
			if (postVoxelData.IsCreated) postVoxelData.Dispose();

			if (ShapeModule) ShapeModule.CleanUp();
			CurrentModificationProcess = null;
			UpdateModificationProcess.Clear();
		}

		public static void CleanAll()
		{
			VoxelModifier_V2[] shapes = GameObject.FindObjectsOfType<VoxelModifier_V2>(true);
			for (int i = 0; i < shapes.Length; i++)
			{
				shapes[i].CleanUp();
			}
		}

		private void OnDestroy()
		{
			CleanUp();
		}

		public bool SafetyCheck()
		{
			ErrorMessage = "";
			bool isSave = true;


			if(ShapeModule == null)
			{
				ShapeModule = GetComponent<VoxelShape_Base>();
				if(!ShapeModule)
				{
					ErrorMessage += "> ShapeModule is missing. Please add one as component (for example Sphere) \n\n";
					isSave = false;
					
				}
			}

			if (TargetingModule == null)
			{
				TargetingModule = GetComponent<VoxelModifier_Target>();
				if (!TargetingModule)
				{
					ErrorMessage += "> TargetingModule is missing! How should it know which voxel generator to modify? Therefore please add one as component. \n\n";
					isSave = false;
					
				}
			}

			if (!isSave) return isSave;

			VoxelGenerator reference = ReferenceGenerator;
			if(reference)
			{
				int modificationcount = ShapeModule.GetVoxelModificationCount(this, reference);
				if (!(modificationcount > 0 && modificationcount < SAFETYLIMIT))
				{
					ErrorMessage += "> This setting would modify more than 100000 voxels on the reference generaor: " + reference +
						". Modifier will not be applied on this generator but can still be applied on other generators which are deemed safe!\n\n";

					isSave = false;
				}
			}
			
			return isSave;
		}

		private void Update()
		{
			for (int i = 0; i < 5; i++)
			{
				if (CurrentModificationProcess == null)
				{
					if (UpdateModificationProcess.Count > 0)
						CurrentModificationProcess = UpdateModificationProcess.Dequeue();
				}

				if (CurrentModificationProcess != null)
				{
					
					CurrentModificationProcess.MoveNext();

					
				}
			}
		}
	}

	[BurstCompile]
	public struct RepositionJob : IJobParallelFor
	{
		public int Offset_X;
		public int Offset_Y;
		public int Offset_Z;
		public byte Depth;
		

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> template;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> results;


		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner result = template[index];
			result.X = result.X + Offset_X;
			result.Y = result.Y + Offset_Y;
			result.Z = result.Z + Offset_Z;
			result.Depth = Depth;
			results[index] = result;
		}
	}

	[BurstCompile]
	public struct OpacityJob : IJobParallelFor
	{
		public float Opacity;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> template;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> results;


		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner result = template[index];
			result.ID = (int)(result.ID * Opacity);
			results[index] = result;
		}
	}



	[BurstCompile]
	public struct RepositionSmoothJob : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;
		public int BoxWidth;
		public int InnerVoxelSize;

		public int Offset_X;
		public int Offset_Y;
		public int Offset_Z;
		public byte Depth;
		public float Opacity;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> template;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> results;

		
		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner result = template[index];
			result.X = result.X + Offset_X;
			result.Y = result.Y + Offset_Y;
			result.Z = result.Z + Offset_Z;
			result.Depth = Depth;
			int TargetID = (int)(result.ID * Opacity);

			int FinalValue = data._PeekVoxelId_InnerCoordinate(result.X, result.Y, result.Z, 20, 0, 128);
			int PeekedID = 0;
			int count = 0;
			for (int a = -BoxWidth; a <= BoxWidth; a++)
			{
				for (int b = -BoxWidth; b <= BoxWidth; b++)
				{
					for (int c = -BoxWidth; c <= BoxWidth; c++)
					{
						int fx = result.X + a * InnerVoxelSize;
						int fy = result.Y + b * InnerVoxelSize;
						int fz = result.Z + c * InnerVoxelSize;

						int Value = data._PeekVoxelId_InnerCoordinate(fx, fy, fz, 20, 0, 128);
						PeekedID += Value;
						count++;
					}
				}
			}

			PeekedID /= count;

			if (FinalValue > PeekedID)
			{
				FinalValue = Mathf.Max(PeekedID, FinalValue - TargetID);
			}
			else if (FinalValue < PeekedID)
			{
				FinalValue = Mathf.Min(PeekedID, FinalValue + TargetID);
			}

			result.ID = FinalValue;



			results[index] = result;
		}
	}

	[BurstCompile]
	public struct GatherInformationJob : IJobParallelFor
	{

		public NativeVoxelTree data;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> readvoxeldata;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> resultvoxeldata;

		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner modifier = readvoxeldata[index];
			int Value = data._PeekVoxelId_InnerCoordinate(modifier.X, modifier.Y, modifier.Z, 20, 0, 128);		
			modifier.ID = Value;
			resultvoxeldata[index] = modifier;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(VoxelModifier_V2))]
	[CanEditMultipleObjects]
	public class VoxelModifier_V2Editor : Editor
	{
	

		private void OnSceneGUI()
		{
			VoxelModifier_V2 myTarget = target as VoxelModifier_V2;

			var e = Event.current;
			if (e.type == EventType.KeyDown)
			{

				if (e.keyCode == VoxelGeneratorSettings.ApplyKey)
				{
					VoxelUndoSystem.CreateManifest();					
					myTarget.ApplyVoxelModifier(myTarget.transform.position);
					e.Use();
					
				}

				if (e.keyCode == VoxelGeneratorSettings.UndoKey)
				{
					VoxelUndoSystem.Undo();
				}

				if (e.keyCode == VoxelGeneratorSettings.RedoKey)
				{
					VoxelUndoSystem.Redo();
				}



			}

			if(e.type == EventType.KeyUp)
			{
				if (e.keyCode == VoxelGeneratorSettings.ApplyKey)
				{
					VoxelUndoSystem.FinishManifest();				
					e.Use();			
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

			GUIStyle normal = new GUIStyle();
			normal.fontStyle = FontStyle.Normal;
			normal.fontSize = 12;
			normal.richText = true;

			VoxelModifier_V2 myTarget = target as VoxelModifier_V2;
		

			DrawDefaultInspector();


			if (!myTarget.SafetyCheck())
			{
				EditorGUILayout.LabelField("<color=red>Errors:</color>", bold);
				EditorGUILayout.TextArea(myTarget.ErrorMessage);
			}

			if (myTarget.ShapeModule)
			{
				EditorGUILayout.LabelField("Voxel Count:" + myTarget.VoxelCount);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Editing Tools:", bold);
			if (GUILayout.Button("Apply at world position"))
			{
				VoxelUndoSystem.CreateManifest();
				myTarget.ApplyVoxelModifier(myTarget.transform.position);

				VoxelUndoSystem.FinishManifest();
			}
			if (!myTarget.GetComponent<VoxelModifier_V2_Raycaster>())
			{
				EditorGUILayout.LabelField("Voxel Raycaster is required for editor painting:");
				if (GUILayout.Button("Add raycaster"))
					myTarget.gameObject.AddComponent<VoxelModifier_V2_Raycaster>();
			}


			VoxelGeneratorSettings.DisplaySettingsInfo("Apply Hotkey: " , VoxelGeneratorSettings.ApplyKey.ToString(), normal);
			VoxelGeneratorSettings.DisplaySettingsInfo("Undo Hotkey: ", VoxelGeneratorSettings.UndoKey.ToString(), normal);
			VoxelGeneratorSettings.DisplaySettingsInfo("Redo Hotkey: ", VoxelGeneratorSettings.RedoKey.ToString(), normal);

			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
				serializedObject.ApplyModifiedProperties();
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Add modules:", bold);

			string label = "Add Shape Module...";
			if (myTarget.ShapeModule) label = "Replace Shape Module...";

			var attachments = FraktaliaEditorUtility.GetDerivedTypesForScriptSelection(typeof(VoxelShape_Base), label);
			int selectedattachments = EditorGUILayout.Popup(0, attachments.Item2);
			if (selectedattachments > 0)
			{
				VoxelShape_Base current = myTarget.GetComponent<VoxelShape_Base>();
				if(current == null)
                {
					myTarget.gameObject.AddComponent(attachments.Item1[selectedattachments]);
					myTarget.ShapeModule = myTarget.GetComponent<VoxelShape_Base>();
				}
                else
                {
					SerializedObject so = new SerializedObject(myTarget.ShapeModule);
					SerializedProperty scriptProperty = so.FindProperty("m_Script");
					so.Update();

					var tmpGO = new GameObject("tempOBJ");
					MonoBehaviour inst = (MonoBehaviour)tmpGO.AddComponent(attachments.Item1[selectedattachments]);
					MonoScript yourReplacementScript = MonoScript.FromMonoBehaviour(inst);
					DestroyImmediate(tmpGO);
					scriptProperty.objectReferenceValue = yourReplacementScript;
					so.ApplyModifiedProperties();					
				}

				EditorUtility.SetDirty(target);
			}

		

			label = "Add Targeting Module...";
			if (myTarget.TargetingModule) label = "Replace Targeting Module...";

			attachments = FraktaliaEditorUtility.GetDerivedTypesForScriptSelection(typeof(VoxelModifier_Target), label);
			selectedattachments = EditorGUILayout.Popup(0, attachments.Item2);
			if (selectedattachments > 0)
			{
				VoxelModifier_Target current = myTarget.GetComponent<VoxelModifier_Target>();
				if (current == null)
				{
					myTarget.gameObject.AddComponent(attachments.Item1[selectedattachments]);
					myTarget.TargetingModule = myTarget.GetComponent<VoxelModifier_Target>();
				}
				else
				{
					SerializedObject so = new SerializedObject(myTarget.TargetingModule);
					SerializedProperty scriptProperty = so.FindProperty("m_Script");
					so.Update();

					var tmpGO = new GameObject("tempOBJ");
					MonoBehaviour inst = (MonoBehaviour)tmpGO.AddComponent(attachments.Item1[selectedattachments]);
					MonoScript yourReplacementScript = MonoScript.FromMonoBehaviour(inst);
					DestroyImmediate(tmpGO);
					scriptProperty.objectReferenceValue = yourReplacementScript;
					so.ApplyModifiedProperties();
				}

				EditorUtility.SetDirty(target);
			}

			label = "Add Post process module...";		
			attachments = FraktaliaEditorUtility.GetDerivedTypesForScriptSelection(typeof(VM_PostProcess), label);
			selectedattachments = EditorGUILayout.Popup(0, attachments.Item2);
			if (selectedattachments > 0)
			{


				myTarget.gameObject.AddComponent(attachments.Item1[selectedattachments]);
				myTarget.PostProcessModule = new List<VM_PostProcess>(myTarget.GetComponents<VM_PostProcess>());
				

				EditorUtility.SetDirty(target);
			}

		}

		
	}

	

#endif
}
