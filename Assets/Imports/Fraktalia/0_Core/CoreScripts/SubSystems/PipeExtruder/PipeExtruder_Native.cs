using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Fraktalia.Core.LMS;
using Fraktalia.Core.Math;

namespace Fraktalia.Core.Pipe.Native
{
    [ExecuteInEditMode()]
    public class PipeExtruder_Native : MonoBehaviour
    {

        public PipeExtruder_NativeJob calculator;    
        public PipeExtruder_NativeJob_Faces facecalculator;

        public Vector2[] Querschnitt;
        public float CrossScale = 1;

        [Range(2,20)]
        public int QuerschnittPunkte = 4;
        [Range(0,360)]
        public float QuerschnittStartAngle;

        public Vector2 UV_Multiplier = new Vector2(1, 1);

        public MeshFilter filter;

        [HideInInspector]
        public List<Vector3> verticelist;

        [HideInInspector]
        public List<Vector2> uvlist;

        [HideInInspector]
        public List<int> indiceslist;

        public float ConnectTreshold = 0.1f;

        [HideInInspector]
        public Vector3[] lastPoints;

        public bool CullEnds = false;
        private int lastPointLenght;
        private int lastCrossLenght;

        private JobHandle calchandle;
        private JobHandle facehandle;

		private NativeMeshCombiner combiner;



		public bool IsWorking;

        public void CreateLineMesh(ref NativeArray<Vector3> Points)
        {
            if (Querschnitt == null) ResetCrossSection();
            bool needTangents = false;
            calculator.Points = Points;

            if (Points.Length != lastPointLenght || Querschnitt.Length != lastCrossLenght ||
                !calculator.Querschnitt.IsCreated || !calculator.Points.IsCreated)
            {
                calculator.CleanUp(true);
                calculator.Initialize(ref Points, this);

                lastCrossLenght = Querschnitt.Length;
                lastPointLenght = Points.Length;

                facecalculator.CleanUp();
                facecalculator.Startpoints = new NativeArray<Vector3>(Querschnitt.Length, Allocator.Persistent);
                facecalculator.EndPoints = new NativeArray<Vector3>(Querschnitt.Length, Allocator.Persistent);
                facecalculator.reverseendpoint = new NativeArray<Vector3>(Querschnitt.Length, Allocator.Persistent);

                needTangents = true;
            }

            calculator.gdata = new NativeArray<PipeExtruder_NativeData>(1, Allocator.TempJob);
            calculator.gdata[0] = new PipeExtruder_NativeData(this);

            if (Points.Length > 3)
            {

                calchandle = calculator.Schedule(Points.Length - 3, SystemInfo.processorCount);
                calchandle.Complete();

                facecalculator.Initialize(calculator);

                facehandle = facecalculator.Schedule();
                facehandle.Complete();
               
            }

            BuildLineMesh(needTangents);
        }

        public void CreateLineMeshRealTime(NativeArray<Vector3> Points)
        {
            IsWorking = true;
            StartCoroutine("creationcoroutine", Points);
        }

        private IEnumerator creationcoroutine(NativeArray<Vector3> Points)
        {
            if (Querschnitt == null) ResetCrossSection();
            bool needTangents = false;
            calculator.Points = Points;

            if (Points.Length != lastPointLenght || Querschnitt.Length != lastCrossLenght ||
                !calculator.Querschnitt.IsCreated || !calculator.Points.IsCreated || !facecalculator.Startpoints.IsCreated)
            {
                calculator.CleanUp(true);
                calculator.Initialize(ref Points, this);

                lastCrossLenght = Querschnitt.Length;
                lastPointLenght = Points.Length;

                facecalculator.CleanUp();
                facecalculator.Startpoints = new NativeArray<Vector3>(Querschnitt.Length, Allocator.Persistent);
                facecalculator.EndPoints = new NativeArray<Vector3>(Querschnitt.Length, Allocator.Persistent);
                facecalculator.reverseendpoint = new NativeArray<Vector3>(Querschnitt.Length, Allocator.Persistent);

                needTangents = true;
            }


            calculator.gdata = new NativeArray<PipeExtruder_NativeData>(1, Allocator.TempJob);
            calculator.gdata[0] = new PipeExtruder_NativeData(this);

            if (Points.Length > 3)
            {

                calchandle = calculator.Schedule(Points.Length - 3, SystemInfo.processorCount);
                while (!calchandle.IsCompleted)
                {
                    yield return null;
                }
                calchandle.Complete();

                facecalculator.Initialize(calculator);

                facehandle = facecalculator.Schedule();

                while (!facehandle.IsCompleted)
                {
                    yield return null;
                }
                facehandle.Complete();

                

            }
            BuildLineMesh(needTangents);
            IsWorking = false;
        }


        public void BuildLineMesh(bool needTangents)
        {
			if (!combiner) combiner = GetComponent<NativeMeshCombiner>();
	
            GetComponent<NativeMeshCombiner>().CreateMeshFromNative(
              ref calculator.verticeArray,
              ref calculator.triangleArray,
              ref calculator.uvArray,
              ref calculator.tangentsArray,
              ref calculator.normalsArray, 1, true);
	
			calculator.CleanUp(false);
        }

        public void ResetCrossSection()
        {
            float step = 360 / QuerschnittPunkte;

            List<Vector2> newquerschnitt = new List<Vector2>();
            for (int i = 0; i < QuerschnittPunkte; i++)
            {
                newquerschnitt.Add(MathUtilities.kreisPosition(QuerschnittStartAngle + step * i));
            }
            newquerschnitt.Reverse();
            Querschnitt = newquerschnitt.ToArray();
        }

        public void CleanMemory()
        {
            StopAllCoroutines();
            calchandle.Complete();
            facehandle.Complete();



            facecalculator.CleanUp();
            calculator.CleanUp(true);

            IsWorking = false;
        }

        private void OnDestroy()
        {
            CleanMemory();
        }



    }
}
