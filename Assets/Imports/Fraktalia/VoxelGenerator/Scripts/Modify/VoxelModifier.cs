using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fraktalia.VoxelGen.Modify.Positioning;
using UnityEngine.XR;
using UnityEngine.UIElements;
using Fraktalia.Core.FraktaliaAttributes;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.VoxelGen.Modify
{
	[System.Serializable]
	public class VoxelEditorTools
	{
		[Header("Editor Mode: Left Click")]
		[Range(0, 255)]

		public int IDLeft = 100;

		[EnumButtons]
		public VoxelModifyMode ModeLeft = VoxelModifyMode.Additive;

		[Header("Editor Mode: Right Click")]
		[Range(0, 255)]

		public int IDRight = 100;

		[EnumButtons]
		public VoxelModifyMode ModeRight = VoxelModifyMode.Subtractive;

		[Header("Editor Mode: Middle Click")]
		[Range(0, 255)]
		public int IDMiddle = 100;

		[EnumButtons]
		public VoxelModifyMode ModeMiddle = VoxelModifyMode.Smooth;

		[Space]
		public int EstimatedModifications;

	}

	public class VoxelModifier : MonoBehaviour
	{
		[BeginInfo("VOXELMODIFIER")]
		[InfoTitle("Voxel Modifier", "This is the main script to modify voxels. When this game object is selected, you can paint and modify voxels in the editor by holding CTRL/STRG button " +
		"and Left/Right/Middle mouse click. You can define the target dimension when the voxel generator has more than one dimension.\n\n" +
		"The main parameter is the depth value which defines the size of the voxel being modified. The Estimated Modifications shows you how many voxels will be modified when applied. " +
		"There are various shapes possible such as sphere and box. Parameters such as Radius or Size define the properties of the shape.", "VOXELMODIFIER")]
		[InfoSection1("Targeting Settings:", "The voxel modifier is able to modify multiple voxel generators at once. " +
		"Voxel Generators defined in the Always Modify list are always targeted when applied" +
		"It is possible to find the voxel generator in a dynamic fashion using sphere casts at the target locationg. The fetched generators can be filtered using the whitelist. " +
		"The Max Changes parameter limits the amount of generators fetched and modified at the same time.", "VOXELMODIFIER")]
		[InfoSection2("Painting Modes:", "The editor painting has 2 modes which is 2D and 3D painting. 3D Painting uses raycast on generic Unity Colliders while 2D Painting uses a 2D plane only. " +
		"The modifying modes Set, Additive and Subtractive use the dedicated ID value. Smooth smoothes the region by applying a average filter to the region. " +
		"It is possible to find the voxel generator in a dynamic fashion using sphere casts at the target locationg. The fetched generators can be filtered using the whitelist. " +
		"The Max Changes parameter limits the amount of generators fetched and modified at the same time.", "VOXELMODIFIER")]


		[InfoText("Voxel Modifier", "VOXELMODIFIER")]
		[InfoVideo("https://www.youtube.com/watch?v=CiRZoxXJ9ns&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=5&t=2s", false, "VOXELMODIFIER")]
		[InfoVideo2("https://www.youtube.com/watch?v=7ZfE6UaEqy4&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=6", "How to use ingame", "VOXELMODIFIER")]
		[Range(0, 5)]
		public int TargetDimension;
		[Range(1, NativeVoxelTree.MaxDepth)]
		[Tooltip("Default Depth when ModifyAtPos is called")]
		public int Depth = 7;
		[Range(0,255)]
		[Tooltip("Default ID when ModifyAtPos is called")]
		public int ID = 2;
		public float Radius = 1f;


		public List<VoxelModifyTarget> AdditionalTargets = new List<VoxelModifyTarget>();

		public Vector3 Radials;
		public Vector3 BoxSize;
		public Vector3 Orientation;

		[Tooltip("Default mode when ModifyAtPos is called")]
		public VoxelModifyMode Mode;
		public VoxelModifyShape Shape;	

		public List<VoxelGenerator> AlwaysModify;
		public bool UseWhiteList;
		public List<VoxelGenerator> WhiteList;
		[Tooltip("When modifying multiple Voxels you want all their layers to be addded here. For example if you have a WATER and a GROUND voxel and need to sculpt them together, add GROUND and WATER here.")]
		public LayerMask SphereCastLayer;
		public int MaxChanges = 3;

		public UnityEvent onModifyVoxel;

		public Vector3 PaintPosition;

		

		#region EDITORTOOLS
		public string ErrorMessage = "";
		public int EstimatedModifications;

		[Tooltip("Individual settings when Left/Right/Middle click is called")]
		public VoxelEditorTools EditorTools;
		[Tooltip("Switch from 3D sculpting to planar sculpting. When no plane is set then a plane parallel to the camera is created automatically.")]
		public bool Paint2D = false;	
		public bool PaintPlaneXY = false;
		public bool PaintPlaneXZ = false;
		public bool PaintPlaneYZ = false;	
		public float PaintPlaneOffset = 0;	
		public float PaintPlaneSize = 2000;
		public Color PaintPlaneColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		#endregion

		


		private List<VoxelGenerator> modifiedgenerators = new List<VoxelGenerator>();

		public VoxelGenerator ReferenceGenerator
		{
			get
			{
				//TOTO INFINITY EXPANSION
				/*
				if (Limitbreaker)
				{
					if (Limitbreaker.RequiredGenerators != null)
					{
						foreach (var item in Limitbreaker.RequiredGenerators)
						{
							return item.Value;
						}	
					}
				}
				else
				*/
				{
					if (AlwaysModify != null)
					{
						for (int i = 0; i < AlwaysModify.Count; i++)
						{
							if (AlwaysModify[i] != null)
							{
								return AlwaysModify[i];
							}
						}
					
					}

					if(UseWhiteList)
					{
						if(WhiteList != null)
						{
							for (int i = 0; i < WhiteList.Count; i++)
							{
								if (WhiteList[i] != null)
								{
									return WhiteList[i];
								}
							}
						}
					}

					if(modifiedgenerators.Count > 0)
					{
						return modifiedgenerators[0];
					}
				}
				
				return null;
			}
		}

		public void ModifyAtPos(Vector3 worldPosition)
		{
			ModifyAtPos(worldPosition, Radius);
		}

		public void ModifyAtPos(Vector3 worldPosition, float radius)
		{
			modifiedgenerators.Clear();
			

			bool voxelsmodified = false;

			//TODO INFINITY EXPANSION
			/*
			if (Limitbreaker)
			{
				if (Limitbreaker.IsInitialized)
				{
					Vector3 extents = Vector3.one * radius;
					if (Shape == VoxelModifyShape.Box || Shape == VoxelModifyShape.RoundedBox)
					{
						extents = BoxSize / 2;
					}
	
					Limitbreaker.WorldPositionToGenerators(worldPosition, extents, extents, modifiedgenerators);				
				}

			}
			else
			*/
			{

				int changes = 0;
				for (int i = 0; i < AlwaysModify.Count; i++)
				{					
					modifiedgenerators.Add(AlwaysModify[i]);
				}


				var foundgenerators = FindGeneratorsBySphereCast(worldPosition, radius, SphereCastLayer);
				
				for (int i = 0; i < foundgenerators.Count; i++)
				{
					VoxelGenerator generator = foundgenerators[i];

					if (!modifiedgenerators.Contains(generator))
					{
						if (!UseWhiteList || WhiteList.Contains(generator))
						{
							modifiedgenerators.Add(generator);													
							changes++;

							if (changes >= MaxChanges) break;
						}
					}


				}
			}


			for (int i = 0; i < modifiedgenerators.Count; i++)
			{
				voxelsmodified = true;

				VoxelGenerator target = modifiedgenerators[i];
				if (target == null) continue;
				
				if (Shape == VoxelModifyShape.Sphere)
				{
					if (VoxelUtility.IsSafe(target, radius, Depth))
					{
						VoxelUtility.ModifyVoxelsSphere(target, worldPosition, Quaternion.identity,
									radius, Depth, ID, Mode, TargetDimension);

						for (int y = 0; y < AdditionalTargets.Count; y++)
						{
							VoxelModifyTarget modifytarget = AdditionalTargets[y];
							if(modifytarget.TargetDimension >= 0 && modifytarget.TargetDimension < target.DimensionCount)
							{
								VoxelUtility.ModifyVoxelsSphere(target, worldPosition, Quaternion.identity,
								radius * modifytarget.ShapeMultiplier, modifytarget.Depth, modifytarget.ID, modifytarget.Mode, modifytarget.TargetDimension);
							}
						}
					}
				}
				else if(Shape == VoxelModifyShape.Box)
				{
					if (VoxelUtility.IsSafe(target, BoxSize, Depth))
					{
						VoxelUtility.ModifyVoxelsBox(target, worldPosition, Quaternion.Euler(Orientation), BoxSize, Depth, ID, Mode, TargetDimension);

						for (int y = 0; y < AdditionalTargets.Count; y++)
						{
							VoxelModifyTarget modifytarget = AdditionalTargets[y];
							if (modifytarget.TargetDimension >= 0 && modifytarget.TargetDimension < target.DimensionCount)
							{
								VoxelUtility.ModifyVoxelsBox(target, worldPosition, Quaternion.Euler(Orientation), BoxSize * modifytarget.ShapeMultiplier
									, modifytarget.Depth, modifytarget.ID, modifytarget.Mode, modifytarget.TargetDimension);
							}
						}
					}
				}
				else if (Shape == VoxelModifyShape.RoundedBox)
				{
					if (VoxelUtility.IsSafe(target, BoxSize, Depth))
					{
					
						VoxelUtility.ModifyVoxels(target, worldPosition, Quaternion.Euler(Orientation), BoxSize,
							new Vector4(BoxSize.x - Radials.x, BoxSize.y - Radials.y, BoxSize.z - Radials.z, radius), Depth, ID, Mode, TargetDimension);

						for (int y = 0; y < AdditionalTargets.Count; y++)
						{
							VoxelModifyTarget modifytarget = AdditionalTargets[y];
							if (modifytarget.TargetDimension >= 0 && modifytarget.TargetDimension < target.DimensionCount)
							{
								VoxelUtility.ModifyVoxels(target, worldPosition, Quaternion.Euler(Orientation), BoxSize * modifytarget.ShapeMultiplier,
									new Vector4(BoxSize.x - Radials.x, BoxSize.y - Radials.y, BoxSize.z - Radials.z, radius) * modifytarget.ShapeMultiplier, modifytarget.Depth, modifytarget.ID, modifytarget.Mode, modifytarget.TargetDimension);

							}
						}
					}
				}
				else if (Shape == VoxelModifyShape.Single)
				{

					VoxelUtility.ModifySingleVoxel(target, worldPosition, Depth, ID, Mode, TargetDimension);
					for (int y = 0; y < AdditionalTargets.Count; y++)
					{
						VoxelModifyTarget modifytarget = AdditionalTargets[y];
						if (modifytarget.TargetDimension >= 0 && modifytarget.TargetDimension < target.DimensionCount)
						{
							VoxelUtility.ModifySingleVoxel(target, worldPosition, modifytarget.Depth, modifytarget.ID, modifytarget.Mode, modifytarget.TargetDimension);
						}
					}
				}
			}


#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				for (int i = 0; i < modifiedgenerators.Count; i++)
				{
					if (modifiedgenerators[i] != null)
					{
						EditorUtility.SetDirty(modifiedgenerators[i]);
					}
				}
			}
#endif
			if (voxelsmodified) onModifyVoxel.Invoke();


		}

		public List<VoxelGenerator> FindGeneratorsBySphereCast(Vector3 worldPosition, float radius, LayerMask mask)
		{
			Collider[] impact = Physics.OverlapSphere(worldPosition, radius, mask);

			List<VoxelGenerator> result = new List<VoxelGenerator>();
			for (int i = 0; i < impact.Length; i++)
			{
				VoxelGenerator generator = impact[i].GetComponentInParent<VoxelGenerator>();
				if (generator)
				{
					result.Add(generator);
				}
			}
			return result;
		}
		public VoxelGenerator FindReferenceGenerator(Vector3 worldPosition)
		{
			if(ReferenceGenerator)
			{
				return ReferenceGenerator;
			}
			else
			{
				var generators = FindGeneratorsBySphereCast(worldPosition, Radius, SphereCastLayer);
				if(generators.Count > 0)
				{
					return generators[0];
				}
			}

			return null;
		}

		public void ApplyPositioning(VoxelModifyPosition positioning)
		{
			Vector3 point = transform.position + positioning.Calculate();
			if (!positioning.NoTargetFound)
			{
				ModifyAtPos(point, positioning.ModifyRadius);
			}
		}



		public void SetModificationMode(VoxelModifyMode mode)
		{
			Mode = mode;
		}

		public void SetModificationMode(int mode)
		{
			Mode = (VoxelModifyMode)mode;
		}

		public void SetRadius(float radius)
		{
			Radius = radius;
		}

		public void SetID(int id)
		{
			ID = id;
		}

		public void SetID(float id)
		{
			ID = (int)id;
		}

		public void SetDimension(float dimension)
		{
			TargetDimension = (int)dimension;
		}

		public void LeftClick(Vector3 center)
		{
			VoxelModifyMode oldMode = Mode;			
			Mode = EditorTools.ModeLeft;

			int oldID = ID;
			ID = EditorTools.IDLeft;

			ModifyAtPos(center);

			ID = oldID;
			Mode = oldMode;
		}

		public void RightClick(Vector3 center)
		{
			VoxelModifyMode oldMode = Mode;
			Mode = EditorTools.ModeRight;

			int oldID = ID;
			ID = EditorTools.IDRight;

			ModifyAtPos(center);

			ID = oldID;
			Mode = oldMode;
		}

		public void MiddleClick(Vector3 center)
		{
			VoxelModifyMode oldMode = Mode;
			Mode = EditorTools.ModeMiddle;

			int oldID = ID;
			ID = EditorTools.IDMiddle;

			ModifyAtPos(center);

			ID = oldID;
			Mode = oldMode;
		}


		public bool SafetyCheck()
		{
			ErrorMessage = "";
			bool isSave = true;


			int highestdepth = Depth;
			float highestmultiplier = 1;
			for (int i = 0; i < AdditionalTargets.Count; i++)
			{
				highestdepth = Mathf.Max(highestdepth, AdditionalTargets[i].Depth);
				highestmultiplier = Mathf.Max(highestmultiplier, AdditionalTargets[i].ShapeMultiplier);
			}



			if (!ReferenceGenerator)
			{
				ErrorMessage += "> No Generator to check safety found. \n\n";
				isSave = false;

				EstimatedModifications = 0;
				return isSave;
			}
			else
			{
				if(Shape == VoxelModifyShape.Sphere)
				{
					EstimatedModifications = VoxelUtility.EvaluateModificationCount(ReferenceGenerator, Radius, Depth);
				}
				else
				{
					EstimatedModifications = VoxelUtility.EvaluateModificationCount(ReferenceGenerator, BoxSize, Depth);
				}
			
			}

			EstimatedModifications = (int)(EstimatedModifications * highestmultiplier);

			if (EstimatedModifications > 100000 || EstimatedModifications < -5)
			{
				ErrorMessage += "> This setting would modify more than 100000 voxels. This is not safe\n\n";
				isSave = false;
			}

			if (EstimatedModifications == -1)
			{
				ErrorMessage += "> The Voxel Engine is not initialized\n\n";
				isSave = false;
			}

			if (EstimatedModifications == -2)
			{
				ErrorMessage += "> Target Depth is larger than the Maximum Depth\n\n";
				isSave = false;
			}

			return isSave;
		}

		public Vector3 WorldPositionToVoxelPoint(Vector3 worldPosition, Vector3 normaldirection, float normalOffset)
		{
			VoxelGenerator reference = FindReferenceGenerator(worldPosition);
			if (reference == null) return worldPosition;


			float size = reference.GetVoxelSize(Depth);
			Vector3 newposition = worldPosition + normaldirection * size * normalOffset;

#if UNITY_EDITOR
			Handles.SphereHandleCap(0, newposition, Quaternion.identity, size * 0.1f, EventType.Repaint);
#endif


			Vector3 center = reference.transform.InverseTransformPoint(newposition);

			if (center.x >= 0)
			{
				center.x -= Mathf.Abs(center.x) % size;
			}
			else
			{
				center.x -= size - (Mathf.Abs(center.x) % size);
			}

			if (center.y >= 0)
			{
				center.y -= Mathf.Abs(center.y) % size;
			}
			else
			{
				center.y -= size - (Mathf.Abs(center.y) % size);
			}

			if (center.z >= 0)
			{
				center.z -= Mathf.Abs(center.z) % size;
			}
			else
			{
				center.z -= size - (Mathf.Abs(center.z) % size);
			}


			center.x += size / 2;
			center.y += size / 2;
			center.z += size / 2;


			return reference.transform.TransformPoint(center);
		}

		public List<VoxelGenerator> GetAssignedGenerators()
		{
			List<VoxelGenerator> output = new List<VoxelGenerator>();
			if(AlwaysModify != null)
			{
				output.AddRange(AlwaysModify);
			}

			if(UseWhiteList)
			{
				if(WhiteList != null)
				{
					output.AddRange(WhiteList);
				}
			}

			return output;
		}
	}

	[System.Serializable]
	public struct VoxelModifyTarget
	{
		public int TargetDimension;
		public int Depth;

		[Range(0, 255)]
		public int ID;
		public VoxelModifyMode Mode;

		public float ShapeMultiplier;

	}


