using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Burst;
using UnityEngine.Rendering;
using Fraktalia.Utility;
using Unity.Collections.LowLevel.Unsafe;
using Fraktalia.Core.Math;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
    public enum SurfaceModifierType
    {
		None,
		Inflate,
		MultiTexture,
		CubeUV,
		Normals,
		CalculateTangents,
		VertexColours
    }

	[System.Serializable]
	public class SurfaceModifierContainer
    {
		public string Name = "";
		public SurfaceModifierType ModifierType;
		public bool Disabled;
		private bool isInitilized;

		[SerializeReference]
		public SurfaceModifier Modifier = new SurfaceModifier_Nothing();
	

		public void ConvertToDerivate()
		{
			CleanUp();
			switch (ModifierType)
			{
				case SurfaceModifierType.None:
					Modifier = Modifier as SurfaceModifier_Nothing;
					if (Modifier == null)
						Modifier = new SurfaceModifier_Nothing();
					
					break;
				case SurfaceModifierType.Inflate:
					Modifier = Modifier as SurfaceModifier_Inflate;
					if (Modifier == null)
						Modifier = new SurfaceModifier_Inflate();			
					break;

				case SurfaceModifierType.MultiTexture:
					Modifier = Modifier as SurfaceModifier_MultiTexture;
					if (Modifier == null)
						Modifier = new SurfaceModifier_MultiTexture();
					break;
				case SurfaceModifierType.CubeUV:
					Modifier = Modifier as SurfaceModifier_CubeUV;
					if (Modifier == null)
						Modifier = new SurfaceModifier_CubeUV();
					break;
				case SurfaceModifierType.Normals:
					Modifier = Modifier as SurfaceModifier_Normals;
					if (Modifier == null)
						Modifier = new SurfaceModifier_Normals();
					break;
				case SurfaceModifierType.CalculateTangents:
					Modifier = Modifier as SurfaceModifier_CalculateTangents;
					if (Modifier == null)
						Modifier = new SurfaceModifier_CalculateTangents();
					break;
				case SurfaceModifierType.VertexColours:
					Modifier = Modifier as SurfaceModifier_VertexColor;
					if (Modifier == null)
						Modifier = new SurfaceModifier_VertexColor();
					break;
				default:
					break;
			}
		}

		public static void RemoveDuplicates(List<SurfaceModifierContainer> resultModifier)
		{
			for (int i = 0; i < resultModifier.Count; i++)
			{
				if (resultModifier[i] == null) continue;
				SurfaceModifierContainer detailtocheck = resultModifier[i];
				for (int k = 0; k < resultModifier.Count; k++)
				{
					if (k == i) continue;
					if (object.ReferenceEquals(detailtocheck.Modifier, resultModifier[k].Modifier))
					{
						resultModifier[k] = new SurfaceModifierContainer();
					}
				}
			}
		}

		public void Initialize(ModularUniformVisualHull hullgenerator)
        {
			if (isInitilized) return;
			isInitilized = true;

			if (Modifier != null)
			{
				Modifier.HullGenerator = hullgenerator;
				Modifier.Initialize();
			}
		}

		public void CleanUp()
        {
			isInitilized = false;
			if (Modifier != null) Modifier.CleanUp();
        }

		public float GetChecksum()
        {
			float modifier = 0;
			if (Modifier != null) modifier = Modifier.GetChecksum();

			return (float)ModifierType + modifier;
        }
	}


    [System.Serializable]
	public class SurfaceModifier
	{
		[NonSerialized]
		public ModularUniformVisualHull HullGenerator;
		[NonSerialized]
		public SurfaceModifierContainer Container;

		

		public void Initialize()
		{			
			initializeModule();
		}

		protected virtual void initializeModule() { }

		/// <summary>
		/// Function to start calculate the hull.
		/// </summary>
		/// <param name="jobIndex">Index of the m_JobHandle/calculation being used.</param>
		/// <param name="cellIndex">The cell index which is updated. Each cell is a region defined by SubdivisionPower (example: SP of 2 means 8 cells). Used for mesh assignment</param>
		/// <param name="cellSize">Size of the cell.</param>
		/// <param name="voxelSize">Size of the individual voxel. Is defined by cellSize/Width (example: cS=32, width = 32, voxelSize = 1)</param>
		/// <param name="startX">Start location. Add this to the vertex parameter. Else all results will be in the bottem corner.</param>
		/// <param name="startY">Start location. Add this to the vertex parameter. Else all results will be in the bottem corner.</param>
		/// <param name="startZ">Start location. Add this to the vertex parameter. Else all results will be in the bottem corner.</param>
		public virtual IEnumerator beginCalculationasync(float cellSize, float voxelSize) { yield return null; }

		public virtual float GetChecksum() { 
			return 0; 
		}

		internal virtual ModularUniformVisualHull.WorkType EvaluateWorkType(int dimension)
		{
			return ModularUniformVisualHull.WorkType.Nothing;
		}

        internal virtual void GetFractionalGeoChecksum(ref ModularUniformVisualHull.FractionalChecksum fractional, SurfaceModifierContainer container)
        {
			
        }

		public virtual void CleanUp()
        {

        }
    }
}