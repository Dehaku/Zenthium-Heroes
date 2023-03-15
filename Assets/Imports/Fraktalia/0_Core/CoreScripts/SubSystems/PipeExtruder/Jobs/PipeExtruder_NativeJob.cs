using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;

namespace Fraktalia.Core.Pipe.Native
{
    public struct PipeExtruder_NativeData
    {
        public float CrossScale;
        public float ConnectTreshold;

        public Vector2 UV_Multiplier;
        public int CullEnds;
        public PipeExtruder_NativeData(PipeExtruder_Native generator)
        {
            CrossScale = generator.CrossScale;
            UV_Multiplier = generator.UV_Multiplier;

            if (generator.CullEnds)
            {
                CullEnds = 1;
            }
            else CullEnds = 0;
        
            ConnectTreshold = generator.ConnectTreshold;
        }
    }

    [BurstCompile]
    public struct PipeExtruder_NativeJob : IJobParallelFor
    {     
       

        [ReadOnly]
        public NativeArray<PipeExtruder_NativeData> gdata;
     

        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> Points;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> Querschnitt;

        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> verticeArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> triangleArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> uvArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector4> tangentsArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> normalsArray;



        public void Initialize(ref NativeArray<Vector3> pointdata, PipeExtruder_Native generator)
        {



           
            Querschnitt = new NativeArray<Vector2>(generator.Querschnitt, Allocator.Persistent);           
           
            int verticecount =  pointdata.Length * Querschnitt.Length * 4;
            int triangles = pointdata.Length * Querschnitt.Length * 6;

            verticecount += (Querschnitt.Length + 1) * 2;
            triangles += (Querschnitt.Length + 1) * 2 * 3;


            verticeArray = new NativeArray<Vector3>(verticecount, Allocator.Persistent);
            triangleArray = new NativeArray<int>(triangles, Allocator.Persistent);
            uvArray = new NativeArray<Vector2>(verticecount, Allocator.Persistent);
            tangentsArray = new NativeArray<Vector4>(verticecount, Allocator.Persistent);
            normalsArray = new NativeArray<Vector3>(verticecount, Allocator.Persistent);

         
        }

        public void Execute(int index)
        {
            int i = index+1;

            Vector3 direction1 = Points[i] + Points[i - 1];
            Vector3 direction2 = Points[i] + Points[i + 1];

            Vector3 direction = (direction1 + direction2).normalized;
            Vector3 up = Vector3.Cross(direction1, direction2).normalized;


            Vector3 nextdirection1 = Points[i + 1] + Points[i];
            Vector3 nextdirection2 = Points[i + 1] + Points[i + 2];
            Vector3 nextdirection = (nextdirection1 + nextdirection2).normalized;
            Vector3 nextup = Vector3.Cross(nextdirection1, nextdirection2).normalized;           

            for (int y = 0; y < Querschnitt.Length; y++)
            {
                Vector2 QPoint_1;
                Vector2 QPoint_2;
                if (y == Querschnitt.Length - 1)
                {
                    QPoint_1 = Querschnitt[Querschnitt.Length - 1];
                    QPoint_2 = Querschnitt[0];
                }
                else
                {
                    QPoint_1 = Querschnitt[y];
                    QPoint_2 = Querschnitt[y + 1];
                }

                QPoint_1 *= gdata[0].CrossScale;
                QPoint_2 *= gdata[0].CrossScale;

                Vector3 localPoint1 = direction * QPoint_1.x + up * QPoint_1.y;
                Vector3 localPoint2 = direction * QPoint_2.x + up * QPoint_2.y;

                Vector3 nextlocalPoint1 = nextdirection * QPoint_1.x + nextup * QPoint_1.y;
                Vector3 nextlocalPoint2 = nextdirection * QPoint_2.x + nextup * QPoint_2.y;           

                GenerateFace(index, y,
                  Points[i] + localPoint1,
                  Points[i] + localPoint2,
                  Points[i + 1] + nextlocalPoint1,
                  Points[i + 1] + nextlocalPoint2
                  );
            }

           

        }