#if UNITY_EDITOR
	[CustomEditor(typeof(VoxelModifier))]
	[CanEditMultipleObjects]
	public class VoxelModifierEditor : Editor
	{
		int activePaintTime;

		void Preview(Vector3 position, VoxelModifier mytarget, RaycastHit hit)
		{
			if (mytarget.Shape == VoxelModifyShape.Sphere)
			{
				Handles.SphereHandleCap(0, position, Quaternion.identity, mytarget.Radius * 2 * mytarget.transform.localScale.x, EventType.Repaint);
			}
			else if(mytarget.Shape == VoxelModifyShape.Single)
			{
				Vector3 newposition = mytarget.WorldPositionToVoxelPoint(position, hit.normal, -0.5f);
				Matrix4x4 old = Handles.matrix;

				VoxelGenerator reference = mytarget.FindReferenceGenerator(position);
				if (reference)
				{			
					Handles.matrix = Matrix4x4.TRS(newposition, Quaternion.identity, Vector3.one * reference.GetVoxelSize(mytarget.Depth));
					Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1, EventType.Repaint);
					Handles.matrix = old;
				}
			}
			else
			{
				Matrix4x4 old = Handles.matrix;
				Handles.matrix = Matrix4x4.TRS(position, Quaternion.Euler(mytarget.Orientation), Vector3.one);
				Handles.DrawWireCube(Vector3.zero, mytarget.BoxSize);
				

				if(mytarget.Shape == VoxelModifyShape.RoundedBox)
				{

					Vector3 size = mytarget.BoxSize;
					
					Handles.color = Color.white;				

					Handles.DrawWireDisc(Vector3.zero, Vector3.right, size.x - mytarget.Radials.x);
					Handles.DrawWireDisc(Vector3.zero, Vector3.forward, size.z - mytarget.Radials.z);
					Handles.DrawWireDisc(Vector3.zero, Vector3.up, size.y - mytarget.Radials.y);

					Handles.color = new Color32(0,0,0,128);
					Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, mytarget.Radius, EventType.Repaint);
				}

				Handles.matrix = old;
			}
		}


		void OnSceneGUI()
		{
			if (Application.isPlaying) return;
			VoxelModifier mytarget = target as VoxelModifier;

			Event e = Event.current;
			if (!e.control)
			{
				return;
			}
			if (e.alt) return;
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

			RaycastHit hit = new RaycastHit();

			Vector2 guiPosition = Event.current.mousePosition;
			Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

			Plane surface = new Plane();
			Vector3 normal = Vector3.zero;
			bool hastarget = false;
			if (mytarget.Paint2D)
			{
				Vector3 direction = new Vector3();
				if (mytarget.PaintPlaneXY) direction.z = 1;
				if (mytarget.PaintPlaneXZ) direction.y = 1;
				if (mytarget.PaintPlaneYZ) direction.x = 1;

				direction = mytarget.transform.localToWorldMatrix.MultiplyVector(direction);
				surface = new Plane(direction, mytarget.transform.position + direction * mytarget.PaintPlaneOffset);
				Handles.DrawWireDisc(mytarget.transform.position + direction * mytarget.PaintPlaneOffset, direction, mytarget.PaintPlaneSize);
				Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
				Handles.color = mytarget.PaintPlaneColor;
				Handles.DrawSolidDisc(mytarget.transform.position + direction * mytarget.PaintPlaneOffset, direction, mytarget.PaintPlaneSize);

				float enter = 0;
				if (surface.Raycast(ray, out enter))
				{					
					mytarget.PaintPosition = ray.GetPoint(enter);
					hastarget = true;			
				}

				normal = surface.normal;
			}
			else
			{
				if (Physics.Raycast(ray, out hit, 2000))
				{				
					mytarget.PaintPosition = hit.point;
					hastarget = true;
					normal = hit.normal;
				}
			}

			


			if (!hastarget) return;



			Handles.DrawWireDisc(mytarget.PaintPosition, normal, mytarget.Radius, 2);
			Handles.DrawWireDisc(mytarget.PaintPosition, normal, mytarget.Radius* 0.33f, 2);
			Handles.DrawWireDisc(mytarget.PaintPosition, normal, mytarget.Radius* 0.66f, 2);

			Handles.color = new Color32(0, 100, 100, 100);
			Preview(mytarget.PaintPosition, mytarget, hit);
			Handles.color = new Color32(0, 150, 255, 255);

			if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 1)
			{
				if (mytarget.Shape == VoxelModifyShape.Single)
				{
					mytarget.PaintPosition = mytarget.WorldPositionToVoxelPoint(mytarget.PaintPosition, hit.normal, 0.5f);
				}

				mytarget.RightClick(mytarget.PaintPosition);
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			}

			if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0)
			{
				if (mytarget.Shape == VoxelModifyShape.Single)
				{
					mytarget.PaintPosition = mytarget.WorldPositionToVoxelPoint(mytarget.PaintPosition, hit.normal, -0.5f);
				}

				mytarget.LeftClick(mytarget.PaintPosition);
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

			}

			if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 2)
			{
				mytarget.MiddleClick(mytarget.PaintPosition);
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

			}

			SceneView.RepaintAll();

		
			


			if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0)
			{
				e.Use();
			}

			if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 1)
			{
				e.Use();
			}

			if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 2)
			{
				e.Use();
			}
		}


		public override void OnInspectorGUI()
		{



			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 14;
			bold.richText = true;

			VoxelModifier myTarget = target as VoxelModifier;

		
			EditorGUILayout.PropertyField(serializedObject.FindProperty("TargetDimension"));	
			EditorGUILayout.PropertyField(serializedObject.FindProperty("Depth"));		
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ID"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("Mode"));

			EditorGUILayout.PropertyField(serializedObject.FindProperty("AdditionalTargets"), true);

			EditorGUILayout.LabelField("Estimated Modifications:" + myTarget.EstimatedModifications);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=green>Shape Settings:</color>", bold);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("Shape"));
			
			switch (myTarget.Shape)
			{
				case VoxelModifyShape.Sphere:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("Radius"));
					break;
				case VoxelModifyShape.Box:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("BoxSize"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("Orientation"));
					break;
				case VoxelModifyShape.RoundedBox:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("BoxSize"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("Orientation"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("Radials"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("Radius"));
					break;
				case VoxelModifyShape.Single:

					break;
				default:
					break;
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=green>Targeting Settings:</color>", bold);
			//EditorGUILayout.PropertyField(serializedObject.FindProperty("Limitbreaker"));
			
				EditorGUILayout.PropertyField(serializedObject.FindProperty("AlwaysModify"), true);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("UseWhiteList"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("WhiteList"), true);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("SphereCastLayer"), true);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxChanges"), true);

			

			EditorGUILayout.Space();



			EditorGUILayout.PropertyField(serializedObject.FindProperty("Paint2D"));
			if (myTarget.Paint2D)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("PaintPlaneXY"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("PaintPlaneXZ"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("PaintPlaneYZ"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("PaintPlaneOffset"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("PaintPlaneSize"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("PaintPlaneColor"));
			}

			EditorGUILayout.Space();


			EditorGUILayout.PropertyField(serializedObject.FindProperty("EditorTools"), true);
		

			VoxelModifyPosition pos = myTarget.GetComponent<VoxelModifyPosition>();
			if(pos)
			{
				if(GUILayout.Button("Apply Positioning"))
				{
					myTarget.ApplyPositioning(pos);
				}

				if (GUILayout.Button("Apply Positioning * 10"))
				{
					for (int i = 0; i < 10; i++)
					{
						myTarget.ApplyPositioning(pos);
					}
					
				}

				if (GUILayout.Button("Apply Positioning * 100"))
				{
					for (int i = 0; i < 100; i++)
					{
						myTarget.ApplyPositioning(pos);
					}
				}

				if (GUILayout.Button("Apply Positioning * 1000"))
				{
					for (int i = 0; i < 1000; i++)
					{
						myTarget.ApplyPositioning(pos);
					}
				}
			}


			if (!myTarget.SafetyCheck())
			{
				EditorGUILayout.LabelField("<color=red>Errors:</color>", bold);
				EditorGUILayout.TextArea(myTarget.ErrorMessage);
			}	

			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
				serializedObject.ApplyModifiedProperties();
			}
		}




	}
#endif

}
