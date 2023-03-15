using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace Fraktalia.Core.LMS
{
    [BurstCompile]
    public struct NativeBarycentricCalculator : IJobParallelFor
    {
		[ReadOnly]
        public NativeArray<Vector3> vertices;

		[ReadOnly]
		public NativeArray<int> triangles;


		[NativeDisableParallelForRestriction]
		public NativeArray<Color> colorArray;


		public void Initialize()
        {
			if(!colorArray.IsCreated)
			colorArray = new NativeArray<Color>(vertices.Length, Allocator.Persistent);

			if(colorArray.Length != vertices.Length)
			{
				colorArray.Dispose();
				colorArray = new NativeArray<Color>(vertices.Length, Allocator.Persistent);
			}
        }

		public void Execute(int i)
		{
			int index = i * 3;


			int vertexindex = triangles[index];
			colorArray[vertexindex] = (new Color(1, 0, 0));

			vertexindex = triangles[index+1];
			colorArray[vertexindex] = (new Color(0, 1, 0));

			vertexindex = triangles[index+2];
			colorArray[vertexindex] = (new Color(0, 0, 1));
		}

        public void CleanUp()
        {
			if(colorArray.IsCreated) colorArray.Dispose();
        }
    }
}
