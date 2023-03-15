using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Fraktalia.Core.LMS
{
    public class NativeMeshBuffer
    {

        public NativeArray<Vector3> vertices;
        public int[] triangles;
        public NativeArray<Vector2> uvs;
        public NativeArray<Vector4> tangents;
        public NativeArray<Vector3> normals;

        public NativeMeshBuffer(int verticecount, int trianglecount)
        {
            triangles = new int[trianglecount];
			
		}

		public void Resize(int verticecount, int trianglecount)
        {         
            triangles = new int[trianglecount];		
        }

        public void ApplyToMesh(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
			mesh.SetUVs(0, uvs);
        }

        public void ApplyVertices(Mesh mesh)
        {
            if (vertices.Length == 0)
            {
                mesh.Clear();
            }
            else
            {				
				mesh.SetVertices(vertices);
				mesh.SetNormals(normals);
				mesh.SetTangents(tangents);
				mesh.SetUVs(0, uvs);
			}
        }
    }

}
