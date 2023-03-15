using System.Collections;
using UnityEngine;
using Unity.Jobs;


#if UNITY_EDITOR
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
    [System.Serializable]
	public unsafe class SurfaceModifier_Inflate : SurfaceModifier
	{
		public float Inflation;

		public override IEnumerator beginCalculationasync(float cellSize, float voxelSize)
		{
			voxelSize = voxelSize * Mathf.Pow(2, HullGenerator.TargetLOD);
			int lodwidth = HullGenerator.LODSize[HullGenerator.TargetLOD];

			JobHandle handle = new JobHandle();

			for (int coreindex = 0; coreindex < HullGenerator.activeCores; coreindex++)
			{
				int cellIndex = HullGenerator.activeCells[coreindex];
				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.RequiresNonGeometryData) continue;

				NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];

				InflateSurfaceJob job = new InflateSurfaceJob();
				job.Inflation = Inflation;
				job.normals = data.normalArray;
				job.vertices = data.verticeArray;
				handle = job.Schedule(data.normalArray.Length, data.normalArray.Length / SystemInfo.processorCount, handle);
			}

			while (!handle.IsCompleted)
			{
				if (HullGenerator.synchronitylevel < 0) break;
				yield return new YieldInstruction();
			}

			handle.Complete();

			yield return null;
		}

        internal override ModularUniformVisualHull.WorkType EvaluateWorkType(int dimension)
        {
			return ModularUniformVisualHull.WorkType.Nothing;
        }

        internal override void GetFractionalGeoChecksum(ref ModularUniformVisualHull.FractionalChecksum fractional, SurfaceModifierContainer container)
        {
			fractional.nongeometryChecksum += Inflation + (container.Disabled ? 0: 1);
        }
    }
}