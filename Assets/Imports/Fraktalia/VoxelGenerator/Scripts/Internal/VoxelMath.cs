using UnityEngine;
using System.Collections;
using Unity.Collections;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen
{

    public static class VoxelMath
    {

        /// <summary>
        /// Checks, if the distance of 2 Vectors is smaller than a given Lenght 
        /// </summary>
        /// <param name="start">Startpoint</param>
        /// <param name="end">Endpoint</param>
        /// <param name="Lenght">The Value to Compare</param>
        /// <returns></returns>
        public static bool IsInRange(Vector2 start, Vector2 end, float Lenght)
        {
            Vector2 dist = start - end;
            if (dist.sqrMagnitude < Lenght * Lenght) return true;

            return false;

        }

        /// <summary>
        /// Returns position on UnitCircle
        /// </summary>
        /// <param name="Winkel">Angle in degree</param>
        /// <returns></returns>
        public static Vector2 kreisPosition(float Angle)
        {
            while (Angle > 360)
            {
                Angle -= 360;
            }

            Vector2 output = new Vector2();
            output.y = Mathf.Sin(Angle * Mathf.PI / 180);
            output.x = Mathf.Cos(Angle * Mathf.PI / 180);
            return output;
        }

        public static Vector3 spherePosition(float AngleX, float AngleY)
        {
            AngleX *= Mathf.Deg2Rad;
            AngleY *= Mathf.Deg2Rad;

            Vector3 output = new Vector3();
            output.z = Mathf.Cos(AngleX) * Mathf.Sin(AngleY);
            output.x = Mathf.Sin(AngleX) * Mathf.Sin(AngleY);
            output.y = Mathf.Cos(AngleY);

            return output;
        }

        public static Vector3 RandomVector(Vector3 first, Vector3 end)
        {
            Vector3 output = new Vector3();
            output.x = Random.Range(first.x, end.x);
            output.y = Random.Range(first.y, end.y);
            output.z = Random.Range(first.z, end.z);
            return output;
        }

		public static Vector3 CalculateRotatedSize(VoxelGenerator generator, Vector3 center, Vector3 size, Quaternion rotation)
		{
			Vector3 extends_max = size * 0.5f;
			Vector3 extends_min = -extends_max;

			Vector3 extends_corner = rotation * (extends_min + new Vector3(extends_max.x - extends_min.x, 0, 0));		
			Vector3 extends_corner3 = rotation * (extends_min + new Vector3(0, extends_max.y - extends_min.y, 0));		
			Vector3 extends_corner5 = rotation * (extends_min + new Vector3(0, 0, extends_max.z - extends_min.z));
			
			extends_max = rotation * extends_max;
			extends_min = -extends_max;

			if (generator.DebugMode)
			{
				Matrix4x4 matrix = generator.transform.localToWorldMatrix;


				Vector3 extends_corner6 = -extends_corner5;
				Vector3 extends_corner4 = -extends_corner3;
				Vector3 extends_corner2 = -extends_corner;

				Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_max), Color.blue, 5);
				Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_min), Color.blue, 5);
				Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner), Color.blue, 5);
				Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner2), Color.blue, 5);
				Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner3), Color.blue, 5);
				Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner4), Color.blue, 5);
				Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner5), Color.blue, 5);
				Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner6), Color.blue, 5);
			}

			extends_max.x = Mathf.Abs(extends_max.x);
			extends_max.y = Mathf.Abs(extends_max.y);
			extends_max.z = Mathf.Abs(extends_max.z);
			extends_corner.x = Mathf.Abs(extends_corner.x);
			extends_corner.y = Mathf.Abs(extends_corner.y);
			extends_corner.z = Mathf.Abs(extends_corner.z);
			extends_corner3.x = Mathf.Abs(extends_corner3.x);
			extends_corner3.y = Mathf.Abs(extends_corner3.y);
			extends_corner3.z = Mathf.Abs(extends_corner3.z);
			extends_corner5.x = Mathf.Abs(extends_corner5.x);
			extends_corner5.y = Mathf.Abs(extends_corner5.y);
			extends_corner5.z = Mathf.Abs(extends_corner5.z);

			Vector3 bounds;
			bounds.x = Mathf.Max(extends_max.x, extends_corner.x, extends_corner3.x, extends_corner5.x);
			bounds.y = Mathf.Max(extends_max.y, extends_corner.y, extends_corner3.y, extends_corner5.y);
			bounds.z = Mathf.Max(extends_max.z, extends_corner.z, extends_corner3.z, extends_corner5.z);

			return bounds;
		}

		public static void CreateNativeCrystalInformation(Mesh crystal, ref FNativeList<Vector3> mesh_verticeArray, ref FNativeList<int> mesh_triangleArray,
		ref FNativeList<Vector2> mesh_uvArray, ref FNativeList<Vector2> mesh_uv3Array, ref FNativeList<Vector2> mesh_uv4Array,
		ref FNativeList<Vector2> mesh_uv5Array, ref FNativeList<Vector2> mesh_uv6Array, ref FNativeList<Vector3> mesh_normalArray,
		ref FNativeList<Vector4> mesh_tangentsArray, ref FNativeList<Color> mesh_colorArray)
		{
			var vertices = crystal.vertices;
			var triangles = crystal.triangles;
			var uv = crystal.uv;
			var uv3 = crystal.uv3;
			var uv4 = crystal.uv4;
			var uv5 = crystal.uv5;
			var uv6 = crystal.uv6;
			var normals = crystal.normals;
			var tangents = crystal.tangents;
			var colors = crystal.colors;

			int vertcount = vertices.Length;
			int tricount = triangles.Length;

			mesh_verticeArray = new FNativeList<Vector3>(vertcount, Allocator.Persistent);
			mesh_triangleArray = new FNativeList<int>(tricount, Allocator.Persistent);
			mesh_uvArray = new FNativeList<Vector2>(vertcount, Allocator.Persistent);
			mesh_uv3Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			mesh_uv4Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			mesh_uv5Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			mesh_uv6Array = new FNativeList<Vector2>(0, Allocator.Persistent);
			mesh_normalArray = new FNativeList<Vector3>(vertcount, Allocator.Persistent);
			mesh_tangentsArray = new FNativeList<Vector4>(vertcount, Allocator.Persistent);
			mesh_colorArray = new FNativeList<Color>(vertcount, Allocator.Persistent);

			for (int i = 0; i < vertcount; i++)
			{
				mesh_verticeArray.Add(crystal.vertices[i]);
				mesh_uvArray.Add(crystal.uv[i]);
				mesh_normalArray.Add(crystal.normals[i]);
				mesh_tangentsArray.Add(crystal.tangents[i]);

			}

			for (int i = 0; i < crystal.colors.Length; i++)
			{
				mesh_colorArray.Add(crystal.colors[i]);
			}

			var array = crystal.uv3;
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					mesh_uv3Array.Add(array[i]);
				}
			}
			array = crystal.uv4;
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					mesh_uv4Array.Add(array[i]);
				}
			}
			array = crystal.uv5;
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					mesh_uv5Array.Add(array[i]);
				}
			}
			array = crystal.uv6;
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					mesh_uv6Array.Add(array[i]);
				}
			}

			for (int i = 0; i < tricount; i++)
			{
				mesh_triangleArray.Add(crystal.triangles[i]);
			}
		}
	}
}
