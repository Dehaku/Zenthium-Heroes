using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace Fraktalia.Core.LMS
{
    public struct NativeUV_Multiply_Data
    {
        public Vector2 MultiPly;
        public Vector2 Shift;
        public NativeUV_Multiply_Data(NativeUV_Multiply Native)
        {
			MultiPly = Native.MultiPly;
			Shift = Native.Shift;
        }
    }

    public class NativeUV_Multiply : NativeUVModifier
    {
        public Vector2 MultiPly;
        public Vector2 Shift;
		protected override void Algorithm( 
            ref NativeArray<Vector3> positionData, 
            ref NativeArray<Vector2> uvData)
        {

			NativeUV_Multiply_Job job = new NativeUV_Multiply_Job();
            job.data = new NativeUV_Multiply_Data(this);
            job.positionData = positionData;
            job.uvData = uvData;
                  
            JobHandle hendl = job.Schedule(uvData.Length, SystemInfo.processorCount);
            hendl.Complete();
            job.CleanUp();




        }


    }

    [BurstCompile]
    public struct NativeUV_Multiply_Job : IJobParallelFor
    {
        
        public NativeUV_Multiply_Data data;

       
        [ReadOnly]
        public NativeArray<Vector3> positionData;
        public NativeArray<Vector2> uvData;
             

        public void Execute(int Index)
        {        
            Vector2 uv = uvData[Index];
            uvData[Index] = uv * data.MultiPly + data.Shift;           
        }

        public void CleanUp()
        {
                 
        }
    }
}