        [BurstDiscard]
        public void CleanUp(bool CleanMesh)
        {          
           
            if (gdata.IsCreated) gdata.Dispose();
            if (CleanMesh)
            {
                if (verticeArray.IsCreated) verticeArray.Dispose();
                if (triangleArray.IsCreated) triangleArray.Dispose();
                if (uvArray.IsCreated) uvArray.Dispose();
                if (tangentsArray.IsCreated) tangentsArray.Dispose();
                if (normalsArray.IsCreated) normalsArray.Dispose();
                if (Querschnitt.IsCreated) Querschnitt.Dispose();


            }



        }

        void GenerateFace(int index,int cross, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int vertIndex = index * Querschnitt.Length * 4 + cross * 4;

            verticeArray[vertIndex] = (a);      
            verticeArray[vertIndex+1] = (b);    
            verticeArray[vertIndex+2] = (d);
            verticeArray[vertIndex+3] = (c);

            Vector3 normal = Vector3.Cross(b - a, d - a).normalized;

            normalsArray[vertIndex + 0] = normal;
            normalsArray[vertIndex + 1] = normal;
            normalsArray[vertIndex + 2] = normal;
            normalsArray[vertIndex + 3] = normal; 

            float lengthx = (b - a).magnitude * gdata[0].UV_Multiplier.x;

            float lengthy = (a - d).magnitude * gdata[0].UV_Multiplier.y;


            uvArray[vertIndex] = (new Vector2(0, 0));
            uvArray[vertIndex+1] = (new Vector2(0, lengthx));
            uvArray[vertIndex+2] = (new Vector2(lengthy, lengthx));
            uvArray[vertIndex+3] = (new Vector2(lengthy, 0));

            int triIndex = index * Querschnitt.Length * 6 + cross * 6;

            triangleArray[triIndex+0] = 0 + vertIndex;
            triangleArray[triIndex+1] = 1 + vertIndex;
            triangleArray[triIndex+2] = 2 + vertIndex;
            triangleArray[triIndex+3] = 2 + vertIndex;
            triangleArray[triIndex+4] = 3 + vertIndex;
            triangleArray[triIndex+5] = 0 + vertIndex;

            

        }
    }

    [BurstCompile]
    public struct PipeExtruder_NativeJob_Faces : IJob
    {


        [ReadOnly]
        public NativeArray<PipeExtruder_NativeData> gdata;


        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> Points;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> Querschnitt;

        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> verticeArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> triangleArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> uvArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector4> tangentsArray;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> normalsArray;


        public NativeArray<Vector3> Startpoints;
        public NativeArray<Vector3> EndPoints;
        public NativeArray<Vector3> reverseendpoint;

        public void Initialize(PipeExtruder_NativeJob generator)
        {



            Points = generator.Points;
            Querschnitt = generator.Querschnitt;
        
            verticeArray = generator.verticeArray;
            triangleArray = generator.triangleArray;
            uvArray = generator.uvArray;
            tangentsArray = generator.tangentsArray;
            normalsArray = generator.normalsArray;

            gdata = generator.gdata;

        }

