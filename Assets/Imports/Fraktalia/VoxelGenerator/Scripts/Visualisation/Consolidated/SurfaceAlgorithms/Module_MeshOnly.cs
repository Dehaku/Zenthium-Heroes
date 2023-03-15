using System.Collections;
using UnityEngine;
using Unity.Collections;
using Fraktalia.Core.Math;
using Fraktalia.Core.Collections;


#if UNITY_EDITOR
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
    [System.Serializable]
	public unsafe class Module_MeshOnly : HullGenerationModule
	{
		[Header("Target Mesh Settings")]
		public Mesh MeshSource;
		public Vector3 MeshPosition;
		public Vector3 MeshRotationEuler;
		public Vector3 MeshScale;
		public bool BestFit;


		private bool isValid;

		[ReadOnly]
		public FNativeList<Vector3> mesh_verticeArray;
		[ReadOnly]
		public FNativeList<Vector3> mesh_normalArray;
		[ReadOnly]
		public FNativeList<int> mesh_triangleArray;
		[ReadOnly]
		public FNativeList<Vector4> mesh_tangentsArray;
		[ReadOnly]
		public FNativeList<Color> mesh_colorArray;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uvArray;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uv3Array;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uv4Array;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uv5Array;
		[ReadOnly]
		public FNativeList<Vector2> mesh_uv6Array;

		protected override void initializeModule()
		{
			if (MeshSource == null) return;

			Vector3 pos = new Vector3();
			Vector3 scale = new Vector3();

			if (BestFit)
			{
				pos = Vector3.one * HullGenerator.engine.RootSize / 2;
				scale = Vector3.one * HullGenerator.engine.RootSize;
			}
			else
			{
				pos = MeshPosition;
				scale = MeshScale;
			}
			Mesh mesh_normalized = MeshUtilities.GetNormalizedMesh(MeshSource, true);
			MeshUtilities.ScaleMesh(mesh_normalized, scale);
			MeshUtilities.TranslateMesh(mesh_normalized, pos);

			VoxelMath.CreateNativeCrystalInformation(mesh_normalized, ref mesh_verticeArray, ref mesh_triangleArray, ref mesh_uvArray,
			ref mesh_uv3Array, ref mesh_uv4Array, ref mesh_uv5Array, ref mesh_uv6Array,
			ref mesh_normalArray, ref mesh_tangentsArray, ref mesh_colorArray);
			GameObject.DestroyImmediate(mesh_normalized);

			isValid = true;
		}

		public override IEnumerator beginCalculationasync(float cellSize, float voxelSize)
		{
			if (!isValid) yield return null;
			voxelSize = voxelSize * Mathf.Pow(2, HullGenerator.TargetLOD);
			int lodwidth = HullGenerator.LODSize[HullGenerator.TargetLOD];

		
			for (int coreindex = 0; coreindex < HullGenerator.activeCores; coreindex++)
			{
				int cellIndex = HullGenerator.activeCells[coreindex];
				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.GenerateGeometry) continue;

				NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];
			
			}

			for (int coreindex = 0; coreindex < HullGenerator.activeCores; coreindex++)
			{
				int cellIndex = HullGenerator.activeCells[coreindex];
				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.GenerateGeometry) continue;


				if (cellIndex != 0) continue;

				NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];
				data.verticeArray_original.AddRange(mesh_verticeArray);
				data.normalArray_original.AddRange(mesh_normalArray);
				data.uvArray.AddRange(mesh_uvArray);
				data.triangleArray_original.AddRange(mesh_triangleArray);

			}

			yield return null;
		}

		public override void CleanUp()
		{			
			if (mesh_verticeArray.IsCreated) mesh_verticeArray.Dispose();
			if (mesh_triangleArray.IsCreated) mesh_triangleArray.Dispose();
			if (mesh_uvArray.IsCreated) mesh_uvArray.Dispose();
			if (mesh_normalArray.IsCreated) mesh_normalArray.Dispose();
			if (mesh_tangentsArray.IsCreated) mesh_tangentsArray.Dispose();
			if (mesh_colorArray.IsCreated) mesh_colorArray.Dispose();
			if (mesh_uv3Array.IsCreated) mesh_uv3Array.Dispose();
			if (mesh_uv4Array.IsCreated) mesh_uv4Array.Dispose();
			if (mesh_uv5Array.IsCreated) mesh_uv5Array.Dispose();
			if (mesh_uv6Array.IsCreated) mesh_uv6Array.Dispose();
		}

        public override float GetChecksum()
        {
			if (!MeshSource) return 0;

            return base.GetChecksum() + MeshPosition.sqrMagnitude + MeshScale.sqrMagnitude + (BestFit ? 0:1) +
				MeshRotationEuler.sqrMagnitude + MeshSource.vertexCount + MeshSource.bounds.size.sqrMagnitude;
		}
    }
}