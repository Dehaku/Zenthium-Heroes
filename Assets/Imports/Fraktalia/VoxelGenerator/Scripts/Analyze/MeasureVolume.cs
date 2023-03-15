using UnityEngine;
using Fraktalia.Core.FraktaliaAttributes;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public class MeasureVolume : ProceduralVoxelModifier
	{
		[Tooltip("Voxel will be completely solid if distance between voxel position and nearest collision contact point is smaller. Ideal value = 0.001f")]
		public float tolerance = 0.001f;

		[Tooltip("If Voxel Position is not touching or inside the collider, fallof is applied to smooth the surface. Large values make the result blocky, Smaller values increases the smoothness. " +
			"Ideal value = 10000, 2000 = Very Smooth, 20000 = Very sharp-blocky")]
		public float falloffdist = 2000;

		[Tooltip("Final Multiplier of the modifier. Negative values would inverse the result")]
		public float finalMultiplier = 1;

		public Collider[] colliders;

		[NonSerialized]
		public int[] Histogramm;
		[NonSerialized]
		public float[] Histogramm_VOLUME;


		public override void EvaluateVoxelInfo(Vector3 start, Vector3 end)
		{
			Histogramm = new int[256];
			Histogramm_VOLUME = new float[256];

			Vector3 voxelPosition;
			for (voxelPosition.x = start.x; voxelPosition.x <= end.x; voxelPosition.x += voxelsize)
			{
				for (voxelPosition.y = start.y; voxelPosition.y <= end.y; voxelPosition.y += voxelsize)
				{
					for (voxelPosition.z = start.z; voxelPosition.z <= end.z; voxelPosition.z += voxelsize)
					{
						Vector3 localPosition = voxelPosition + Vector3.one * halfvoxelsize;
						Vector3 worldPos = targetgenerator_localtoworldmatrix.MultiplyPoint3x4(localPosition);
						
						if (IsPointWithinCollider(worldPos) == 255)
						{
							int ID = TargetGenerator.Data[TargetDimension]._PeekVoxelId(voxelPosition.x, voxelPosition.y, voxelPosition.z, (byte)Depth, 0.0001f, -1);
							if(ID >= 0)
							{
								Histogramm[ID]++;
							}

						}				
					}
				}
			}

			float size = TargetGenerator.GetVoxelSize(Depth);
			for (int i = 0; i < 256; i++)
			{
				float volume = size * size * size * ((float)Histogramm[i]);
			
				Histogramm_VOLUME[i] = volume;
			}

			

			


		}

		public override Bounds CalculateBounds()
		{
			colliders = GetComponentsInChildren<Collider>();

			Bounds bound = new Bounds();
			bound.max = new Vector3();
			bound.min = new Vector3();

			for (int i = 0; i < colliders.Length; i++)
			{
				Vector3 max = colliders[i].bounds.max;
				Vector3 min = colliders[i].bounds.min;

				if (i == 0)
				{
					bound.max = max;
					bound.min = min;
				}
				else
				{
					bound.max = Vector3.Max(bound.max, max);
					bound.min = Vector3.Min(bound.min, min);
				}
			}


			bound.min = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(bound.min);
			bound.max = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(bound.max);
			bound.size = bound.size * 1.1f;

			return bound;
		}

		public byte IsPointWithinCollider(Vector3 point)
		{

			float dist2 = float.MaxValue;

			for (int i = 0; i < colliders.Length; i++)
			{
				dist2 = Mathf.Min(dist2, (colliders[i].ClosestPoint(point) - point).sqrMagnitude);
			}

			float toleranceinvariant = tolerance * (blocksize / 40);

			if (dist2 < toleranceinvariant * toleranceinvariant)
			{
				return 255;
			}
			else
			{
				float rest = dist2 - voxelsize * toleranceinvariant * toleranceinvariant;

				int value = 255 - (int)(rest * (falloffdist / (blocksize / 40)));

				return (byte)Mathf.Clamp(value, 0, 255);
			}



		}

		public override bool IsSave()
		{


			bool issave = true;


			Bounds bound = CalculateBounds();
			voxelsize = TargetGenerator.GetVoxelSize(Depth);
			int voxellength_x = (int)(bound.size.x / voxelsize);
			int voxellength_y = (int)(bound.size.y / voxelsize);
			int voxellength_z = (int)(bound.size.z / voxelsize);
			int voxels = voxellength_x * voxellength_y * voxellength_z;

			if (voxels > 500000000 || voxels < 0)
			{
				ErrorMessage = "You do not want to freeze Unity";



				issave = false;
			}


			MeshCollider[] meshcolliders = GetComponentsInChildren<MeshCollider>();
			for (int i = 0; i < meshcolliders.Length; i++)
			{
				if (!meshcolliders[i].convex)
				{
					ErrorMessage = "Mesh Collider " + meshcolliders[i].name + " is not set convex. Concave is not supported";
					issave = false;
				}
			}

			return issave;
		}

	}


#if UNITY_EDITOR

	[CustomEditor(typeof(MeasureVolume))]
	[CanEditMultipleObjects]
	public class MeasureVolumeEditor : Editor
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

			DrawDefaultInspector();

			MeasureVolume myTarget = target as MeasureVolume;
			EditorGUILayout.Space();
			if (GUILayout.Button("Analyze"))
			{
				myTarget.ApplyProceduralModifier();

			}

			if (myTarget.Histogramm != null)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("ID Histogramm:");
				for (int i = 0; i < myTarget.Histogramm.Length; i++)
				{
					EditorGUILayout.LabelField("Voxel ID:" + i + " Amount:" + myTarget.Histogramm[i]);
				}
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Volumes:");
				for (int i = 0; i < myTarget.Histogramm.Length; i++)
				{
					EditorGUILayout.LabelField("Voxel ID:" + i + " Volume:" + myTarget.Histogramm_VOLUME[i]);
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
