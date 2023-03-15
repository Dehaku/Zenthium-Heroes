using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace Fraktalia.Core.LMS
{
    public struct NativeUV_Displace_Data
    {
        public int Seed;
        public int Modulo;
        public float DisplacementPower;
        

        public NativeUV_Displace_Data(NativeUV_Displace Native)
        {
            DisplacementPower = Native.DisplacementPower;
            Seed = Native.Seed;
            Modulo = Native.Modulo;
          
        }
    }


    public class NativeUV_Displace : NativeUVModifier
    {
        public int Seed = 0;
        public int Modulo = 6;
        public float DisplacementPower;

        private int oldSeed = 0;
        private Vector2[] randomTable;

        protected override void Algorithm( 
            ref NativeArray<Vector3> positionData, 
            ref NativeArray<Vector2> uvData)
        {
            Modulo = Mathf.Max(1, Modulo);

            if (Seed != oldSeed || randomTable == null || randomTable.Length != uvData.Length)
            {
                Random.State oldstate = Random.state;
                Random.InitState(Seed);
                randomTable = new Vector2[uvData.Length];
                for (int i = 0; i < randomTable.Length; i++)
                {
                    randomTable[i] = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                }
                Random.state = oldstate;
                oldSeed = Seed;
            }

            NativeUV_Displace_Job job = new NativeUV_Displace_Job();
            job.data = new NativeArray<NativeUV_Displace_Data>(1, Allocator.TempJob);
          
            job.data[0] = new NativeUV_Displace_Data(this);
            job.positionData = positionData;
            job.uvData = uvData;
            job.Initialize(randomTable);
            JobHandle hendl = job.Schedule(uvData.Length, SystemInfo.processorCount);
            hendl.Complete();
            job.CleanUp();

          


        }


    }

    [BurstCompile]
    public struct NativeUV_Displace_Job : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<NativeUV_Displace_Data> data;

       
        [ReadOnly]
        public NativeArray<Vector3> positionData;

        public NativeArray<Vector2> uvData;

        [ReadOnly]
        public NativeArray<Vector2> randomTable;  
        
        public void Initialize(Vector2[] table)
        {
            randomTable = new NativeArray<Vector2>(table, Allocator.TempJob);
            
        }

        public void Execute(int Index)
        {
            Vector2 output = uvData[Index];

            int seed = Index / data[0].Modulo;

            Vector2 displacement = randomTable[seed] * data[0].DisplacementPower;
         
            uvData[Index] = output + displacement;

            
        }

        public void CleanUp()
        {
            data.Dispose();
            randomTable.Dispose();
        }
    }
}