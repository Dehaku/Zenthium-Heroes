using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Fraktalia.Core.Math;
using Fraktalia.Utility;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	public class DestroyBackwardFaces : BasicSurfaceModifier
	{
		[Tooltip("in world space")] public Vector3 Center;
		[Range(0, 1)] public float Threshold;

		public override void DefineSurface(VoxelPiece piece, FNativeList<Vector3> surface_verticeArray, FNativeList<int> surface_triangleArray, FNativeList<Vector3> surface_normalArray, int slot)
		{
			DestroyBackwardFacesJob job;
			job.Center = transform.InverseTransformPoint(Center);
			job.Threshold = Threshold;
			job.vertices = surface_verticeArray;
			job.normals = surface_normalArray;
			job.Schedule(surface_verticeArray.Length / 3, surface_verticeArray.Length / 3 / SystemInfo.processorCount).Complete();
			piece.SetVertices(job.vertices);
		}

		internal override float GetChecksum()
		{
			return base.GetChecksum() + Center.sqrMagnitude + Threshold;
		}
	}
	[BurstCompile]
	public struct DestroyBackwardFacesJob : IJobParallelFor
	{
		public Vector3 Center;
		public float Threshold;
		[NativeDisableParallelForRestriction] public FNativeList<Vector3> vertices;
		[ReadOnly] public FNativeList<Vector3> normals;

		public void Execute(int index)
		{
			var i = index * 3;
			var vertex1 = vertices[i];
			var vertex2 = vertices[i + 1];
			var vertex3 = vertices[i + 2];
			var normal1 = normals[i];
			var normal2 = normals[i + 1];
			var normal3 = normals[i + 2];
			var facenormal = (normal1 + normal2 + normal3).normalized;
			var faceCenter = (vertex1 + vertex2 + vertex3) / 3;
			var faceToCenter = faceCenter - Center;
			if (Vector3.Dot(facenormal.normalized, faceToCenter.normalized) < Threshold)
			{
				vertex1 = Vector3.zero;
				vertex2 = Vector3.zero;
				vertex3 = Vector3.zero;
			}
			vertices[i] = vertex1;
			vertices[i + 1] = vertex2;
			vertices[i + 2] = vertex3;
		}
	}
}