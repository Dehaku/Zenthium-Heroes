using System.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Fraktalia.Core.Collections;


#if UNITY_EDITOR
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
    [System.Serializable]
	public unsafe class SurfaceModifier_CalculateTangents : SurfaceModifier
	{	
		public JobHandle[] handles;
		public CalculateTangentsJob[] jobs = new CalculateTangentsJob[0];
		protected override void initializeModule()
		{
			handles = new JobHandle[HullGenerator.CurrentNumCores];
			jobs = new CalculateTangentsJob[HullGenerator.CurrentNumCores];

            for (int i = 0; i < jobs.Length; i++)
            {
				jobs[i].tan1 = new FNativeList<Vector3>(0, Allocator.Persistent);	
				jobs[i].tan2 = new FNativeList<Vector3>(0, Allocator.Persistent);
			}
		}


		public override IEnumerator beginCalculationasync(float cellSize, float voxelSize)
		{
			for (int coreindex = 0; coreindex < HullGenerator.activeCores; coreindex++)
			{
				int cellIndex = HullGenerator.activeCells[coreindex];
				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.RequiresNonGeometryData) continue;

				if (HullGenerator.DebugMode)
					Debug.Log("Modify Geometry");

				NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];

				jobs[coreindex].verticeArray = data.verticeArray;
				jobs[coreindex].normalArray = data.normalArray;
				jobs[coreindex].tangentsArray = data.tangentsArray;
				jobs[coreindex].triangleArray = data.triangleArray_original;
				jobs[coreindex].uvArray = data.uvArray;		
				jobs[coreindex].Rootsize = this.HullGenerator.engine.RootSize;

				handles[coreindex] = jobs[coreindex].Schedule();
			}

			for (int i = 0; i < HullGenerator.activeCores; i++)
			{
				while (!handles[i].IsCompleted)
				{
					if (HullGenerator.synchronitylevel < 0) break;
					yield return new YieldInstruction();
				}
				handles[i].Complete();
			}

			yield return null;
		}
		
        public override void CleanUp()
        {
			for (int i = 0; i < jobs.Length; i++)
			{
				if(jobs[i].tan1.IsCreated) jobs[i].tan1.Dispose();
				if (jobs[i].tan2.IsCreated) jobs[i].tan2.Dispose();
			}
		}

		public struct CalculateTangentsJob : IJob
		{
			public float UVPower;
			public float Rootsize;
			public FNativeList<Vector3> verticeArray;
			public FNativeList<Vector2> uvArray;
			public FNativeList<Vector3> normalArray;
			public FNativeList<int> triangleArray;
			public FNativeList<Vector4> tangentsArray;

			public int CalculateTangents;

			public FNativeList<Vector3> tan1;
			public FNativeList<Vector3> tan2;

			public void Execute()
			{
				if (uvArray.Length != verticeArray.Length) return;


				if (verticeArray.Length == triangleArray.Length)
					calculateTangentsCheap();
				else
				{
					calculateTangentsExpensive();
				}



			}

			public void calculateTangentsCheap()
			{
				int triangleCount = triangleArray.Length;

				tangentsArray.Clear();
				for (int a = 0; a < triangleCount; a += 3)
				{
					int i1 = triangleArray[a + 0];
					int i2 = triangleArray[a + 1];
					int i3 = triangleArray[a + 2];

					Vector3 v1 = verticeArray[i1];
					Vector3 v2 = verticeArray[i2];
					Vector3 v3 = verticeArray[i3];

					Vector2 w1 = uvArray[i1];
					Vector2 w2 = uvArray[i2];
					Vector2 w3 = uvArray[i3];

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

					Vector3 n1 = normalArray[i1];
					Vector3 n2 = normalArray[i2];
					Vector3 n3 = normalArray[i3];
					Vector3 tan1 = sdir;
					Vector3 tan2 = sdir;
					Vector3 tan3 = sdir;



					Vector3.OrthoNormalize(ref n1, ref tan1);
					Vector4 output = new Vector4();
					output.x = tan1.x;
					output.y = tan1.y;
					output.z = tan1.z;
					output.w = (Vector3.Dot(Vector3.Cross(n1, tan1), tdir) < 0.0f) ? -1.0f : 1.0f;
					tangentsArray.Add(output);

					Vector3.OrthoNormalize(ref n2, ref tan2);
					output.x = tan2.x;
					output.y = tan2.y;
					output.z = tan2.z;
					output.w = (Vector3.Dot(Vector3.Cross(n2, tan2), tdir) < 0.0f) ? -1.0f : 1.0f;
					tangentsArray.Add(output);

					Vector3.OrthoNormalize(ref n3, ref tan3);
					output.x = tan3.x;
					output.y = tan3.y;
					output.z = tan3.z;
					output.w = (Vector3.Dot(Vector3.Cross(n3, tan3), tdir) < 0.0f) ? -1.0f : 1.0f;
					tangentsArray.Add(output);
				}
			}

			public void calculateTangentsExpensive()
			{
				//variable definitions
				int triangleCount = triangleArray.Length;
				int vertexCount = verticeArray.Length;

				while (tan1.Length < vertexCount)
				{
					tan1.Add(new Vector3(0, 0, 0));
					tan2.Add(new Vector3(0, 0, 0));
				}

				tangentsArray.Clear();
				for (int a = 0; a < triangleCount; a += 3)
				{
					int i1 = triangleArray[a + 0];
					int i2 = triangleArray[a + 1];
					int i3 = triangleArray[a + 2];

					Vector3 v1 = verticeArray[i1];
					Vector3 v2 = verticeArray[i2];
					Vector3 v3 = verticeArray[i3];

					Vector2 w1 = uvArray[i1];
					Vector2 w2 = uvArray[i2];
					Vector2 w3 = uvArray[i3];

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
					Vector3 n = normalArray[a];
					Vector3 t = tan1[a];

					//Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
					//tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
					Vector3.OrthoNormalize(ref n, ref t);

					Vector4 output = new Vector4();
					output.x = t.x;
					output.y = t.y;
					output.z = t.z;

					output.w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
					tangentsArray.Add(output);

				}


			}

		}
	}
}