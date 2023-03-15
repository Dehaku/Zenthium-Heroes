using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using System;
using Unity.Burst;
using Fraktalia.Core.Math;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
    [System.Serializable]
	public unsafe class HullGenerationModule
	{
		[NonSerialized]
		public ModularUniformVisualHull HullGenerator;

		public void Initialize(ModularUniformVisualHull hullgenerator)
		{
			HullGenerator = hullgenerator;
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

		public virtual float GetChecksum() { return 0; }

		public virtual void CleanUp() { }
	}
}