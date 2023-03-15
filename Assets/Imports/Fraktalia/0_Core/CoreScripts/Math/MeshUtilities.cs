using UnityEngine;
using System.Collections;
using Unity.Collections;

namespace Fraktalia.Core.Math
{

    public static class MeshUtilities
    {



		public static Mesh GetNormalizedMesh(Mesh source, bool keepAspect)
		{
			Mesh output = new Mesh();
			source.RecalculateBounds();
			Vector3[] vertices = source.vertices;

			Bounds boundary = source.bounds;

			Vector3 size = boundary.size;
			if(keepAspect)
			{
				size = Vector3.one * Mathf.Max(size.x, size.y, size.z);
			}

			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vpos = vertices[i];
				vpos -= source.bounds.center;
				vpos.x /= size.x;
				vpos.y /= size.y;
				vpos.z /= size.z;
				vertices[i] = vpos;
			}

			output.vertices = vertices;
			output.normals = source.normals;
			output.triangles = source.triangles;
			output.uv = source.uv;

			output.RecalculateBounds();
			output.RecalculateNormals();
			output.RecalculateTangents();

			return output;
		}

		public static Mesh ApplyMatrixToMesh(Mesh source, Matrix4x4 matrix)
		{
			Mesh output = new Mesh();
			source.RecalculateBounds();
			Vector3[] vertices = source.vertices;		
			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vpos = vertices[i];
				vpos = matrix.MultiplyPoint3x4(vpos);	
				vertices[i] = vpos;
			}

			output.vertices = vertices;
			output.normals = source.normals;
			output.triangles = source.triangles;
			output.uv = source.uv;

			output.RecalculateBounds();
			output.RecalculateNormals();
			output.RecalculateTangents();

			return output;
		}

		public static void ScaleMesh(Mesh source, Vector3 scale)
		{					
			Vector3[] vertices = source.vertices;
			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vpos = vertices[i];
				vpos.Scale(scale);
				vertices[i] = vpos;
			}

			source.vertices = vertices;
			source.RecalculateBounds();
			source.RecalculateNormals();
			source.RecalculateTangents();
		}

		public static void TranslateMesh(Mesh source, Vector3 translate)
		{
			Vector3[] vertices = source.vertices;
			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vpos = vertices[i] + translate;	
				vertices[i] = vpos;
			}

			source.vertices = vertices;
			source.RecalculateBounds();
			source.RecalculateNormals();
			source.RecalculateTangents();
		}
	}


}
