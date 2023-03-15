using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace Fraktalia.Core.LMS
{
    public struct NativeUV_Spherical_Data
    {
        public float Power;
        public Vector3 center;
        public NativeUV_Spherical_Data(NativeUV_Spherical Native)
        {
            Power = Native.Power;
            center = Native.center;
        }
    }

    public class NativeUV_Spherical : NativeUVModifier
    {
        public float Power = 1;
        public Vector3 center;
        protected override void Algorithm( 
            ref NativeArray<Vector3> positionData, 
            ref NativeArray<Vector2> uvData)
        {

            NativeUV_Spherical_Job job = new NativeUV_Spherical_Job();
            job.data = new NativeArray<NativeUV_Spherical_Data>(1, Allocator.TempJob);
          
            job.data[0] = new NativeUV_Spherical_Data(this);
            job.positionData = positionData;
            job.uvData = uvData;
                  
            JobHandle hendl = job.Schedule(uvData.Length, SystemInfo.processorCount);
            hendl.Complete();
            job.CleanUp();




        }


    }

    [BurstCompile]
    public struct NativeUV_Spherical_Job : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<NativeUV_Spherical_Data> data;

       
        [ReadOnly]
        public NativeArray<Vector3> positionData;

        public NativeArray<Vector2> uvData;
             

        public void Execute(int Index)
        {
            Vector2 output = new Vector2();
            Vector2 output2 = new Vector2();
            Vector2 output3 = new Vector2();

            Vector3 n = positionData[Index].normalized - data[0].center;
            output.x = Mathf.Atan2(n.x, n.z) / (2 * Mathf.PI) + 0.5f;
            output.y = n.y * 0.5f + 0.5f;
            output2.x = Mathf.Atan2(n.y, n.z) / (2 * Mathf.PI) + 0.5f;
            output2.y = n.x * 0.5f + 0.5f;
            output3.x = Mathf.Atan2(n.x, n.y) / (2 * Mathf.PI) + 0.5f;
            output3.y = n.z * 0.5f + 0.5f;


            Vector2 uv = uvData[Index];
            uvData[Index] = uv + output * data[0].Power;
           
        }

        public void CleanUp()
        {
            data.Dispose();         
        }
    }
}