        public void Execute()
        {                 
            
            if (gdata[0].CullEnds== 0)
            {
                if (Points.Length > 3)
                {
                    Vector3 direction1 = Points[Points.Length - 2] + Points[Points.Length - 3];
                    Vector3 direction2 = Points[Points.Length - 2] + Points[Points.Length - 1];

                    Vector3 direction = (direction1 + direction2).normalized;
                    Vector3 up = Vector3.Cross(direction1, direction2).normalized;


                    Vector3 nextdirection1 = Points[1] + Points[0];
                    Vector3 nextdirection2 = Points[1] + Points[2];
                    Vector3 nextdirection = (nextdirection1 + nextdirection2).normalized;
                    Vector3 nextup = Vector3.Cross(nextdirection1, nextdirection2).normalized;

                    Vector3 connectdirection1 = Points[0] + Points[Points.Length - 2];
                    Vector3 connectdirection2 = Points[0] + Points[1];
                    Vector3 connectdirection = (connectdirection1 + connectdirection2).normalized;
                    Vector3 connectup = Vector3.Cross(connectdirection1, connectdirection2).normalized;

                    
                    for (int y = 0; y < Querschnitt.Length; y++)
                    {
                        Vector2 QPoint_1;
                        Vector2 QPoint_2;
                        if (y == Querschnitt.Length - 1)
                        {
                            QPoint_1 = Querschnitt[Querschnitt.Length - 1];
                            QPoint_2 = Querschnitt[0];
                        }
                        else
                        {
                            QPoint_1 = Querschnitt[y];
                            QPoint_2 = Querschnitt[y + 1];
                        }

                        QPoint_1 *= gdata[0]. CrossScale;
                        QPoint_2 *= gdata[0].CrossScale;

                        Vector3 localPoint1 = direction * QPoint_1.x + up * QPoint_1.y;
                        Vector3 localPoint2 = direction * QPoint_2.x + up * QPoint_2.y;

                        Vector3 nextlocalPoint1 = nextdirection * QPoint_1.x + nextup * QPoint_1.y;
                        Vector3 nextlocalPoint2 = nextdirection * QPoint_2.x + nextup * QPoint_2.y;

                        Vector3 plane = (Points[1] - Points[0]);
                        Vector3 plane2 = (Points[Points.Length - 2] - Points[Points.Length - 1]);


                        Vector3 projectednextlocalPoint1 = ProjectOnPlane(nextlocalPoint1, plane);
                        Vector3 projectednextlocalPoint2 = ProjectOnPlane(nextlocalPoint2, plane);
                   
                        Vector3 projectedlocalPoint1 = ProjectOnPlane(localPoint1, plane2);
                        Vector3 projectedlocalPoint2 = ProjectOnPlane(localPoint2, plane2);



                        Vector3 connectlocalPoint1 = connectdirection * QPoint_1.x + connectup * QPoint_1.y;
                        Vector3 connectlocalPoint2 = connectdirection * QPoint_2.x + connectup * QPoint_2.y;



                        if ((Points[0] - Points[Points.Length - 1]).sqrMagnitude < gdata[0].ConnectTreshold * gdata[0].ConnectTreshold)
                        {

                            GenerateFace(Points.Length - 1, y,
                              Points[0] + connectlocalPoint1,
                              Points[0] + connectlocalPoint2,
                              Points[1] + nextlocalPoint1,
                              Points[1] + nextlocalPoint2
                              );

                            Startpoints[y] = (Points[0] + connectlocalPoint1);

                            GenerateFace(Points.Length-2,y,
                             Points[Points.Length - 2] + localPoint1,
                             Points[Points.Length - 2] + localPoint2,
                             Points[Points.Length - 1] + connectlocalPoint1,
                             Points[Points.Length - 1] + connectlocalPoint2
                             );

                            EndPoints[y] = (Points[Points.Length - 1] + connectlocalPoint1);
                        }
                        else
                        {


                            GenerateFace(Points.Length - 1, y,
                             Points[0] + projectednextlocalPoint1,
                             Points[0] + projectednextlocalPoint2,
                             Points[1] + nextlocalPoint1,
                             Points[1] + nextlocalPoint2
                             );

                            Startpoints[y] = (Points[0] + projectednextlocalPoint1);

                            GenerateFace(Points.Length - 2,y,
                             Points[Points.Length - 2] + localPoint1,
                             Points[Points.Length - 2] + localPoint2,
                             Points[Points.Length - 1] + projectedlocalPoint1,
                             Points[Points.Length - 1] + projectedlocalPoint2
                             );

                            EndPoints[y] = (Points[Points.Length - 1] + projectedlocalPoint1);
                        }




                    }
                    
                }
            }
            else
            {
                int lastIndex = (Points.Length-4) * Querschnitt.Length * 4;
                for (int i = 0; i < Querschnitt.Length; i++)
                {
                    Startpoints[i] = verticeArray[i*4];
                    EndPoints[i] = verticeArray[lastIndex + 2 + i * 4];


                }



            }

            GenerateSurface(Startpoints, 0);
           
            
            for (int i = 0; i < EndPoints.Length; i++)
            {
                reverseendpoint[i] = EndPoints[EndPoints.Length - 1 - i];
            }
           
            GenerateSurface(reverseendpoint, 1);


            


        }    

