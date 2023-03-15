using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
using UnityEngineInternal;
using Fraktalia.Core.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


namespace Fraktalia.VoxelGen.Visualisation
{
	[ExecuteInEditMode]
	public class BasicSurfaceModifier : MonoBehaviour
	{
		[HideInInspector]
		public List<VoxelPiece> VoxelMeshes = new List<VoxelPiece>();

		[HideInInspector]
		public int Width = 8;
		[HideInInspector]
		public int Cell_Subdivision = 8;
		[HideInInspector]
		public int NumCores = 1;

		public enum HideflagsMode
		{
			Normal,
			DontSave,
			Hidden,
			HiddenDontSave
		}

		public VoxelGenerator engine;
		public HideflagsMode AppliedHideflags = HideflagsMode.HiddenDontSave;
		
		public bool Locked = false;
		public bool LockAfterGeneration = false;
		public bool NoCollision = false;
		public bool ConvexCollider = false;
		public bool Invisible = false;

		[NonSerialized]
		public string ErrorMessage;
		protected int Slots;
		protected Stack<int> SlotsDirty;
		protected Stack<int> SlotsProcessing;

		[SerializeField][HideInInspector]
		private HideflagsMode usedHideflags = HideflagsMode.HiddenDontSave;

		protected static VoxelPiece originalVoxelPiece;
		private void Start()
		{
			engine = GetComponentInParent<VoxelGenerator>();
		}

		private void OnDrawGizmosSelected()
		{
#if UNITY_EDITOR
			if (PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab)
			{
				return;
			}

			if(AppliedHideflags != usedHideflags)
			{
				usedHideflags = AppliedHideflags;
				updatehideflags();
			}


#endif
		
			if (engine == null)
			{
				CleanUp();
				CleanVisualisation();
			}
		}

		public static void RemoveDuplicates(List<BasicSurfaceModifier> details)
		{
			for (int i = 0; i < details.Count; i++)
			{
				if (details[i] == null) continue;
				BasicSurfaceModifier detailtocheck = details[i];
				for (int k = 0; k < details.Count; k++)
				{
					if (k == i) continue;
					if(detailtocheck == details[k])
					{
						details[k] = null;
					}
				}
			}
		}

		public void InitVisualHull(int slots)
		{
			Slots = slots;
			SlotsDirty = new Stack<int>();
			SlotsProcessing = new Stack<int>();
			
			Initialize();			
		}		

		public void SetSlotDirty(int slot)
		{
			SlotsDirty.Push(slot);
		}
	

		public virtual void SetLock(int slot, bool state)
		{
			
		}

		public void LockAllSlots()
		{
			for (int i = 0; i < Slots; i++)
			{
				SetLock(i, true);
			}
		}

		public void UnlockAllSlots()
		{
			for (int i = 0; i < Slots; i++)
			{
				SetLock(i, false);
			}
		}


		public virtual void DefineSurface(VoxelPiece piece ,FNativeList<Vector3> surface_verticeArray, FNativeList<int> surface_triangleArray, FNativeList<Vector3> surface_normalArray, int slot)
		{

		}

		public virtual void PrepareWorks()
		{

		}
		
		public virtual void CompleteWorks()
		{

		}
	
		protected virtual void Initialize()
		{

		}
	
		public virtual void CleanUp()
		{
			
		}

		public virtual void CleanVisualisation()
		{
			MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
			for (int i = 0; i < filters.Length; i++)
			{
				MeshFilter filter = filters[i];
				if (filter)
				{
					DestroyImmediate(filter.sharedMesh);
				}
				DestroyImmediate(filter.gameObject);
			}
		}

		public virtual bool IsSave()
		{
			return true;
		}

		public virtual bool IsCompleted()
        {
			return true;
        }


		internal virtual float GetChecksum()
		{
			return BasicNativeVisualHull.Bools2Int(enabled, Locked, LockAfterGeneration, NoCollision, ConvexCollider, Invisible);
		}

		#region Mesh Piece Functions
		protected void CreateVoxelPieces(int piececount, Material VoxelMaterial)
		{
			VoxelMeshes = new List<VoxelPiece>(piececount);
			VoxelMeshes.AddRange(GetComponentsInChildren<VoxelPiece>());

			if (originalVoxelPiece == null)
				originalVoxelPiece = ((GameObject)Resources.Load("__VOXELPIECE")).GetComponent<VoxelPiece>();


			for (int i = VoxelMeshes.Count; i < piececount; i++)
			{
				VoxelMeshes.Add(CreateVoxelPiece("__VOXELPIECE", VoxelMaterial));
			}
		}

