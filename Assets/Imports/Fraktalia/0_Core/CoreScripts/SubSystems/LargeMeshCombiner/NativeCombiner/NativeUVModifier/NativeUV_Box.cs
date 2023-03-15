using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace Fraktalia.Core.LMS
{
    public struct NativeUV_Box_Data
    {
        public float Power;
        public int Skips;
        public NativeUV_Box_Data(NativeUV_Box Native)
        {
            Power = Native.Power;
            Skips = Native.Skips;
        }
    }


    public class NativeUV_Box : NativeUVModifier
    {
        public float Power = 1;
        public int Skips = 4;
        protected override void Algorithm( 
            ref NativeArray<Vector3> positionData, 
            ref NativeArray<Vector2> uvData)
        {

            NativeUV_Box_Job job = new NativeUV_Box_Job();
            job.data = new NativeArray<NativeUV_Box_Data>(1, Allocator.TempJob);
          
            job.data[0] = new NativeUV_Box_Data(this);
            job.positionData = positionData;
            job.uvData = uvData;
                  
            JobHandle hendl = job.Schedule(uvData.Length/Skips, SystemInfo.processorCount);
            hendl.Complete();
            job.CleanUp();
        }

        public override bool HasErrors()
        {
            if (Skips <= 0) return true;
            return false;
        }
    }

    [BurstCompile]
    public struct NativeUV_Box_Job : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<NativeUV_Box_Data> data;

       
        [ReadOnly]
        public NativeArray<Vector3> positionData;

        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> uvData;
             

        public void Execute(int Index)
        {

           
            Vector2 output = new Vector2();

            Vector3 vertex = positionData[Index * data[0].Skips];

           
            float absX = Mathf.Abs(vertex.x);
            float absY = Mathf.Abs(vertex.y);
            float absZ = Mathf.Abs(vertex.z);

            int choosenX = 0;
            int choosenY = 0;
            int choosenZ = 0;

            if (absX > absY && absX > absZ)
            {
                choosenX = 1;              
            }           
            else if (absY > absX && absY > absZ)
            {
                choosenY = 1;        
            }          
            else if (absZ > absX && absZ > absY)
            {
                choosenZ = 1;            
            }

            int skips = data[0].Skips;
         
           
            for (int i = 0; i < skips; i++)
            {
                vertex = positionData[Index * data[0].Skips + i];
                absX = Mathf.Abs(vertex.x);
                absY = Mathf.Abs(vertex.y);
                absZ = Mathf.Abs(vertex.z);




                output = new Vector2();
                output += new Vector2(-vertex.z, -vertex.y) * choosenX / absX;
                output += new Vector2(vertex.x, vertex.z) * choosenY / absY;
                output += new Vector2(vertex.x, -vertex.y) * choosenZ / absZ;
                output *= 0.5f;
                output += new Vector2(0.5f, 0.5f);

                Vector2 uv = uvData[Index * data[0].Skips + i];
                uvData[Index * data[0].Skips + i] = uv + output * data[0].Power;
            }



          

           

          

        }

        public void CleanUp()
        {
            data.Dispose();         
        }
    }
}