        void GenerateFace(int index, int cross, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int vertIndex = index * Querschnitt.Length * 4 + cross * 4;

            verticeArray[vertIndex] = (a);
            verticeArray[vertIndex + 1] = (b);
            verticeArray[vertIndex + 2] = (d);
            verticeArray[vertIndex + 3] = (c);

            Vector3 normal = -Vector3.Cross(b - a, b - d).normalized;

            normalsArray[vertIndex + 0] = normal;//   Vector3.Cross(b, d).normalized;
            normalsArray[vertIndex + 1] = normal;//Vector3.Cross(a, d).normalized;
            normalsArray[vertIndex + 2] = normal;//Vector3.Cross(a, b).normalized;
            normalsArray[vertIndex + 3] = normal;//Vector3.Cross(d, a).normalized; 

            float lengthx = (b - a).magnitude * gdata[0].UV_Multiplier.x;

            float lengthy = (a - d).magnitude * gdata[0].UV_Multiplier.y;


            uvArray[vertIndex] = (new Vector2(0, 0));
            uvArray[vertIndex + 1] = (new Vector2(0, lengthx));
            uvArray[vertIndex + 2] = (new Vector2(lengthy, lengthx));
            uvArray[vertIndex + 3] = (new Vector2(lengthy, 0));

            int triIndex = index * Querschnitt.Length * 6 + cross * 6;

            triangleArray[triIndex + 0] = 0 + vertIndex;
            triangleArray[triIndex + 1] = 1 + vertIndex;
            triangleArray[triIndex + 2] = 2 + vertIndex;
            triangleArray[triIndex + 3] = 2 + vertIndex;
            triangleArray[triIndex + 4] = 3 + vertIndex;
            triangleArray[triIndex + 5] = 0 + vertIndex;



        }

        void GenerateSurface(NativeArray<Vector3> surfacepoints, int faceNumber)
        {
           

            int vertexCount = surfacepoints.Length + 1;
            int index = Points.Length * Querschnitt.Length * 4;
            int triIndex = Points.Length * Querschnitt.Length * 6;

            index += vertexCount * faceNumber;
            triIndex += vertexCount * faceNumber * 3;

            Vector3 center = new Vector3();
            for (int i = 0; i < surfacepoints.Length; i++)
            {
                verticeArray[index + i + 1] = surfacepoints[i];
                center += surfacepoints[i];
            }

            

            Vector2 uvcenter = new Vector3();
            for (int i = 0; i < Querschnitt.Length; i++)
            {
                uvcenter += Querschnitt[i];
            }
            uvcenter /= Querschnitt.Length;

            if (surfacepoints.Length > 0)
            {
                center /= surfacepoints.Length;
                uvcenter /= Querschnitt.Length;
            }
            verticeArray[index] = center;

            Vector3 normal = Vector3.Cross(center - surfacepoints[0], center - surfacepoints[1]).normalized;
            for (int i = 0; i < vertexCount; i++)
            {
                normalsArray[index + i] = normal;
            }


            uvArray[index] =(new Vector2(uvcenter.x * gdata[0].UV_Multiplier.x, uvcenter.y * gdata[0].UV_Multiplier.y));

            for (int i = 1; i < vertexCount - 1; i++)
            {

                triangleArray[triIndex + i * 3] = (index);
                triangleArray[triIndex + i * 3 + 1] = (i + 1 + index);
                triangleArray[triIndex + i * 3 + 2] = (i + index);


                uvArray[index + i] = (new Vector2(Querschnitt[i].x * gdata[0].UV_Multiplier.x, Querschnitt[i].y * gdata[0].UV_Multiplier.y));
            }
            uvArray[index + vertexCount-1]=  (new Vector2(Querschnitt[0].x * gdata[0].UV_Multiplier.x, Querschnitt[0].y * gdata[0].UV_Multiplier.y));

            

            triangleArray[triIndex] = (index);
            triangleArray[triIndex + 1] = (1 + index);
            triangleArray[triIndex + 2] = (vertexCount - 1 + index);       
        }

        public void CleanUp()
        {
            if (Startpoints.IsCreated) Startpoints.Dispose();
            if (EndPoints.IsCreated) EndPoints.Dispose();
            if(reverseendpoint.IsCreated) reverseendpoint.Dispose();

        }

        public Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        public Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            
            float num = Vector3.Dot(onNormal, onNormal);
            if (num < 0.000000000000001f)
                return new Vector3(0,0,0);
            else
                return onNormal * Vector3.Dot(vector, onNormal) / num;
        }
    }
}