		protected VoxelPiece CreateVoxelPiece(string Name, Material material)
		{
			VoxelPiece newobj = Instantiate(originalVoxelPiece, transform);
			newobj.transform.localRotation = Quaternion.identity;
			newobj.transform.localPosition = new Vector3(0, 0, 0);
			newobj.transform.localScale = Vector3.one;

			newobj.gameObject.isStatic = gameObject.isStatic;
			newobj.gameObject.layer = gameObject.layer;
		
			switch (usedHideflags)
			{
				case HideflagsMode.Normal:
					newobj.gameObject.hideFlags = HideFlags.None;
					break;
				case HideflagsMode.DontSave:
					newobj.gameObject.hideFlags = HideFlags.DontSave;
					break;
				case HideflagsMode.Hidden:
					newobj.gameObject.hideFlags = HideFlags.HideInHierarchy;
					break;
				case HideflagsMode.HiddenDontSave:
					newobj.gameObject.hideFlags = HideFlags.HideAndDontSave;
					break;
				default:
					break;
			}

			VoxelPiece piece = newobj;
			piece.Initialize();			
			piece.meshrenderer.material = material;
			Mesh visualhull = new Mesh();
			visualhull.MarkDynamic();
			visualhull.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			piece.meshfilter.sharedMesh = visualhull;
			piece.voxelMesh = visualhull;

			if(!NoCollision)
			{
				piece.meshcollider.sharedMesh = visualhull;
				piece.meshcollider.convex = ConvexCollider;
			}
			else
			{
				piece.meshcollider.enabled = false;
			}

			if(Invisible)
			{
				piece.meshrenderer.enabled = false;
			}

			return (piece);


		}

		public virtual void HideMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].gameObject.SetActive(false);
			}
		}

		public virtual void ShowMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].gameObject.SetActive(true);
			}
		}

		public void ClearMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].meshfilter.sharedMesh.Clear();
			}
		}

		public void DestroyMeshes()
		{
			if (!engine || !engine.KeepHulls)
			{
				for (int i = 0; i < VoxelMeshes.Count; i++)
				{
					if (VoxelMeshes[i])
					{
						if (VoxelMeshes[i].meshfilter.sharedMesh)
						{
							DestroyImmediate(VoxelMeshes[i].meshfilter.sharedMesh);
						}

						DestroyImmediate(VoxelMeshes[i].gameObject);
					}
					
				}

				VoxelMeshes.Clear();
			}
		}

		#endregion

		protected GameObject CreateEmptyPiece(string Name)
		{
			GameObject newobj = new GameObject(Name);
			newobj.transform.SetParent(transform);
			newobj.transform.localRotation = Quaternion.identity;
			newobj.transform.localPosition = new Vector3(0, 0, 0);
			newobj.transform.localScale = Vector3.one;

			newobj.gameObject.isStatic = gameObject.isStatic;
			newobj.layer = gameObject.layer;

			switch (usedHideflags)
			{
				case HideflagsMode.Normal:
					newobj.hideFlags = HideFlags.None;
					break;
				case HideflagsMode.DontSave:
					newobj.hideFlags = HideFlags.DontSave;
					break;
				case HideflagsMode.Hidden:
					newobj.hideFlags = HideFlags.HideInHierarchy;
					break;
				case HideflagsMode.HiddenDontSave:
					newobj.hideFlags = HideFlags.HideAndDontSave;
					break;
				default:
					break;
			}
			
			return newobj;
		}


		private void updatehideflags()
		{
			VoxelPiece[] pieces = GetComponentsInChildren<VoxelPiece>(true);

			for (int i = 0; i < pieces.Length; i++)
			{
				switch (usedHideflags)
				{
					case HideflagsMode.Normal:
						pieces[i].gameObject.hideFlags = HideFlags.None;
						break;
					case HideflagsMode.DontSave:
						pieces[i].gameObject.hideFlags = HideFlags.DontSave;
						break;
					case HideflagsMode.Hidden:
						pieces[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
						break;
					case HideflagsMode.HiddenDontSave:
						pieces[i].gameObject.hideFlags = HideFlags.HideAndDontSave;
						break;
					default:
						break;
				}	
			}			
		}

		internal virtual void OnDuplicate()
		{

		}

		private void OnDestroy()
		{
			CleanUp();
		}


	}

#if UNITY_EDITOR

	[CustomEditor(typeof(BasicSurfaceModifier), true)]
	[CanEditMultipleObjects]
	public class BasicNativeDetailHullEditor : Editor
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



			BasicSurfaceModifier myTarget = (BasicSurfaceModifier)target;
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
