using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace Fraktalia.Core.LMS
{
    [BurstCompile]
    public struct NativeTangentsCalculator : IJob
    {

        public NativeArray<Vector3> vertices;
        public NativeArray<Vector2> uv;
        public NativeArray<Vector3> normals;
        public NativeArray<int> triangles;
        public NativeArray<Vector4> tangents;

        public NativeArray<Vector3> tan1;
        public NativeArray<Vector3> tan2;

        public void Initialize()
        {
            tan1 = new NativeArray<Vector3>(uv.Length, Allocator.TempJob);
            tan2 = new NativeArray<Vector3>(uv.Length, Allocator.TempJob);
        }

        public void Execute()
        {

            //variable definitions
            int triangleCount = triangles.Length;
            int vertexCount = vertices.Length;


            for (int a = 0; a < triangleCount; a += 3)
            {
                int i1 = triangles[a + 0];
                int i2 = triangles[a + 1];
                int i3 = triangles[a + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = uv[i1];
                Vector2 w2 = uv[i2];
                Vector2 w3 = uv[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float div = s1 * t2 - s2 * t1;
                float r = div == 0.0f ? 0.0f : 1.0f / div;

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }


            for (int a = 0; a < vertexCount; ++a)
            {
                Vector3 n = normals[a];
                Vector3 t = tan1[a];

                //Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
                //tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
                Vector3.OrthoNormalize(ref n, ref t);

                Vector4 output = new Vector4();
                output.x = t.x;
                output.y = t.y;
                output.z = t.z;

                output.w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
                tangents[a] = output;

            }


        }

        public void CleanUp()
        {
            tan2.Dispose();
            tan1.Dispose();
        }
    }

	[BurstCompile]
	public struct NativeTangentsCalculator_FirstStep : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeArray<Vector3> vertices;
		[NativeDisableParallelForRestriction]
		public NativeArray<Vector2> uv;
		[NativeDisableParallelForRestriction]
		public NativeArray<int> triangles;

		[NativeDisableParallelForRestriction]
		public NativeArray<Vector3> tan1;

		[NativeDisableParallelForRestriction]
		public NativeArray<Vector3> tan2;

		public void Initialize()
		{
			tan1 = new NativeArray<Vector3>(uv.Length, Allocator.TempJob);
			tan2 = new NativeArray<Vector3>(uv.Length, Allocator.TempJob);
		}


		public void Execute(int index)
		{
			int a = index * 3;

			int i1 = triangles[a + 0];
			int i2 = triangles[a + 1];
			int i3 = triangles[a + 2];

			Vector3 v1 = vertices[i1];
			Vector3 v2 = vertices[i2];
			Vector3 v3 = vertices[i3];

			Vector2 w1 = uv[i1];
			Vector2 w2 = uv[i2];
			Vector2 w3 = uv[i3];

			float x1 = v2.x - v1.x;
			float x2 = v3.x - v1.x;
			float y1 = v2.y - v1.y;
			float y2 = v3.y - v1.y;
			float z1 = v2.z - v1.z;
			float z2 = v3.z - v1.z;

			float s1 = w2.x - w1.x;
			float s2 = w3.x - w1.x;
			float t1 = w2.y - w1.y;
			float t2 = w3.y - w1.y;

			float div = s1 * t2 - s2 * t1;
			float r = div == 0.0f ? 0.0f : 1.0f / div;

			Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
			Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

			tan1[i1] += sdir;
			tan1[i2] += sdir;
			tan1[i3] += sdir;

			tan2[i1] += tdir;
			tan2[i2] += tdir;
			tan2[i3] += tdir;
		}

		public void CleanUp()
		{
			tan2.Dispose();
			tan1.Dispose();
		}

	}

	[BurstCompile]
	public struct NativeTangentsCalculator_SecondStep : IJobParallelFor
	{
		[WriteOnly]
		public NativeArray<Vector4> tangents;

		[ReadOnly]
		public NativeArray<Vector3> tan1;

		[ReadOnly]
		public NativeArray<Vector3> tan2;

		[ReadOnly]
		public NativeArray<Vector3> normals;

		public void Execute(int index)
		{
			int a = index;
			Vector3 n = normals[a];
			Vector3 t = tan1[a];
			Vector3.OrthoNormalize(ref n, ref t);
			Vector4 output = new Vector4();
			output.x = t.x;
			output.y = t.y;
			output.z = t.z;
			output.w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
			tangents[a] = output;
		}	
	}
}
